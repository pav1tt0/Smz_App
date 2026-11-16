using SMZ.Conta.App.Infrastructure;

namespace SMZ.Conta.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private string _serviziDataDal = string.Empty;
    private string _serviziDataAl = string.Empty;
    private int _serviziPeriodoModalita;
    private int _serviziMeseFiltro = DateTime.Today.Month;
    private int _serviziAnnoFiltro = DateTime.Today.Year;
    private string _serviziGiornoFiltro = DateTime.Today.ToString("dd/MM/yyyy");
    private string _registroDataDal = string.Empty;
    private string _registroDataAl = string.Empty;

    // The visible mode alone determines the date constraint; hidden fields
    // retain their values without contributing to the active search.
    public int ServiziPeriodoModalita
    {
        get => _serviziPeriodoModalita;
        set
        {
            if (value is < 0 or > 2 || !SetProperty(ref _serviziPeriodoModalita, value)) return;
            if (value == 2 && string.IsNullOrEmpty(_serviziDataDal) && string.IsNullOrEmpty(_serviziDataAl))
            {
                var inizio = new DateOnly(ServiziAnnoFiltro, ServiziMeseFiltro, 1);
                _serviziDataDal = inizio.ToString("dd/MM/yyyy");
                _serviziDataAl = inizio.AddDays(DateTime.DaysInMonth(inizio.Year, inizio.Month) - 1).ToString("dd/MM/yyyy");
                OnPropertyChanged(nameof(ServiziDataDal));
                OnPropertyChanged(nameof(ServiziDataAl));
            }
            CaricaServiziSalvati();
        }
    }

    public int ServiziMeseFiltro
    {
        get => _serviziMeseFiltro;
        set { if (value is >= 1 and <= 12 && SetProperty(ref _serviziMeseFiltro, value)) CaricaServiziSalvati(); }
    }

    public int ServiziAnnoFiltro
    {
        get => _serviziAnnoFiltro;
        set { if (value is >= 1 and <= 9999 && SetProperty(ref _serviziAnnoFiltro, value)) CaricaServiziSalvati(); }
    }

    public string ServiziGiornoFiltro
    {
        get => _serviziGiornoFiltro;
        set { if (SetProperty(ref _serviziGiornoFiltro, value)) CaricaServiziSalvati(); }
    }

    public string ServiziDataDal
    {
        get => _serviziDataDal;
        set { if (SetProperty(ref _serviziDataDal, value)) CaricaServiziSalvati(); }
    }

    public string ServiziDataAl
    {
        get => _serviziDataAl;
        set { if (SetProperty(ref _serviziDataAl, value)) CaricaServiziSalvati(); }
    }

    public string RegistroDataDal
    {
        get => _registroDataDal;
        set { if (SetProperty(ref _registroDataDal, value)) CaricaRegistroImmersioniMensile(); }
    }

    public string RegistroDataAl
    {
        get => _registroDataAl;
        set { if (SetProperty(ref _registroDataAl, value)) CaricaRegistroImmersioniMensile(); }
    }

    public RelayCommand ResetPeriodoRegistroCommand => new(() =>
    {
        _registroDataDal = _registroDataAl = string.Empty;
        OnPropertyChanged(nameof(RegistroDataDal));
        OnPropertyChanged(nameof(RegistroDataAl));
        CaricaRegistroImmersioniMensile();
    });

    private bool TryGetPeriodoServizi(out DateOnly inizio, out DateOnly fine, out string errore)
    {
        if (ServiziPeriodoModalita == 2)
            return TryGetIntervallo(ServiziDataDal, ServiziDataAl, out inizio, out fine, out errore);
        if (ServiziPeriodoModalita == 1)
        {
            var giorno = TryParseDate(ServiziGiornoFiltro);
            inizio = fine = giorno ?? default;
            errore = giorno is null ? "Seleziona una data valida (gg/mm/aaaa)." : string.Empty;
            return giorno is not null;
        }
        inizio = new DateOnly(ServiziAnnoFiltro, ServiziMeseFiltro, 1);
        fine = inizio.AddDays(DateTime.DaysInMonth(inizio.Year, inizio.Month) - 1);
        errore = string.Empty;
        return true;
    }

    private void RipristinaPeriodoServizi()
    {
        _serviziPeriodoModalita = 0;
        _serviziMeseFiltro = DateTime.Today.Month;
        _serviziAnnoFiltro = DateTime.Today.Year;
        _serviziGiornoFiltro = DateTime.Today.ToString("dd/MM/yyyy");
        _serviziDataDal = _serviziDataAl = string.Empty;
        foreach (var property in new[] { nameof(ServiziPeriodoModalita), nameof(ServiziMeseFiltro), nameof(ServiziAnnoFiltro),
            nameof(ServiziGiornoFiltro), nameof(ServiziDataDal), nameof(ServiziDataAl) }) OnPropertyChanged(property);
    }

    private bool TryGetPeriodoRegistro(out DateOnly inizio, out DateOnly fine, out string errore)
    {
        if (!string.IsNullOrWhiteSpace(RegistroDataDal) || !string.IsNullOrWhiteSpace(RegistroDataAl))
            return TryGetIntervallo(RegistroDataDal, RegistroDataAl, out inizio, out fine, out errore);
        inizio = new DateOnly(ContabilitaAnnoEffettivo, ContabilitaMeseSelezionato?.NumeroMese ?? DateTime.Today.Month, 1);
        fine = new DateOnly(inizio.Year, inizio.Month, DateTime.DaysInMonth(inizio.Year, inizio.Month));
        errore = string.Empty;
        return true;
    }

    private static bool TryGetIntervallo(string dal, string al, out DateOnly inizio, out DateOnly fine, out string errore)
    {
        var prima = TryParseDate(dal);
        var ultima = TryParseDate(al);
        inizio = prima ?? default;
        fine = ultima ?? default;
        errore = prima is null || ultima is null
            ? "Completa entrambe le date Dal e Al (gg/mm/aaaa)."
            : inizio > fine ? "La data Dal deve precedere o coincidere con la data Al." : string.Empty;
        return errore.Length == 0;
    }
}
