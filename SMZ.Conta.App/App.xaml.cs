using System.Windows;
using SMZ.Conta.App.Data;
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

            var mainWindow = new MainWindow(landingWindow.Session);
            MainWindow = mainWindow;
            mainWindow.ShowDialog();
            if (mainWindow.LogoutRequested)
            {
                continue;
            }

            Shutdown();
            return;
        }
    }
}
