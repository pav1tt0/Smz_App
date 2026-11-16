using System.Collections.ObjectModel;
using System.Globalization;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class CalendarioRepartoViewModel : ObservableObject
{
    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");
    private readonly PersonaleRepository _repository = new();
    private DateTime _mese = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _giorno = DateTime.Today;
    private string _errore = string.Empty;
    private IReadOnlyList<EventoCalendario> _eventi = [];

    public CalendarioRepartoViewModel()
    {
        MesePrecedenteCommand = new RelayCommand(() => CambiaMese(-1), () => Mese > DateTime.MinValue);
        MeseSuccessivoCommand = new RelayCommand(() => CambiaMese(1), () => Mese.Year < 9999 || Mese.Month < 12);
        OggiCommand = new RelayCommand(() => SelezionaData(DateTime.Today));
        SelezionaGiornoCommand = new RelayCommand(p => { if (p is DateTime data) SelezionaData(data); });
    }

    public ObservableCollection<GiornoCalendario> Giorni { get; } = [];
    public ObservableCollection<EventoCalendario> EventiGiorno { get; } = [];
    public DateTime Mese => _mese;
    public DateTime GiornoSelezionato => _giorno;
    public string TitoloMese => Italiano.TextInfo.ToTitleCase(Mese.ToString("MMMM yyyy", Italiano));
    public string TitoloGiorno => _giorno.ToString("dddd d MMMM yyyy", Italiano);
    public string Errore { get => _errore; private set => SetProperty(ref _errore, value); }
    public RelayCommand MesePrecedenteCommand { get; }
    public RelayCommand MeseSuccessivoCommand { get; }
    public RelayCommand OggiCommand { get; }
    public RelayCommand SelezionaGiornoCommand { get; }

    public void SelezionaData(DateTime data) => CaricaData(data.Date);

    private void CambiaMese(int delta)
    {
        var mese = Mese.AddMonths(delta);
        SelezionaData(new DateTime(mese.Year, mese.Month, Math.Min(_giorno.Day, DateTime.DaysInMonth(mese.Year, mese.Month))));
    }

    public void Ricarica() => CaricaData(_giorno);

    private void CaricaData(DateTime selezione)
    {
        try
        {
            var mese = new DateTime(selezione.Year, selezione.Month, 1);
            var offset = ((int)mese.DayOfWeek + 6) % 7;
            var inizio = mese.AddDays(-Math.Min(offset, (mese - DateTime.MinValue).Days));
            var fine = inizio.AddDays(Math.Min(41, (DateTime.MaxValue.Date - inizio).Days));
            var eventi = _repository.GetEventiCalendario(inizio, fine);
            _giorno = selezione;
            _mese = mese;
            _eventi = eventi;
            Giorni.Clear();
            for (var i = 0; i < 42; i++)
            {
                DateTime? data = i <= (DateTime.MaxValue.Date - inizio).Days ? inizio.AddDays(i) : null;
                Giorni.Add(new GiornoCalendario(data, Mese, _giorno, data is { } d ? _eventi.Where(e => e.Data == d).ToList() : []));
            }
            EventiGiorno.Clear();
            foreach (var evento in _eventi.Where(e => e.Data == _giorno)) EventiGiorno.Add(evento);
            Errore = string.Empty;
        }
        catch (Exception ex)
        {
            Errore = $"Impossibile aggiornare il calendario: {ex.Message}";
        }
        OnPropertyChanged(nameof(TitoloMese));
        OnPropertyChanged(nameof(TitoloGiorno));
        MesePrecedenteCommand.RaiseCanExecuteChanged();
        MeseSuccessivoCommand.RaiseCanExecuteChanged();
    }
}

public sealed class GiornoCalendario(DateTime? data, DateTime mese, DateTime selezione, IReadOnlyList<EventoCalendario> eventi)
{
    public DateTime? Data { get; } = data;
    public string Numero => Data?.Day.ToString() ?? string.Empty;
    public bool IsOggi => Data == DateTime.Today;
    public bool IsSelezionato => Data == selezione;
    public double Opacita => Data?.Month == mese.Month ? 1 : 0.5;
    public string Descrizione => Data is { } d ? $"{d:dd/MM/yyyy}: {eventi.Count} eventi" : string.Empty;
    public IReadOnlyList<string> Indicatori => eventi.Select(e => e.Colore).Distinct().ToList();
}
