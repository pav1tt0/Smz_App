using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
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
        _reportPersonaleMeseSelezionato = ReportPersonaleMesiDisponibili.FirstOrDefault(item => item.NumeroMese == oggi.Month)
            ?? ReportPersonaleMesiDisponibili.FirstOrDefault();
        _contabilitaSelezionePronta = true;
        OnPropertyChanged(nameof(ContabilitaAnnoSelezionato));
        OnPropertyChanged(nameof(ContabilitaAnnoEffettivo));
        OnPropertyChanged(nameof(ContabilitaMeseSelezionato));
        OnPropertyChanged(nameof(ReportPersonaleMeseSelezionato));
        OnPropertyChanged(nameof(ContabilitaPeriodoTitolo));
        OnPropertyChanged(nameof(RegistroImmersioniPeriodoTitolo));
        OnPropertyChanged(nameof(ReportPersonalePeriodoTitolo));
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
        CaricaReportPersonaleMensile();
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

    private void CaricaReportPersonaleMensile()
    {
        if (ReportPersonaleMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            return;
        }

        var dataInizio = ReportPersonaleMeseSelezionato.NumeroMese == 0
            ? new DateOnly(ContabilitaAnnoSelezionato, 1, 1)
            : new DateOnly(ContabilitaAnnoSelezionato, ReportPersonaleMeseSelezionato.NumeroMese, 1);
        var dataFine = ReportPersonaleMeseSelezionato.NumeroMese == 0
            ? new DateOnly(ContabilitaAnnoSelezionato, 12, 31)
            : dataInizio.AddMonths(1).AddDays(-1);

        _reportPersonaleSource.Clear();
        _reportPersonaleSource.AddRange(_repository.GetReportPersonale(dataInizio, dataFine));
        ApplicaFiltriReportPersonale();
    }

    private void ApplicaFiltriReportPersonale()
    {
        var nominativo = ReportPersonaleFiltroNominativo.Trim();
        var items = _reportPersonaleSource.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(nominativo))
        {
            items = items.Where(item =>
                item.Nominativo.Contains(nominativo, StringComparison.CurrentCultureIgnoreCase)
                || item.PerIdDisplay.Contains(nominativo, StringComparison.CurrentCultureIgnoreCase));
        }

        ReportPersonaleItems.Clear();
        foreach (var item in items
                     .OrderBy(item => item.DataServizio)
                     .ThenBy(item => item.NumeroOrdineServizio)
                     .ThenBy(item => item.Nominativo)
                     .ThenBy(item => item.TipoRiga)
                     .ThenBy(item => item.NumeroImmersione ?? 0))
        {
            ReportPersonaleItems.Add(item);
        }

        AggiornaRiepilogoReportPersonale();
    }

    private void PulisciFiltriReportPersonale()
    {
        if (string.IsNullOrWhiteSpace(_reportPersonaleFiltroNominativo))
        {
            return;
        }

        _reportPersonaleFiltroNominativo = string.Empty;
        OnPropertyChanged(nameof(ReportPersonaleFiltroNominativo));
        ApplicaFiltriReportPersonale();
    }

    private void AggiornaRiepilogoReportPersonale()
    {
        OnPropertyChanged(nameof(ReportPersonalePeriodoTitolo));
        OnPropertyChanged(nameof(ReportPersonaleStato));
        OnPropertyChanged(nameof(ReportPersonaleTotaleRighe));
        OnPropertyChanged(nameof(ReportPersonaleTotalePersone));
        OnPropertyChanged(nameof(ReportPersonaleTotaleImmersioni));
        OnPropertyChanged(nameof(ReportPersonaleTotaleOre));
        OnPropertyChanged(nameof(ReportPersonaleTotaleOreDisplay));
        OnPropertyChanged(nameof(HasFiltriReportPersonaleAttivi));
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

    private void EsportaContabilitaExcel()
    {
        if (ContabilitaMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(DatabasePaths.ExportDirectory);

            var fileName = $"contabilita-mensile-{ContabilitaAnnoSelezionato:D4}-{ContabilitaMeseSelezionato.NumeroMese:D2}.xlsx";
            var filePath = Path.Combine(DatabasePaths.ExportDirectory, fileName);
            WriteContabilitaExcelXlsx(filePath);

            Stato = $"Export Excel creato: {filePath}";
            MessageBox.Show(
                $"Export Excel contabilita creato in:\n{filePath}",
                "Export contabilita Excel",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export contabilita Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Export contabilita Excel non riuscito.";
        }
    }

    private void WriteContabilitaExcelXlsx(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);
        AddZipEntry(archive, "[Content_Types].xml", BuildXlsxContentTypes());
        AddZipEntry(archive, "_rels/.rels", BuildXlsxRootRelationships());
        AddZipEntry(archive, "xl/workbook.xml", BuildXlsxWorkbook());
        AddZipEntry(archive, "xl/_rels/workbook.xml.rels", BuildXlsxWorkbookRelationships());
        AddZipEntry(archive, "xl/styles.xml", BuildXlsxStyles());
        AddZipEntry(archive, "xl/worksheets/sheet1.xml", BuildContabilitaWorksheetXml());
    }

    private string BuildContabilitaWorksheetXml()
    {
        var rows = BuildContabilitaWorksheetRows();
        var builder = new StringBuilder();
        builder.AppendLine("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.AppendLine("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">""");
        builder.AppendLine("""<sheetViews><sheetView workbookViewId="0"/></sheetViews>""");
        builder.AppendLine("""<sheetFormatPr defaultRowHeight="15"/>""");
        builder.AppendLine("""<cols><col min="1" max="1" width="12" customWidth="1"/><col min="2" max="2" width="28" customWidth="1"/><col min="3" max="5" width="10" customWidth="1"/><col min="6" max="12" width="12" customWidth="1"/></cols>""");
        builder.AppendLine("<sheetData>");
        foreach (var row in rows)
        {
            builder.AppendLine(row);
        }

        builder.AppendLine("</sheetData>");
        builder.AppendLine("""<mergeCells count="5"><mergeCell ref="A1:L1"/><mergeCell ref="A2:L2"/><mergeCell ref="A3:L3"/><mergeCell ref="A5:L5"/><mergeCell ref="A6:L6"/></mergeCells>""");
        builder.AppendLine("""<pageMargins left="0.25" right="0.25" top="0.5" bottom="0.5" header="0.3" footer="0.3"/>""");
        builder.AppendLine("</worksheet>");
        return builder.ToString();
    }

    private List<string> BuildContabilitaWorksheetRows()
    {
        var rows = new List<string>();
        var rowIndex = 1;
        rows.Add(BuildXlsxRow(rowIndex++, [TextCell(1, 1, "POLIZIA DI STATO", 1)]));
        rows.Add(BuildXlsxRow(rowIndex++, [TextCell(2, 1, "Centro Nautico e Sommozzatori", 2)]));
        rows.Add(BuildXlsxRow(rowIndex++, [TextCell(3, 1, "Nucleo Sommozzatori", 2)]));
        rows.Add(BuildXlsxRow(rowIndex++, []));
        rows.Add(BuildXlsxRow(rowIndex++, [TextCell(5, 1, "D I C H I A R A Z I O N E", 3)]));
        rows.Add(BuildXlsxRow(rowIndex++, [TextCell(6, 1, $"Indennita di rischio per operatori subacquei - {ContabilitaPeriodoTitolo}", 2)]));
        rows.Add(BuildXlsxRow(rowIndex++, []));
        rows.Add(BuildXlsxRow(rowIndex++, BuildHeaderCells(rowIndex - 1)));

        var groups = ContabilitaSmzItems
            .GroupBy(item => new { item.PerId, item.Cognome, item.Nome, item.Qualifica })
            .OrderBy(group => group.Key.Cognome)
            .ThenBy(group => group.Key.Nome)
            .ToList();

        if (groups.Count == 0)
        {
            rows.Add(BuildXlsxRow(rowIndex, [TextCell(rowIndex, 1, "Nessuna riga contabile SMZ nel periodo selezionato.", 5)]));
            return rows;
        }

        decimal totaleGenerale = 0;
        foreach (var group in groups)
        {
            var righeOperatore = BuildRigheMensiliOperatore(group)
                .Where(HasValoriContabili)
                .ToList();
            if (righeOperatore.Count == 0)
            {
                continue;
            }

            var totaleOperatore = righeOperatore.Sum(item => item.Importo);
            totaleGenerale += totaleOperatore;
            for (var index = 0; index < righeOperatore.Count; index++)
            {
                var riga = righeOperatore[index];
                var cells = new List<string>
                {
                    TextCell(rowIndex, 1, index == 0 ? QualificaFormatter.AbbreviaPerVisualizzazione(group.Key.Qualifica) : string.Empty, 5),
                    TextCell(rowIndex, 2, index == 0 ? $"{group.Key.Cognome} {group.Key.Nome}".Trim() : string.Empty, 5),
                    TextCell(rowIndex, 3, riga.Apparato, 5),
                    TextCell(rowIndex, 4, riga.FasciaProfondita, 5),
                    NumberCell(rowIndex, 5, riga.Tariffa, 6, blankZero: false),
                    NumberCell(rowIndex, 6, riga.OreOrd, 6),
                    NumberCell(rowIndex, 7, riga.OreAdd, 6),
                    NumberCell(rowIndex, 8, riga.OreSper, 6),
                    NumberCell(rowIndex, 9, riga.OreCi, 6),
                    NumberCell(rowIndex, 10, riga.Importo, 6),
                    NumberCell(rowIndex, 11, 0m, 6),
                    NumberCell(rowIndex, 12, index == righeOperatore.Count - 1 ? totaleOperatore : 0m, 6),
                };
                rows.Add(BuildXlsxRow(rowIndex++, cells));
            }
        }

        rows.Add(BuildXlsxRow(rowIndex, new List<string>
        {
            BlankCell(rowIndex, 1, 7),
            TextCell(rowIndex, 2, "TOTALE GENERALE", 7),
            BlankCell(rowIndex, 3, 7),
            BlankCell(rowIndex, 4, 7),
            BlankCell(rowIndex, 5, 7),
            NumberCell(rowIndex, 6, ContabilitaSmzItems.Sum(item => item.OreOrd), 8),
            NumberCell(rowIndex, 7, ContabilitaSmzItems.Sum(item => item.OreAdd), 8),
            NumberCell(rowIndex, 8, ContabilitaSmzItems.Sum(item => item.OreSper), 8),
            NumberCell(rowIndex, 9, ContabilitaSmzItems.Sum(item => item.OreCi), 8),
            NumberCell(rowIndex, 10, totaleGenerale, 8),
            NumberCell(rowIndex, 11, 0m, 8),
            NumberCell(rowIndex, 12, totaleGenerale, 8),
        }));
        return rows;
    }

    private static List<string> BuildHeaderCells(int rowIndex)
    {
        var headers = new[] { "Qual", "Cognome e Nome", "Appar.", "Prof.", "Tariffa", "ORE ORD", "ORE ADD", "ORE SPER", "ORE C.I.", "Importo", "Med. Rag.", "TOTALE" };
        return headers
            .Select((header, index) => TextCell(rowIndex, index + 1, header, 4))
            .ToList();
    }

    private static IEnumerable<ContabilitaMensileOperatoreRow> BuildRigheMensiliOperatore(IEnumerable<ContabilitaSmzSummary> items)
    {
        var source = items.ToList();
        foreach (var template in GetRigheTariffarieMensili())
        {
            var matches = source
                .Where(item =>
                    string.Equals(NormalizeApparatoContabile(item.Apparato), template.Apparato, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.FasciaProfondita, template.FasciaProfondita, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var oreOrd = matches.Sum(item => item.OreOrd);
            var oreAdd = matches.Sum(item => item.OreAdd);
            var oreSper = matches.Sum(item => item.OreSper);
            var oreCi = matches.Sum(item => item.OreCi);
            var importo = matches.Count == 0
                ? 0m
                : matches.Sum(item => item.Importo);

            yield return new ContabilitaMensileOperatoreRow(
                template.Apparato,
                template.FasciaProfondita,
                template.Tariffa,
                oreOrd,
                oreAdd,
                oreSper,
                oreCi,
                importo);
        }
    }

    private static IEnumerable<ContabilitaMensileTariffaRow> GetRigheTariffarieMensili()
    {
        yield return new ContabilitaMensileTariffaRow("A.R.O.", "00/12", 30m);
        yield return new ContabilitaMensileTariffaRow("A.R.A.", "00/12", 5m);
        yield return new ContabilitaMensileTariffaRow("A.R.A.", "13/25", 10m);
        yield return new ContabilitaMensileTariffaRow("A.R.A.", "26/40", 20m);
        yield return new ContabilitaMensileTariffaRow("A.R.A.", "41/55", 28m);
        yield return new ContabilitaMensileTariffaRow("A.R.A.", "56/80", 38m);
        yield return new ContabilitaMensileTariffaRow("A.R.M.", "00/12", 10m);
        yield return new ContabilitaMensileTariffaRow("A.R.M.", "13/25", 15m);
        yield return new ContabilitaMensileTariffaRow("A.R.M.", "26/40", 18m);
        yield return new ContabilitaMensileTariffaRow("A.R.M.", "41/55", 24m);
        yield return new ContabilitaMensileTariffaRow("C.I.", "00/12", 2.48m);
        yield return new ContabilitaMensileTariffaRow("C.I.", "13/25", 2.48m);
        yield return new ContabilitaMensileTariffaRow("C.I.", "26/40", 2.48m);
        yield return new ContabilitaMensileTariffaRow("C.I.", "41/55", 2.48m);
    }

    private static string NormalizeApparatoContabile(string apparato)
    {
        if (apparato.Contains("A.R.A", StringComparison.OrdinalIgnoreCase)
            || apparato.Contains("ASAS", StringComparison.OrdinalIgnoreCase))
        {
            return "A.R.A.";
        }

        if (apparato.Contains("A.R.O", StringComparison.OrdinalIgnoreCase))
        {
            return "A.R.O.";
        }

        if (apparato.Contains("A.R.M", StringComparison.OrdinalIgnoreCase))
        {
            return "A.R.M.";
        }

        if (apparato.Contains("C.I", StringComparison.OrdinalIgnoreCase))
        {
            return "C.I.";
        }

        return apparato.Trim();
    }

    private void StampaContabilitaMensile()
    {
        if (ContabilitaMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            return;
        }

        try
        {
            var dialog = new PrintDialog
            {
                PrintTicket = { PageOrientation = PageOrientation.Landscape },
            };

            if (dialog.ShowDialog() != true)
            {
                Stato = "Stampa contabilita annullata.";
                return;
            }

            var document = BuildContabilitaPrintDocument();
            document.PageWidth = dialog.PrintableAreaWidth;
            document.PageHeight = dialog.PrintableAreaHeight;
            document.PagePadding = new Thickness(28);
            document.ColumnWidth = document.PageWidth - document.PagePadding.Left - document.PagePadding.Right;
            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Contabilita {ContabilitaPeriodoTitolo}");
            Stato = "Stampa contabilita inviata alla stampante.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Stampa contabilita", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Stampa contabilita non riuscita.";
        }
    }

    private FlowDocument BuildContabilitaPrintDocument()
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Calibri"),
            FontSize = 8.5,
            PagePadding = new Thickness(28),
        };

        document.Blocks.Add(new Paragraph(new Run($"CONTABILITA MENSILE IMMERSIONI - {ContabilitaPeriodoTitolo.ToUpper(CultureInfo.CurrentCulture)}"))
        {
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12),
        });
        document.Blocks.Add(BuildContabilitaPrintTable());
        return document;
    }

    private Table BuildContabilitaPrintTable()
    {
        var table = new Table { CellSpacing = 0 };
        foreach (var width in new[] { 58d, 76d, 42d, 62d, 150d, 58d, 58d, 50d, 48d, 48d, 48d, 48d, 62d })
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        }

        table.RowGroups.Add(new TableRowGroup());
        AddPrintHeader(table, "Data", "Ordine", "PerID", "Qual.", "Cognome e nome", "Appar.", "Prof.", "Tariffa", "ORD", "ADD", "SPER", "C.I.", "Importo");
        foreach (var item in ContabilitaSmzItems)
        {
            AddPrintRow(
                table,
                item.DataServizioDescrizione,
                item.NumeroOrdineServizio,
                item.PerId.ToString(CultureInfo.CurrentCulture),
                item.QualificaDisplay,
                item.Nominativo,
                item.Apparato,
                item.FasciaProfondita,
                item.TariffaDisplay,
                item.OreOrdDisplay,
                item.OreAddDisplay,
                item.OreSperDisplay,
                item.OreCiDisplay,
                item.ImportoDisplay);
        }

        AddPrintRow(table, string.Empty, string.Empty, string.Empty, string.Empty, "TOTALI", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, ContabilitaSmzTotaleOreDisplay, ContabilitaSmzTotaleImportiDisplay);
        return table;
    }

    private static void AddPrintHeader(Table table, params string[] values)
    {
        var row = new TableRow { FontWeight = FontWeights.Bold, Background = Brushes.WhiteSmoke };
        foreach (var value in values)
        {
            row.Cells.Add(CreatePrintCell(value));
        }

        table.RowGroups[0].Rows.Add(row);
    }

    private static void AddPrintRow(Table table, params string[] values)
    {
        var row = new TableRow();
        foreach (var value in values)
        {
            row.Cells.Add(CreatePrintCell(value));
        }

        table.RowGroups[0].Rows.Add(row);
    }

    private static TableCell CreatePrintCell(string value) =>
        new(new Paragraph(new Run(value)) { Margin = new Thickness(2) })
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(2),
        };

    private static bool HasValoriContabili(ContabilitaMensileOperatoreRow riga) =>
        riga.OreOrd != 0m
        || riga.OreAdd != 0m
        || riga.OreSper != 0m
        || riga.OreCi != 0m
        || riga.Importo != 0m;

    private static void AddZipEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content.TrimStart());
    }

    private static string BuildXlsxContentTypes() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private static string BuildXlsxRootRelationships() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private static string BuildXlsxWorkbook() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets>
            <sheet name="Mensile" sheetId="1" r:id="rId1"/>
          </sheets>
        </workbook>
        """;

    private static string BuildXlsxWorkbookRelationships() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private static string BuildXlsxStyles() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="3">
            <font><sz val="10"/><name val="Calibri"/></font>
            <font><b/><sz val="14"/><name val="Calibri"/></font>
            <font><b/><sz val="10"/><name val="Calibri"/></font>
          </fonts>
          <fills count="3">
            <fill><patternFill patternType="none"/></fill>
            <fill><patternFill patternType="gray125"/></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FFD9EAF7"/><bgColor indexed="64"/></patternFill></fill>
          </fills>
          <borders count="2">
            <border><left/><right/><top/><bottom/><diagonal/></border>
            <border><left style="thin"><color auto="1"/></left><right style="thin"><color auto="1"/></right><top style="thin"><color auto="1"/></top><bottom style="thin"><color auto="1"/></bottom><diagonal/></border>
          </borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="9">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment horizontal="center"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment horizontal="center"/></xf>
            <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment horizontal="center"/></xf>
            <xf numFmtId="0" fontId="2" fillId="2" borderId="1" xfId="0" applyAlignment="1"><alignment horizontal="center"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0"/>
            <xf numFmtId="2" fontId="0" fillId="0" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment horizontal="right"/></xf>
            <xf numFmtId="0" fontId="2" fillId="2" borderId="1" xfId="0"/>
            <xf numFmtId="2" fontId="2" fillId="2" borderId="1" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment horizontal="right"/></xf>
          </cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;

    private static string BuildXlsxRow(int rowIndex, IEnumerable<string> cells) =>
        $"<row r=\"{rowIndex}\">{string.Concat(cells)}</row>";

    private static string TextCell(int rowIndex, int columnIndex, string value, int styleIndex) =>
        string.IsNullOrWhiteSpace(value)
            ? BlankCell(rowIndex, columnIndex, styleIndex)
            : $"<c r=\"{CellRef(rowIndex, columnIndex)}\" s=\"{styleIndex}\" t=\"inlineStr\"><is><t>{Xml(value)}</t></is></c>";

    private static string NumberCell(int rowIndex, int columnIndex, decimal value, int styleIndex, bool blankZero = true) =>
        blankZero && value == 0m
            ? BlankCell(rowIndex, columnIndex, styleIndex)
            : $"<c r=\"{CellRef(rowIndex, columnIndex)}\" s=\"{styleIndex}\"><v>{value.ToString("0.##", CultureInfo.InvariantCulture)}</v></c>";

    private static string BlankCell(int rowIndex, int columnIndex, int styleIndex) =>
        $"<c r=\"{CellRef(rowIndex, columnIndex)}\" s=\"{styleIndex}\"/>";

    private static string CellRef(int rowIndex, int columnIndex) => $"{ColumnName(columnIndex)}{rowIndex}";

    private static string ColumnName(int columnIndex)
    {
        var dividend = columnIndex;
        var columnName = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    private static string Xml(string value) => WebUtility.HtmlEncode(value);

    private sealed record ContabilitaMensileTariffaRow(string Apparato, string FasciaProfondita, decimal Tariffa);

    private sealed record ContabilitaMensileOperatoreRow(
        string Apparato,
        string FasciaProfondita,
        decimal Tariffa,
        decimal OreOrd,
        decimal OreAdd,
        decimal OreSper,
        decimal OreCi,
        decimal Importo);

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
