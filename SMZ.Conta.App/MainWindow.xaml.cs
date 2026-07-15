using System.ComponentModel;
using System.Windows;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.ViewModels;
using SMZ.Conta.App.Views;

namespace SMZ.Conta.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    private readonly AccessSession _session;

    public MainWindow(AccessSession session)
    {
        InitializeComponent();
        _session = session;
        _viewModel = new MainWindowViewModel(session);
        DataContext = _viewModel;

        Closing += MainWindow_Closing;
    }

    public bool LogoutRequested { get; private set; }

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

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
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
