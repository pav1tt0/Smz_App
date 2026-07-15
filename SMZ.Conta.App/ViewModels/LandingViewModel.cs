using System.Reflection;
using SMZ.Conta.App.Infrastructure;

namespace SMZ.Conta.App.ViewModels;

public sealed class LandingViewModel : ObservableObject
{
    private bool _isWelcomeAudioEnabled = true;

    public LandingViewModel(Action enterApplication)
    {
        EnterAppCommand = new RelayCommand(enterApplication);
        ToggleWelcomeAudioCommand = new RelayCommand(() => IsWelcomeAudioEnabled = !IsWelcomeAudioEnabled);
    }

    public RelayCommand EnterAppCommand { get; }
    public RelayCommand ToggleWelcomeAudioCommand { get; }
    public bool IsWelcomeVisible => true;

    public bool IsWelcomeAudioEnabled
    {
        get => _isWelcomeAudioEnabled;
        set
        {
            if (SetProperty(ref _isWelcomeAudioEnabled, value))
            {
                OnPropertyChanged(nameof(WelcomeAudioTooltip));
            }
        }
    }

    public string WelcomeAudioTooltip =>
        IsWelcomeAudioEnabled ? "Disattiva audio welcome" : "Attiva audio welcome";

    public string WelcomeCredits => "© Nucleo SMZ La Spezia 2026 - Sviluppato da Paolo Vittori e Codex";

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
