using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.ViewModels;
using SMZ.Conta.App.Views;

namespace SMZ.Conta.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    private readonly AccessSession _session;

    private bool _skipUnsavedChangesPrompt;
    private int _zoomPercent = 100;

    public MainWindow(AccessSession session)
    {
        InitializeComponent();
        _session = session;
        _viewModel = new MainWindowViewModel(session);
        _viewModel.ReauthenticationRequired += ViewModel_ReauthenticationRequired;
        DataContext = _viewModel;

        ApplyZoom(DisplayPreferences.LoadZoom(), save: false);
        PreviewKeyDown += OnDisplayKeyDown;

        Closing += MainWindow_Closing;
    }

    public bool LogoutRequested { get; private set; }

    private void ShellLayout_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateResponsiveLayout();

    private void UpdateResponsiveLayout()
    {
        if (!IsInitialized || SidebarNavigation is null) return;
        var width = ShellLayout.ActualWidth;
        var iconsOnly = width < 960;
        var compact = width < 1500 || ShellLayout.ActualHeight < 720;
        SidebarColumn.Width = new GridLength(iconsOnly ? 64 : width < 1500 ? 224 : 312);
        SidebarBrand.Margin = iconsOnly ? new Thickness(4, 8, 4, 0) : new Thickness(16, 10, 16, 8);
        SidebarLogo.Width = SidebarLogo.Height = iconsOnly ? 48 : compact ? 112 : 221;
        SidebarBrandTitle.Visibility = SidebarBrandSubtitle.Visibility = iconsOnly ? Visibility.Collapsed : Visibility.Visible;
        foreach (var button in SidebarNavigation.Children.OfType<Button>())
        {
            if (button.Content is not Grid grid) continue;
            var label = grid.Children.OfType<TextBlock>().FirstOrDefault(text => Grid.GetColumn(text) == 1);
            if (label is null) continue;
            button.ToolTip = label.Text;
            System.Windows.Automation.AutomationProperties.SetName(button, label.Text);
            label.Visibility = iconsOnly ? Visibility.Collapsed : Visibility.Visible;
            label.TextWrapping = TextWrapping.Wrap;
            button.Padding = iconsOnly ? new Thickness(2, 8, 2, 8) : new Thickness(10, 8, 10, 8);
            button.Margin = iconsOnly ? new Thickness(6, 0, 6, 4) : new Thickness(10, 0, 10, 4);
        }
        SidebarAccount.Margin = iconsOnly ? new Thickness(5, 0, 5, 8) : new Thickness(16, 0, 16, 10);
        SidebarUser.Visibility = SidebarRole.Visibility = iconsOnly ? Visibility.Collapsed : Visibility.Visible;
        SidebarAccount.ToolTip = $"{_viewModel?.CurrentUserDisplay}\n{_viewModel?.CurrentRoleDisplay}";
        ChangePasswordButton.Content = iconsOnly ? "PW" : "Cambia password";
        ChangePasswordButton.ToolTip = "Cambia password";
        LogoutButton.Content = iconsOnly ? "↪" : "Disconnetti";
        LogoutButton.ToolTip = "Disconnetti";
        System.Windows.Automation.AutomationProperties.SetName(ChangePasswordButton, "Cambia password");
        System.Windows.Automation.AutomationProperties.SetName(LogoutButton, "Disconnetti");
        HeaderRow.Height = new GridLength(compact ? 64 : 86);
        HeaderBorder.Padding = new Thickness(compact ? 12 : 32, 0, compact ? 12 : 32, 0);
        HeaderTitle.FontSize = compact ? 24 : 38;
        var contentWidth = width - SidebarColumn.Width.Value;
        HeaderDate.Visibility = HeaderDateDivider.Visibility = contentWidth < 1100 ? Visibility.Collapsed : Visibility.Visible;
        HeaderTime.Visibility = HeaderTimeDivider.Visibility = contentWidth < 780 ? Visibility.Collapsed : Visibility.Visible;
        ExitButton.Width = iconsOnly ? 56 : 88;
        WorkspaceScroll.Margin = new Thickness(compact ? 8 : 20);
        FooterBackup.Visibility = contentWidth < 1100 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e) => ApplyZoom(_zoomPercent - 10);
    private void ZoomIn_Click(object sender, RoutedEventArgs e) => ApplyZoom(_zoomPercent + 10);
    private void ZoomReset_Click(object sender, RoutedEventArgs e) => ApplyZoom(100);

    private void ApplyZoom(int percent, bool save = true)
    {
        _zoomPercent = Math.Clamp(percent, DisplayPreferences.MinimumZoom, DisplayPreferences.MaximumZoom);
        InterfaceScale.ScaleX = InterfaceScale.ScaleY = _zoomPercent / 100d;
        ZoomResetButton.Content = $"{_zoomPercent}%";
        ZoomOutButton.IsEnabled = _zoomPercent > DisplayPreferences.MinimumZoom;
        ZoomInButton.IsEnabled = _zoomPercent < DisplayPreferences.MaximumZoom;
        if (save) DisplayPreferences.SaveZoom(_zoomPercent);
    }

    private void OnDisplayKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0
            || (Keyboard.Modifiers & (ModifierKeys.Alt | ModifierKeys.Windows)) != 0) return;
        switch (e.Key)
        {
            case Key.Add: case Key.OemPlus: ApplyZoom(_zoomPercent + 10); break;
            case Key.Subtract: case Key.OemMinus: ApplyZoom(_zoomPercent - 10); break;
            case Key.D0: case Key.NumPad0: ApplyZoom(100); break;
            default: return;
        }
        e.Handled = true;
    }

    private void CloseAppButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PasswordChangeWindow(new Data.AccessService(), _session, required: false) { Owner = this };
        dialog.ShowDialog();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        LogoutRequested = true;
        Close();
    }

    private void ViewModel_ReauthenticationRequired(object? sender, EventArgs e)
    {
        _skipUnsavedChangesPrompt = true;
        LogoutRequested = true;
        Close();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_skipUnsavedChangesPrompt)
        {
            return;
        }

        var areeConModifiche = _viewModel.GetAreeConModificheNonSalvate();
        if (areeConModifiche.Count == 0)
        {
            return;
        }

        var elencoAree = string.Join(", ", areeConModifiche);
        var result = MessageBox.Show(
            $"Ci sono modifiche non salvate in: {elencoAree}.\n\nVuoi chiudere comunque l'applicazione?",
            "Modifiche non salvate",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            e.Cancel = true;
            LogoutRequested = false;
        }
    }

}
