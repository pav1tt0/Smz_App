using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Printing;

namespace SMZ.Conta.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private void CreaBackupLocaleManuale()
    {
        try
        {
            var result = _backupService.CreateLocalBackup("manual");
            AggiornaStatoBackup();
            Stato = $"Backup locale creato: {Path.GetFileName(result.BackupPath)}";
            MessageBox.Show(
                $"Backup locale creato in:\n{result.BackupPath}",
                "Backup locale",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Backup locale", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Backup locale non riuscito.";
        }
    }

    private void CreaBackupEsternoManuale()
    {
        if (string.IsNullOrWhiteSpace(_backupSettings.ExternalBackupDirectory))
        {
            ConfiguraCartellaBackupEsterno();
            if (string.IsNullOrWhiteSpace(_backupSettings.ExternalBackupDirectory))
            {
                return;
            }
        }

        try
        {
            var result = _backupService.CreateExternalBackup(_backupSettings.ExternalBackupDirectory, "manual");
            AggiornaStatoBackup();
            Stato = $"Backup esterno creato: {Path.GetFileName(result.BackupPath)}";
            MessageBox.Show(
                $"Backup esterno creato in:\n{result.BackupPath}",
                "Backup esterno",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Backup esterno", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Backup esterno non riuscito.";
        }
    }

    private void ConfiguraCartellaBackupEsterno()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Seleziona la cartella per il backup esterno",
            InitialDirectory = Directory.Exists(_backupSettings.ExternalBackupDirectory)
                ? _backupSettings.ExternalBackupDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _backupSettings.ExternalBackupDirectory = dialog.FolderName;
        _backupService.SaveSettings(_backupSettings);
        AggiornaStatoBackup();
        Stato = $"Cartella backup esterno impostata: {_backupSettings.ExternalBackupDirectory}";

        try
        {
            var result = _backupService.CreateExternalBackup(_backupSettings.ExternalBackupDirectory, "configure-external");
            AggiornaStatoBackup();
            Stato = $"Cartella backup esterno impostata e primo backup creato: {Path.GetFileName(result.BackupPath)}";
            MessageBox.Show(
                $"Cartella backup esterno impostata.\n\nPrimo backup creato in:\n{result.BackupPath}",
                "Backup esterno",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AggiornaStatoBackup();
            MessageBox.Show(
                $"Cartella impostata, ma il primo backup esterno non e riuscito.\n\n{ex.Message}",
                "Backup esterno",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Stato = $"Cartella backup esterno impostata, ma primo backup non riuscito: {ex.Message}";
        }
    }

    private void RipristinaDaBackup()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona un backup SMZ da ripristinare",
            Filter = "Backup SMZ (*.smzbak)|*.smzbak|Archivio ZIP (*.zip)|*.zip|Tutti i file|*.*",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = GetBackupRestoreInitialDirectory(),
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var conferma = MessageBox.Show(
            $"Ripristinare il backup selezionato?\n\n{dialog.FileName}\n\nPrima del ripristino verra creato un backup di sicurezza locale del database attuale.",
            "Ripristina backup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (conferma != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var result = _backupService.RestoreBackup(dialog.FileName);
            RicaricaDatiApplicazioneDaDatabase();
            AggiornaStatoBackup();
            Stato = $"Backup ripristinato: {Path.GetFileName(result.RestoredBackupPath)}";
            MessageBox.Show(
                $"Ripristino completato.\n\nBackup applicato:\n{result.RestoredBackupPath}\n\nBackup di sicurezza creato prima del restore:\n{result.SafetyBackupPath}",
                "Ripristino backup",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ripristino backup", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Ripristino backup non riuscito.";
        }
    }

    private void EseguiBackupLocaleAutomaticoAvvio()
    {
        if (!_backupService.NeedsAutomaticLocalBackup())
        {
            return;
        }

        try
        {
            _backupService.CreateLocalBackup("startup-auto");
            EseguiBackupEsternoSilenzioso("startup-auto");
            AggiornaStatoBackup();
            Stato = "Backup locale automatico eseguito all'avvio.";
        }
        catch (Exception ex)
        {
            AggiornaStatoBackup();
            Stato = $"Avvio completato, ma il backup locale automatico non e riuscito: {ex.Message}";
        }
    }

    private void EseguiBackupLocaleSilenzioso(string reason)
    {
        try
        {
            _backupService.CreateLocalBackup(reason);
            EseguiBackupEsternoSilenzioso(reason);
            AggiornaStatoBackup();
        }
        catch (Exception ex)
        {
            AggiornaStatoBackup();
            Stato = $"{Stato} Backup locale non riuscito: {ex.Message}";
        }
    }

    private void EseguiBackupEsternoSilenzioso(string reason)
    {
        if (string.IsNullOrWhiteSpace(_backupSettings.ExternalBackupDirectory))
        {
            return;
        }

        try
        {
            _backupService.CreateExternalBackup(_backupSettings.ExternalBackupDirectory, reason);
        }
        catch (Exception ex)
        {
            Stato = $"{Stato} Backup esterno non riuscito: {ex.Message}";
        }
    }

    private string GetBackupRestoreInitialDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_backupSettings.ExternalBackupDirectory)
            && Directory.Exists(_backupSettings.ExternalBackupDirectory))
        {
            return _backupSettings.ExternalBackupDirectory;
        }

        if (Directory.Exists(DatabasePaths.LocalBackupDirectory))
        {
            return DatabasePaths.LocalBackupDirectory;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
