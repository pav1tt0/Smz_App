using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.App.Views;

public partial class LandingWindow : Window
{
    private readonly LandingViewModel _viewModel;
    private readonly AccessService _accessService;

    public LandingWindow(AccessService accessService)
    {
        InitializeComponent();
        _accessService = accessService;
        _viewModel = new LandingViewModel(OpenLogin);
        DataContext = _viewModel;
    }

    public AccessSession? Session { get; private set; }

    public MainWindow? PreparedMainWindow { get; private set; }

    public StartupLoadingWindow? TransitionWindow { get; private set; }

    private void OpenLogin()
    {
        var loginWindow = new LoginWindow(_accessService) { Owner = this };
        if (loginWindow.ShowDialog() != true || loginWindow.Session is null)
        {
            return;
        }

        var session = loginWindow.Session;
        if (session.MustChangePassword)
        {
            var passwordWindow = new PasswordChangeWindow(_accessService, session, required: true) { Owner = this };
            if (passwordWindow.ShowDialog() != true)
            {
                return;
            }

            session = session with { MustChangePassword = false };
        }

        Session = session;
        LoadingOverlay.Visibility = Visibility.Visible;
        Cursor = Cursors.Wait;
        Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));

        try
        {
            PreparedMainWindow = new MainWindow(session);
            TransitionWindow = new StartupLoadingWindow();
            TransitionWindow.Show();
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
            DialogResult = true;
        }
        catch
        {
            TransitionWindow?.Close();
            TransitionWindow = null;
            PreparedMainWindow = null;
            Session = null;
            LoadingOverlay.Visibility = Visibility.Collapsed;
            Cursor = null;
            throw;
        }
    }

}
