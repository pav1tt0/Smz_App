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
    private void InizializzaContabilita()
    {
        AggiornaAnniContabilitaDisponibili();

        var oggi = DateTime.Today;
        _contabilitaAnnoSelezionato = ContabilitaAnniDisponibili.Contains(oggi.Year)
            ? oggi.Year
            : ContabilitaAnniDisponibili.FirstOrDefault();
        _contabilitaMeseSelezionato = ContabilitaMesiDisponibili.FirstOrDefault(item => item.NumeroMese == oggi.Month)
            ?? ContabilitaMesiDisponibili.FirstOrDefault();
        _contabilitaSelezionePronta = true;
        OnPropertyChanged(nameof(ContabilitaAnnoSelezionato));
        OnPropertyChanged(nameof(ContabilitaAnnoEffettivo));
        OnPropertyChanged(nameof(ContabilitaMeseSelezionato));
        OnPropertyChanged(nameof(ContabilitaPeriodoTitolo));
        OnPropertyChanged(nameof(RegistroImmersioniPeriodoTitolo));
    }

    private void InizializzaEditorTariffeContabili()
    {
        RegoleContabiliEditorItems.Clear();

        foreach (var regola in RegoleContabiliImmersioneCatalogo
                     .OrderBy(item => item.TipologiaImmersioneOperativaId)
                     .ThenBy(item => item.FasciaProfonditaId)
                     .ThenBy(item => item.CategoriaContabileOreId))
        {
            RegoleContabiliEditorItems.Add(new RegolaContabileEditorRowViewModel
            {
                RegolaContabileImmersioneId = regola.RegolaContabileImmersioneId,
                TipologiaDescrizione = TipologieImmersioneOperativeCatalogo.FirstOrDefault(item => item.TipologiaImmersioneOperativaId == regola.TipologiaImmersioneOperativaId)?.Descrizione ?? string.Empty,
                FasciaDescrizione = FasceProfonditaCatalogo.FirstOrDefault(item => item.FasciaProfonditaId == regola.FasciaProfonditaId)?.Descrizione ?? string.Empty,
                CategoriaDescrizione = CategorieContabiliOreCatalogo.FirstOrDefault(item => item.CategoriaContabileOreId == regola.CategoriaContabileOreId)?.Descrizione ?? string.Empty,
                Tariffa = FormatDecimal(regola.Tariffa),
                Attiva = regola.Attiva,
            });
        }

        OnPropertyChanged(nameof(TariffeContabiliStato));
        RegistraSnapshotTariffeContabili();
    }

    private void AggiornaAnniContabilitaDisponibili()
    {
        var anni = _repository.GetAnniServiziDisponibili();
        var annoCorrente = DateTime.Today.Year;
        var annoDaMantenere = _contabilitaAnnoSelezionato;

        if (!anni.Contains(annoCorrente))
        {
            anni.Add(annoCorrente);
        }

        anni = anni
            .Distinct()
            .OrderByDescending(item => item)
            .ToList();

        ContabilitaAnniDisponibili.Clear();
        foreach (var anno in anni)
        {
            ContabilitaAnniDisponibili.Add(anno);
        }

        _contabilitaAnnoSelezionato = annoDaMantenere > 0 && ContabilitaAnniDisponibili.Contains(annoDaMantenere)
            ? annoDaMantenere
            : ContabilitaAnniDisponibili.Contains(annoCorrente)
            ? annoCorrente
            : ContabilitaAnniDisponibili.FirstOrDefault();

        OnPropertyChanged(nameof(ContabilitaAnnoSelezionato));
        OnPropertyChanged(nameof(ContabilitaAnnoEffettivo));
        OnPropertyChanged(nameof(ContabilitaPeriodoTitolo));
        OnPropertyChanged(nameof(RegistroImmersioniPeriodoTitolo));
    }

    private void AggiornaDatiMensili()
    {
        CaricaContabilitaMensile();
        CaricaRegistroImmersioniMensile();
    }

    private void CaricaContabilitaMensile()
    {
        if (ContabilitaMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            return;
        }

        _elaborazioneMensileInfo = _repository.GetElaborazioneMensileInfo(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese);

        var snapshot = _elaborazioneMensileInfo is null
            ? _repository.GetContabilitaGiornateImpiego(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese)
            : _repository.GetElaborazioneMensileSnapshot(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese)
                ?? _repository.GetContabilitaGiornateImpiego(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese);

        _contabilitaSmzSource.Clear();
        _contabilitaSmzSource.AddRange(snapshot.SmzImmersioni);
        AggiornaOpzioniFiltriContabilitaSmz();
        ApplicaFiltriContabilitaSmz();

        ContabilitaSanitariItems.Clear();
        foreach (var item in snapshot.Sanitari)
        {
            ContabilitaSanitariItems.Add(item);
        }

        ContabilitaSupportiItems.Clear();
        foreach (var item in snapshot.SupportiOccasionali)
        {
            ContabilitaSupportiItems.Add(item);
        }

        AggiornaRiepilogoContabilita();
    }

    private void AggiornaOpzioniFiltriContabilitaSmz()
    {
        AggiornaFiltroContabilitaDisponibile(
            ContabilitaSmzDateDisponibili,
            _contabilitaSmzSource
                .Select(item => item.DataServizioDescrizione)
                .Distinct()
                .OrderBy(item => DateOnly.ParseExact(item, "dd/MM/yyyy", CultureInfo.InvariantCulture)),
            nameof(ContabilitaSmzFiltroData),
            ref _contabilitaSmzFiltroData);

        AggiornaFiltroContabilitaDisponibile(
            ContabilitaSmzNumeriServizioDisponibili,
            _contabilitaSmzSource
                .Select(item => item.NumeroOrdineServizio)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(item => item, StringComparer.CurrentCultureIgnoreCase),
            nameof(ContabilitaSmzFiltroNumeroServizio),
            ref _contabilitaSmzFiltroNumeroServizio);

        AggiornaFiltroContabilitaDisponibile(
            ContabilitaSmzApparatiDisponibili,
            _contabilitaSmzSource
                .Select(item => item.Apparato)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(item => item, StringComparer.CurrentCultureIgnoreCase),
            nameof(ContabilitaSmzFiltroApparato),
            ref _contabilitaSmzFiltroApparato);
    }

    private void AggiornaFiltroContabilitaDisponibile(
        ObservableCollection<string> target,
        IEnumerable<string> values,
        string propertyName,
        ref string selectedValue)
    {
        const string emptyValue = "";

        target.Clear();
        target.Add(emptyValue);
        foreach (var value in values)
        {
            target.Add(value);
        }

        var currentValue = selectedValue;
        if (!string.IsNullOrWhiteSpace(currentValue)
            && !target.Any(item => string.Equals(item, currentValue, StringComparison.CurrentCultureIgnoreCase)))
        {
            selectedValue = string.Empty;
            OnPropertyChanged(propertyName);
        }
    }

    private void ApplicaFiltriContabilitaSmz()
    {
        var data = ContabilitaSmzFiltroData.Trim();
        var numeroServizio = ContabilitaSmzFiltroNumeroServizio.Trim();
        var nominativo = ContabilitaSmzFiltroNominativo.Trim();
        var apparato = ContabilitaSmzFiltroApparato.Trim();

        var items = _contabilitaSmzSource.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(data))
        {
            items = items.Where(item => string.Equals(item.DataServizioDescrizione, data, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(numeroServizio))
        {
            items = items.Where(item => string.Equals(item.NumeroOrdineServizio, numeroServizio, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(nominativo))
        {
            items = items.Where(item => item.Nominativo.Contains(nominativo, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(apparato))
        {
            items = items.Where(item => string.Equals(item.Apparato, apparato, StringComparison.CurrentCultureIgnoreCase));
        }

        ContabilitaSmzItems.Clear();
        foreach (var item in items
                     .OrderBy(item => item.DataServizio)
                     .ThenBy(item => item.NumeroOrdineServizio)
                     .ThenBy(item => item.Cognome)
                     .ThenBy(item => item.Nome)
                     .ThenBy(item => item.Apparato)
                     .ThenBy(item => item.FasciaProfondita))
        {
            ContabilitaSmzItems.Add(item);
        }

        AggiornaRiepilogoContabilita();
        OnPropertyChanged(nameof(HasFiltriContabilitaSmzAttivi));
    }

    private void PulisciFiltriContabilitaSmz()
    {
        var changed = false;

        if (!string.IsNullOrWhiteSpace(_contabilitaSmzFiltroData))
        {
            _contabilitaSmzFiltroData = string.Empty;
            OnPropertyChanged(nameof(ContabilitaSmzFiltroData));
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(_contabilitaSmzFiltroNumeroServizio))
        {
            _contabilitaSmzFiltroNumeroServizio = string.Empty;
            OnPropertyChanged(nameof(ContabilitaSmzFiltroNumeroServizio));
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(_contabilitaSmzFiltroNominativo))
        {
            _contabilitaSmzFiltroNominativo = string.Empty;
            OnPropertyChanged(nameof(ContabilitaSmzFiltroNominativo));
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(_contabilitaSmzFiltroApparato))
        {
            _contabilitaSmzFiltroApparato = string.Empty;
            OnPropertyChanged(nameof(ContabilitaSmzFiltroApparato));
            changed = true;
        }

        if (changed)
        {
            ApplicaFiltriContabilitaSmz();
        }
    }

    private void CaricaRegistroImmersioniMensile()
    {
        if (ContabilitaMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            return;
        }

        var items = _repository.GetRegistroImmersioniMensile(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese);

        RegistroImmersioniItems.Clear();
        foreach (var item in items)
        {
            RegistroImmersioniItems.Add(item);
        }

        var categorie = items
            .GroupBy(item => item.CategoriaRegistro)
            .OrderBy(group =>
                CategorieRegistroCatalogo.FirstOrDefault(item =>
                    string.Equals(item.Descrizione, group.Key, StringComparison.OrdinalIgnoreCase))?.Ordine ?? int.MaxValue)
            .ThenBy(group => group.Key)
            .Select(group => new RegistroImmersioneCategoriaSummary
            {
                CategoriaRegistro = group.Key,
                ImmersioniTotali = group.Select(item => item.ServizioImmersioneId).Distinct().Count(),
                RigheOperatoreTotali = group.Count(),
                OreTotali = group.Sum(item => item.OreImmersione),
            })
            .ToList();

        RegistroImmersioniCategorieItems.Clear();
        foreach (var item in categorie)
        {
            RegistroImmersioniCategorieItems.Add(item);
        }

        AggiornaRiepilogoRegistroImmersioni();
    }

    private void AggiornaRiepilogoContabilita()
    {
        OnPropertyChanged(nameof(ContabilitaPeriodoTitolo));
        OnPropertyChanged(nameof(ContabilitaStato));
        OnPropertyChanged(nameof(ContabilitaSmzTotaleRighe));
        OnPropertyChanged(nameof(ContabilitaSmzTotaleOre));
        OnPropertyChanged(nameof(ContabilitaSmzTotaleImporti));
        OnPropertyChanged(nameof(ContabilitaSmzTotaleOreDisplay));
        OnPropertyChanged(nameof(ContabilitaSmzTotaleImportiDisplay));
        OnPropertyChanged(nameof(ContabilitaSmzStato));
        OnPropertyChanged(nameof(HasFiltriContabilitaSmzAttivi));
        OnPropertyChanged(nameof(ContabilitaSanitariTotalePersone));
        OnPropertyChanged(nameof(ContabilitaSanitariTotaleGiornate));
        OnPropertyChanged(nameof(ContabilitaSupportoTotalePersone));
        OnPropertyChanged(nameof(ContabilitaSupportoTotaleGiornate));
        OnPropertyChanged(nameof(ContabilitaSanitariStato));
        OnPropertyChanged(nameof(ContabilitaSupportoStato));
        OnPropertyChanged(nameof(TariffeContabiliStato));
        OnPropertyChanged(nameof(ElaborazioneMensileStato));
        OnPropertyChanged(nameof(SalvaElaborazioneMensileLabel));
    }

    private void AggiornaRiepilogoRegistroImmersioni()
    {
        OnPropertyChanged(nameof(RegistroImmersioniPeriodoTitolo));
        OnPropertyChanged(nameof(RegistroImmersioniTotaleRighe));
        OnPropertyChanged(nameof(RegistroImmersioniTotaleImmersioni));
        OnPropertyChanged(nameof(RegistroImmersioniTotaleOperatori));
        OnPropertyChanged(nameof(RegistroImmersioniTotaleOre));
        OnPropertyChanged(nameof(RegistroImmersioniTotaleOreDisplay));
        OnPropertyChanged(nameof(RegistroImmersioniStato));
        OnPropertyChanged(nameof(RegistroImmersioniCategorieStato));
    }

    private void EntraNellApp()
    {
        IsWelcomeVisible = false;
        SezioneAttivaIndex = HomeSectionIndex;
        Stato = "Home iniziale caricata.";
    }

    private void ToggleWelcomeAudio()
    {
        IsWelcomeAudioEnabled = !IsWelcomeAudioEnabled;
        Stato = IsWelcomeAudioEnabled
            ? "Audio welcome attivato."
            : "Audio welcome disattivato.";
    }

    private void SalvaElaborazioneMensile()
    {
        if (ContabilitaMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            return;
        }

        try
        {
            if (_elaborazioneMensileInfo is not null)
            {
                var result = MessageBox.Show(
                    $"Esiste gia una chiusura per {ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoSelezionato}. Vuoi rigenerarla con i dati correnti?",
                    "Elaborazione mensile",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    Stato = "Rigenerazione elaborazione mensile annullata.";
                    return;
                }
            }

            var snapshot = _repository.GetContabilitaGiornateImpiego(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese);
            _repository.SaveElaborazioneMensile(
                ContabilitaAnnoSelezionato,
                ContabilitaMeseSelezionato.NumeroMese,
                snapshot,
                $"Snapshot amministrativo {ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoSelezionato}");

            CaricaContabilitaMensile();
            Stato = $"Elaborazione mensile registrata per {ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoSelezionato}.";
            EseguiBackupLocaleSilenzioso("save-monthly-snapshot");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Elaborazione mensile", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Salvataggio elaborazione mensile non riuscito.";
        }
    }

    private void EsportaContabilitaCsv()
    {
        if (ContabilitaMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            return;
        }

        if (_elaborazioneMensileInfo is null)
        {
            MessageBox.Show(
                "Chiudi prima il mese con \"Chiudi mese\". L'export CSV deve partire da uno snapshot congelato da inviare ai pagamenti.",
                "Export contabilita CSV",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Stato = "Export CSV non eseguito: manca la chiusura mensile.";
            return;
        }

        try
        {
            Directory.CreateDirectory(DatabasePaths.ExportDirectory);

            var fileName = $"contabilita-smz-{ContabilitaAnnoSelezionato:D4}-{ContabilitaMeseSelezionato.NumeroMese:D2}.csv";
            var filePath = Path.Combine(DatabasePaths.ExportDirectory, fileName);
            var builder = new StringBuilder();

            builder.AppendLine("Periodo;Data;Ordine;PerID;Qual;Cognome e Nome;Appar.;Prof.;Tariffa;ORE ORD;ORE ADD;ORE SPER;ORE C.I.;Importo;Med. Rag.;TOTALE");

            var periodo = $"{ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoSelezionato}";
            foreach (var item in ContabilitaSmzItems
                         .OrderBy(x => x.Cognome)
                         .ThenBy(x => x.Nome)
                         .ThenBy(x => x.DataServizio)
                         .ThenBy(x => x.NumeroOrdineServizio)
                         .ThenBy(x => x.Apparato)
                         .ThenBy(x => x.FasciaProfondita))
            {
                builder.AppendLine(string.Join(";",
                    Csv(periodo),
                    Csv(item.DataServizioDescrizione),
                    Csv(item.NumeroOrdineServizio),
                    Csv(item.PerId),
                    Csv(item.Qualifica),
                    Csv(item.Nominativo),
                    Csv(item.Apparato),
                    Csv(item.FasciaProfondita),
                    Csv(item.TariffaDisplay),
                    Csv(item.OreOrdDisplay),
                    Csv(item.OreAddDisplay),
                    Csv(item.OreSperDisplay),
                    Csv(item.OreCiDisplay),
                    Csv(item.ImportoDisplay),
                    Csv(string.Empty),
                    Csv(item.ImportoDisplay)));
            }

            File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            Stato = $"Export CSV creato: {filePath}";
            MessageBox.Show(
                $"Export contabilita creato in:\n{filePath}",
                "Export contabilita CSV",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export contabilita CSV", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Export contabilita CSV non riuscito.";
        }
    }

    private void SalvaLocalitaOperative()
    {
        try
        {
            _repository.UpdateLocalitaOperative(LocalitaOperativeCatalogo);
            RicaricaCataloghiServizio(preserveSelections: true);
            Stato = "Localita operative aggiornate.";
            EseguiBackupLocaleSilenzioso("save-service-locations");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Localita operative", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Aggiornamento localita operative non riuscito.";
        }
    }

    private void SalvaUnitaNavali()
    {
        try
        {
            _repository.UpdateUnitaNavali(UnitaNavaliGestioneCatalogo);
            RicaricaCataloghiServizio(preserveSelections: true);
            Stato = "Mezzi nautici aggiornati.";
            EseguiBackupLocaleSilenzioso("save-service-vessels");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Mezzi nautici", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Aggiornamento mezzi nautici non riuscito.";
        }
    }

    private void SalvaTariffeContabili()
    {
        try
        {
            var regoleAggiornate = new List<RegolaContabileImmersione>();

            foreach (var row in RegoleContabiliEditorItems)
            {
                var tariffa = ParseNullableDecimal(row.Tariffa, $"Tariffa {row.TipologiaDescrizione} {row.FasciaDescrizione} {row.CategoriaDescrizione}")
                    ?? throw new InvalidOperationException($"Tariffa mancante per {row.TipologiaDescrizione} {row.FasciaDescrizione} {row.CategoriaDescrizione}.");

                var regola = RegoleContabiliImmersioneCatalogo.FirstOrDefault(item => item.RegolaContabileImmersioneId == row.RegolaContabileImmersioneId)
                    ?? throw new InvalidOperationException($"Regola tariffaria {row.RegolaContabileImmersioneId} non trovata.");

                regola.Tariffa = tariffa;
                regola.Attiva = row.Attiva;
                regoleAggiornate.Add(regola);
            }

            _repository.UpdateRegoleContabiliImmersione(regoleAggiornate);

            foreach (var immersione in ServizioImmersioniBozza)
            {
                foreach (var partecipazione in immersione.Partecipazioni)
                {
                    AggiornaCalcoliPartecipazioneImmersione(partecipazione);
                }
            }

            CaricaContabilitaMensile();
            RegistraSnapshotTariffeContabili();
            Stato = "Tariffe contabili aggiornate nel database.";
            EseguiBackupLocaleSilenzioso("save-accounting-rules");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Tariffe contabili", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Aggiornamento tariffe non riuscito.";
        }
    }
}
