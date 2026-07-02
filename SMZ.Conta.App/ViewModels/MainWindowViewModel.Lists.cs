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
    private void CaricaElenco()
    {
        try
        {
            var ricercaPerNomeAttiva = !string.IsNullOrWhiteSpace(FiltroCognome);
            var filtroAbilitazioneEffettivo = ricercaPerNomeAttiva ? null : FiltroAbilitazione?.TipoAbilitazioneId;

            var items = _repository.SearchPersonale(
                FiltroCognome,
                filtroAbilitazioneEffettivo,
                ParseDate(FiltroVisiteEntro, "Filtro visite entro"));

            PersonaleItems.Clear();
            foreach (var personale in items)
            {
                PersonaleItems.Add(PersonaleListItemViewModel.FromModel(personale));
            }

            if (PersonaleItems.Count == 1)
            {
                SelectedPersonale = PersonaleItems[0];
            }

            IsSearchSuggestionsOpen = false;
            Stato = ricercaPerNomeAttiva && FiltroAbilitazione?.TipoAbilitazioneId is not null
                ? $"{PersonaleItems.Count} dipendenti trovati. Ricerca per cognome/nome attiva: filtro abilitazione sospeso."
                : $"{PersonaleItems.Count} dipendenti trovati";
            OnPropertyChanged(nameof(DashboardDipendentiTotali));
            OnPropertyChanged(nameof(DashboardStatoSintesi));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ricerca personale", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Errore nella ricerca";
        }
    }

    private void AggiornaScadenziario()
    {
        var oggi = DateOnly.FromDateTime(DateTime.Today);
        var finoA = oggi.AddDays(90);
        var items = _repository.GetScadenzeProssime(oggi, finoA);
        var scadenzeViewModel = items
            .Select(ScadenzaItemViewModel.FromModel)
            .Where(ApplicaFiltroScadenziario)
            .OrderBy(item => item.IsExpired ? 0 : item.IsUrgent ? 1 : 2)
            .ThenBy(item => item.IsExpired ? Math.Abs(item.GiorniResiduiNumero) : item.GiorniResiduiNumero)
            .ThenBy(item => item.Nominativo)
            .ToList();

        _scadenzeTotali = scadenzeViewModel.Count;
        _scadenzeScadute = scadenzeViewModel.Count(item => item.IsExpired);
        _scadenzeUrgenti = scadenzeViewModel.Count(item => item.IsUrgent);

        ScadenzeProssime.Clear();
        foreach (var item in scadenzeViewModel)
        {
            ScadenzeProssime.Add(item);
        }

        OnPropertyChanged(nameof(ScadenzeTotali));
        OnPropertyChanged(nameof(ScadenzeScadute));
        OnPropertyChanged(nameof(ScadenzeUrgenti));
        OnPropertyChanged(nameof(DashboardScadenzeAperte));
        OnPropertyChanged(nameof(DashboardScadenzeSintesi));
        OnPropertyChanged(nameof(DashboardStatoSintesi));
        OnPropertyChanged(nameof(DashboardTopScadenze));
        OnPropertyChanged(nameof(DashboardTopScadenzeTitolo));
        OnPropertyChanged(nameof(DashboardCriticitaItems));
        OnPropertyChanged(nameof(HasDashboardCriticitaOperative));
        OnPropertyChanged(nameof(DashboardCriticitaOperativeEmptyText));
        OnPropertyChanged(nameof(DashboardVisiteScadutePersonale));
        OnPropertyChanged(nameof(DashboardVisiteInScadenzaPersonale));
    }

    private void ApriScadenza(ScadenzaItemViewModel item)
    {
        var personaleListItem = PersonaleItems.FirstOrDefault(entry => entry.PerId == item.PerId);

        if (personaleListItem is null)
        {
            CaricaPersonale(item.PerId);
        }
        else
        {
            SelectedPersonale = personaleListItem;
        }

        if (string.Equals(item.Origine, "Visita medica", StringComparison.OrdinalIgnoreCase))
        {
            SchedaDettaglioTabIndex = 1;
            SelectedVisita = VisiteMediche.FirstOrDefault(row =>
                string.Equals(row.TipoVisita, item.Titolo, StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.DataScadenza, item.DataScadenza, StringComparison.OrdinalIgnoreCase))
                ?? VisiteMediche.FirstOrDefault(row => string.Equals(row.TipoVisita, item.Titolo, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            SchedaDettaglioTabIndex = 0;
            SelectedAbilitazione = Abilitazioni.FirstOrDefault(row =>
                string.Equals(row.TipoDescrizione, item.Titolo, StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.DataScadenza, item.DataScadenza, StringComparison.OrdinalIgnoreCase))
                ?? Abilitazioni.FirstOrDefault(row => string.Equals(row.TipoDescrizione, item.Titolo, StringComparison.OrdinalIgnoreCase));
        }

        SezioneAttivaIndex = PersonalSectionIndex;
        IsSchedaPersonaleVisibile = true;
    }

    private void ApriScadenzaDaParametro(object? parameter)
    {
        if (parameter is ScadenzaItemViewModel item)
        {
            ApriScadenza(item);
        }
    }

    private bool ApplicaFiltroScadenziario(ScadenzaItemViewModel item)
    {
        return FiltroScadenzeSelezionato switch
        {
            "Solo visite" => string.Equals(item.Origine, "Visita medica", StringComparison.OrdinalIgnoreCase),
            "Solo abilitazioni" => string.Equals(item.Origine, "Abilitazione", StringComparison.OrdinalIgnoreCase),
            _ => true,
        };
    }

    public int ScadenzeTotali => _scadenzeTotali;

    public int ScadenzeScadute => _scadenzeScadute;

    public int ScadenzeUrgenti => _scadenzeUrgenti;

    public string ArchivioTitolo => _archivioDettaglio?.NominativoCompleto ?? "Seleziona una scheda archiviata";

    public string ArchivioPerId => _archivioDettaglio is null
        ? "PerID originario non disponibile"
        : $"PerID originario {_archivioDettaglio.PerIdOriginale}";

    public string ArchivioCodiceFiscale => string.IsNullOrWhiteSpace(_archivioDettaglio?.CodiceFiscale)
        ? "Codice fiscale non disponibile"
        : _archivioDettaglio.CodiceFiscale;

    public string ArchivioDataArchiviazione => _archivioDettaglio is null
        ? "Data archiviazione non disponibile"
        : $"Archiviata il {_archivioDettaglio.DataArchiviazione:dd/MM/yyyy HH:mm}";

    public int ArchivioAbilitazioniTotali => ArchivioAbilitazioni.Count;

    public int ArchivioVisiteTotali => ArchivioVisiteMediche.Count;

    public string ArchivioContatti
    {
        get
        {
            if (_archivioDettaglio is null)
            {
                return "Contatti non disponibili";
            }

            return string.IsNullOrWhiteSpace(_archivioDettaglio.ContattiSintesi)
                ? "Contatti non disponibili"
                : _archivioDettaglio.ContattiSintesi;
        }
    }

    public string ArchivioAnagraficaSintesi
    {
        get
        {
            if (_archivioDettaglio is null)
            {
                return "Seleziona una scheda per vedere i dettagli archiviati.";
            }

            var parti = new List<string>();

            if (_archivioDettaglio.DataNascita is not null)
            {
                parti.Add($"Nata/o il {_archivioDettaglio.DataNascita.Value:dd/MM/yyyy}");
            }

            if (!string.IsNullOrWhiteSpace(_archivioDettaglio.LuogoNascita))
            {
                parti.Add(_archivioDettaglio.LuogoNascita);
            }

            if (!string.IsNullOrWhiteSpace(_archivioDettaglio.Qualifica))
            {
                parti.Add(_archivioDettaglio.Qualifica);
            }

            if (!string.IsNullOrWhiteSpace(_archivioDettaglio.ProfiloPersonale))
            {
                parti.Add(_archivioDettaglio.IsProfiloSanitario && !string.IsNullOrWhiteSpace(_archivioDettaglio.RuoloSanitario)
                    ? $"{_archivioDettaglio.ProfiloPersonale} - {_archivioDettaglio.RuoloSanitario}"
                    : _archivioDettaglio.ProfiloPersonale);
            }

            if (!string.IsNullOrWhiteSpace(_archivioDettaglio.NumeroBrevettoSmz))
            {
                parti.Add($"Brevetto subacqueo {_archivioDettaglio.NumeroBrevettoSmz}");
            }

            if (!string.IsNullOrWhiteSpace(_archivioDettaglio.IndirizzoResidenzaCompleto))
            {
                parti.Add(_archivioDettaglio.IndirizzoResidenzaCompleto);
            }

            return parti.Count == 0 ? "Nessun dato anagrafico aggiuntivo." : string.Join(" | ", parti);
        }
    }

    private void CaricaArchivio()
    {
        var items = _repository.GetArchivio();
        var selectedArchiveId = SelectedArchivio?.PersonaleArchivioId;

        ArchivioItems.Clear();
        foreach (var item in items)
        {
            ArchivioItems.Add(PersonaleArchivioListItemViewModel.FromModel(item));
        }

        if (ArchivioItems.Count == 0)
        {
            SelectedArchivio = null;
            OnPropertyChanged(nameof(DashboardArchivioSintesi));
            return;
        }

        SelectedArchivio = ArchivioItems.FirstOrDefault(item => item.PersonaleArchivioId == selectedArchiveId) ?? ArchivioItems[0];
        OnPropertyChanged(nameof(DashboardArchivioSintesi));
    }

    private void CaricaDettaglioArchivio(long archiveId)
    {
        var archivio = _repository.GetArchivioById(archiveId);
        if (archivio is null)
        {
            PulisciDettaglioArchivio();
            return;
        }

        _archivioDettaglio = archivio;

        ArchivioAbilitazioni.Clear();
        foreach (var abilitazione in archivio.Abilitazioni)
        {
            ArchivioAbilitazioni.Add(PersonaleAbilitazioneRowViewModel.FromModel(abilitazione));
        }

        ArchivioVisiteMediche.Clear();
        foreach (var visita in archivio.VisiteMediche)
        {
            ArchivioVisiteMediche.Add(VisitaMedicaRowViewModel.FromModel(visita));
        }

        AggiornaDettaglioArchivio();
    }

    private void PulisciDettaglioArchivio()
    {
        _archivioDettaglio = null;
        ArchivioAbilitazioni.Clear();
        ArchivioVisiteMediche.Clear();
        AggiornaDettaglioArchivio();
    }

    private void AggiornaDettaglioArchivio()
    {
        OnPropertyChanged(nameof(ArchivioTitolo));
        OnPropertyChanged(nameof(ArchivioPerId));
        OnPropertyChanged(nameof(ArchivioCodiceFiscale));
        OnPropertyChanged(nameof(ArchivioDataArchiviazione));
        OnPropertyChanged(nameof(ArchivioAbilitazioniTotali));
        OnPropertyChanged(nameof(ArchivioVisiteTotali));
        OnPropertyChanged(nameof(ArchivioContatti));
        OnPropertyChanged(nameof(ArchivioAnagraficaSintesi));
    }

    private void CaricaServiziSalvati(long? selectedServizioId = null)
    {
        var meseFiltro = TryParseMonthFilter(ServiziDataSearchText);
        var dataFiltro = meseFiltro is null ? TryParseDate(ServiziDataSearchText) : null;
        var dataFiltroDb = dataFiltro is not null
            ? dataFiltro.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : string.Empty;
        var dataInizioDb = string.Empty;
        var dataFineDb = string.Empty;
        if (meseFiltro is not null)
        {
            var dataInizio = new DateOnly(meseFiltro.Value.Year, meseFiltro.Value.Month, 1);
            var dataFine = dataInizio.AddMonths(1).AddDays(-1);
            dataInizioDb = dataInizio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            dataFineDb = dataFine.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        var hasSearch = !string.IsNullOrWhiteSpace(ServiziSearchText)
            || !string.IsNullOrWhiteSpace(ServiziNumeroSearchText)
            || !string.IsNullOrWhiteSpace(dataFiltroDb)
            || !string.IsNullOrWhiteSpace(dataInizioDb);
        var items = _repository.GetServiziGiornalieriRecenti(
            maxItems: hasSearch ? 100 : 10,
            searchText: ServiziSearchText,
            numeroServizio: ServiziNumeroSearchText,
            dataServizio: dataFiltroDb,
            dataInizio: dataInizioDb,
            dataFine: dataFineDb);
        var selectedId = selectedServizioId ?? SelectedServizioSalvato?.ServizioGiornalieroId;

        ServiziSalvati.Clear();
        foreach (var item in items)
        {
            ServiziSalvati.Add(item);
        }

        SelectedServizioSalvato = selectedId is null
            ? null
            : ServiziSalvati.FirstOrDefault(item => item.ServizioGiornalieroId == selectedId.Value);

        OnPropertyChanged(nameof(ServiziSalvatiStato));
    }

    private void PulisciRicercaServizi()
    {
        _serviziSearchText = string.Empty;
        _serviziNumeroSearchText = string.Empty;
        _serviziDataSearchText = string.Empty;
        OnPropertyChanged(nameof(ServiziSearchText));
        OnPropertyChanged(nameof(ServiziNumeroSearchText));
        OnPropertyChanged(nameof(ServiziDataSearchText));
        CaricaServiziSalvati();
    }
}
