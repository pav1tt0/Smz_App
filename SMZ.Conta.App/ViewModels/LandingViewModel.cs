using System.Reflection;
using SMZ.Conta.App.Infrastructure;

namespace SMZ.Conta.App.ViewModels;

public sealed class LandingViewModel : ObservableObject
{
    public LandingViewModel(Action enterApplication)
    {
        EnterAppCommand = new RelayCommand(enterApplication);
    }

    public RelayCommand EnterAppCommand { get; }
    public bool IsWelcomeVisible => true;

    public string WelcomeCredits => "© Nucleo SMZ La Spezia 2026 - Sviluppato da Paolo Vittori, Cesare Scapazzoni e Codex";

    public string WelcomeVersione
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version is null
                ? "Versione 1.0.0"
                : $"Versione {version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
        }
    }
}
