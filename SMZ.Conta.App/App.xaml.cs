using System.Windows;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Views;

namespace SMZ.Conta.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            DatabaseInitializer.EnsureDatabase();
        }
        catch (Exception ex)
        {
            var dettagli = ex.Message;
            if (dettagli.Contains("readonly database", StringComparison.OrdinalIgnoreCase))
            {
                dettagli =
                    $"{ex.Message}{Environment.NewLine}{Environment.NewLine}Percorso database: {DatabasePaths.DatabasePath}";
            }

            MessageBox.Show(
                $"Errore durante l'inizializzazione del database SQLite.{Environment.NewLine}{Environment.NewLine}{dettagli}",
                "SMZ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
            return;
        }

        try
        {
            RunAuthenticatedApplication();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Errore durante l'avvio dell'interfaccia principale.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "SMZ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    private void RunAuthenticatedApplication()
    {
#if DEBUG
        var developmentSession = new AccessSession(
            0,
            "Modalità test - login disattivato",
            AccessRole.Administrator,
            false);
        var developmentWindow = new MainWindow(developmentSession);
        MainWindow = developmentWindow;
        developmentWindow.ShowDialog();
        Shutdown();
        return;
#else
        var accessService = new AccessService();
        while (true)
        {
            var landingWindow = new LandingWindow(accessService);
            MainWindow = landingWindow;
            if (landingWindow.ShowDialog() != true || landingWindow.Session is null)
            {
                Shutdown();
                return;
            }

            var mainWindow = landingWindow.PreparedMainWindow ?? new MainWindow(landingWindow.Session);
            var transitionWindow = landingWindow.TransitionWindow;
            MainWindow = mainWindow;
            EventHandler? contentRenderedHandler = null;
            contentRenderedHandler = (_, _) =>
            {
                mainWindow.ContentRendered -= contentRenderedHandler;
                transitionWindow?.Close();
            };
            mainWindow.ContentRendered += contentRenderedHandler;
            try
            {
                mainWindow.ShowDialog();
            }
            finally
            {
                mainWindow.ContentRendered -= contentRenderedHandler;
                transitionWindow?.Close();
            }
            if (mainWindow.LogoutRequested)
            {
                continue;
            }

            Shutdown();
            return;
        }
#endif
    }
}
