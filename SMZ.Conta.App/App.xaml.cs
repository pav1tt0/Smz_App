using System.Windows;
using SMZ.Conta.App.Data;

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
            MessageBox.Show(
                $"Errore durante l'inizializzazione del database SQLite.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "SMZ Conta",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
            return;
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
