using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.App.Views;

public partial class CalendarioRepartoView : UserControl
{
    private CalendarioRepartoViewModel? _viewModel;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMinutes(1) };
    private DateTime _ultimoGiorno = DateTime.Today;

    public CalendarioRepartoView()
    {
        InitializeComponent();
        _timer.Tick += (_, _) =>
        {
            if (_ultimoGiorno == DateTime.Today) return;
            _ultimoGiorno = DateTime.Today;
            _viewModel?.Ricarica();
        };
        Unloaded += (_, _) => _timer.Stop();
    }

    private bool AccessoConsentito => DataContext is MainWindowViewModel { IsAdministrator: true };
    private void Calendario_Loaded(object sender, RoutedEventArgs e) => Aggiorna();
    private void Calendario_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible && IsLoaded) Aggiorna();
        else _timer.Stop();
    }

    private void Aggiorna()
    {
        if (!AccessoConsentito || !IsVisible) return;
        _viewModel ??= new CalendarioRepartoViewModel();
        CalendarioHost.DataContext = _viewModel;
        _viewModel.Ricarica();
        _timer.Start();
    }

    private void NuovoEvento_Click(object sender, RoutedEventArgs e) => Apri(new EventoCalendario { Data = _viewModel?.GiornoSelezionato ?? DateTime.Today });
    private void ApriEvento_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: EventoCalendario evento }) Apri(evento);
    }

    private void Apri(EventoCalendario evento)
    {
        if (!AccessoConsentito || _viewModel is null) return;
        var editor = new EventoCalendarioWindow(evento) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() == true) _viewModel.SelezionaData(editor.DataSalvata);
    }
}
