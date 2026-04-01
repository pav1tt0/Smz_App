using System.ComponentModel;
using System.Windows;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.App;

public partial class MainWindow : Window
{
    private readonly DiveAmbiencePlayer _diveAmbiencePlayer = new();
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateWelcomeAudio();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _diveAmbiencePlayer.Dispose();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsWelcomeVisible)
            || e.PropertyName == nameof(MainWindowViewModel.IsWelcomeAudioEnabled))
        {
            UpdateWelcomeAudio();
        }
    }

    private void UpdateWelcomeAudio()
    {
        if (_viewModel.IsWelcomeVisible && _viewModel.IsWelcomeAudioEnabled)
        {
            _diveAmbiencePlayer.Start();
            return;
        }

        _diveAmbiencePlayer.Stop();
    }
}
