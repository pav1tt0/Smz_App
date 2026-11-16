using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using System.Xml.Linq;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Printing;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.Tests;

internal static partial class Program
{
    private static ServizioGiornaliero ServizioPeriodo(PersonaleRepository repository, DateOnly data, string numero)
    {
        var c = repository.GetCataloghiServizio();
        return new ServizioGiornaliero
        {
            DataServizio = data, NumeroOrdineServizio = numero, OrarioServizio = "08.00/14.00",
            LocalitaOperativaId = c.LocalitaOperative.First().LocalitaOperativaId,
            ScopoImmersioneId = c.ScopiImmersione.First().ScopoImmersioneId,
            AttivitaSvolta = "Ricognizione del fondale e verifica delle procedure operative.",
        };
    }

    private static void TestPeriodiServizi() => ConDatabaseServiziIsolato(repository =>
    {
        var oggi = DateOnly.FromDateTime(DateTime.Today);
        var primo = new DateOnly(oggi.Year, oggi.Month, 1);
        for (var i = 0; i < 105; i++) repository.SaveServizioGiornaliero(ServizioPeriodo(repository, primo, $"MESE-{i}"));
        repository.SaveServizioGiornaliero(ServizioPeriodo(repository, primo.AddDays(-1), "FUORI-MESE"));
        var vm = new MainWindowViewModel();
        AssertEqual(105, vm.ServiziSalvati.Count, "Tutti i servizi del mese, oltre il vecchio limite");
        AssertTrue(vm.ServiziSalvati.All(s => s.DataServizio.Month == oggi.Month && s.DataServizio.Year == oggi.Year), "Mesi diversi nell'elenco iniziale.");
        vm.ServiziPeriodoModalita = 2;
        AssertEqual(105, vm.ServiziSalvati.Count, "Intervallo inizializzato al mese scelto");
        vm.ServiziDataAl = string.Empty;
        vm.ServiziDataDal = primo.AddDays(-1).ToString("dd/MM/yyyy");
        AssertEqual(0, vm.ServiziSalvati.Count, "Intervallo incompleto non deve mostrare risultati non filtrati");
        vm.ServiziDataAl = primo.ToString("dd/MM/yyyy");
        AssertEqual(106, vm.ServiziSalvati.Count, "Intervallo a cavallo del mese e estremi inclusi");
        vm.ServiziDataDal = primo.AddDays(1).ToString("dd/MM/yyyy");
        AssertEqual(0, vm.ServiziSalvati.Count, "Intervallo invertito");
        AssertTrue(vm.ServiziSalvatiStato.Contains("precedere"), "Messaggio intervallo invertito assente.");
        vm.ClearServiziSearchCommand.Execute(null);
        AssertEqual(105, vm.ServiziSalvati.Count, "Pulisci ripristina il mese corrente");
        vm.ServiziAnnoFiltro = primo.AddMonths(-1).Year;
        vm.ServiziMeseFiltro = primo.AddMonths(-1).Month;
        AssertEqual(1, vm.ServiziSalvati.Count, "Mese selezionato");
        vm.ServiziPeriodoModalita = 1;
        vm.ServiziGiornoFiltro = "data errata";
        AssertEqual(0, vm.ServiziSalvati.Count, "Data non valida senza ripiego su tutti i servizi");
        vm.ServiziGiornoFiltro = primo.AddDays(-1).ToString("dd/MM/yyyy");
        AssertEqual(1, vm.ServiziSalvati.Count, "Giorno preciso");
        vm.ServiziPeriodoModalita = 0;
        AssertEqual(1, vm.ServiziSalvati.Count, "Mese conservato passando da giorno a mese");
        var fineAnno = new DateOnly(2024, 12, 31);
        repository.SaveServizioGiornaliero(ServizioPeriodo(repository, fineAnno, "ANNO-A"));
        repository.SaveServizioGiornaliero(ServizioPeriodo(repository, fineAnno.AddDays(1), "ANNO-B"));
        vm.ServiziPeriodoModalita = 2;
        vm.ServiziDataDal = "31/12/2024";
        vm.ServiziDataAl = "01/01/2025";
        AssertEqual(2, vm.ServiziSalvati.Count, "Intervallo fra due anni");
        vm.ServiziPeriodoModalita = 0;
        AssertEqual(1, vm.ServiziSalvati.Count, "Intervallo nascosto non deve filtrare il mese");
        vm.ServiziPeriodoModalita = 2;
        AssertEqual(2, vm.ServiziSalvati.Count, "Intervallo conservato cambiando modalità");
        vm.ClearServiziSearchCommand.Execute(null);
        AssertEqual(0, vm.ServiziPeriodoModalita, "Reimposta seleziona Mese");
        AssertEqual(105, vm.ServiziSalvati.Count, "Reimposta ripristina il mese corrente dopo un intervallo");
    });

    private static ServizioGiornaliero PreparaRegistro(PersonaleRepository repository)
    {
        var agente = CreaPersonale(801, "Alfa", "Agente", "REG801", "");
        agente.Qualifica = "Agente";
        var ispettore = CreaPersonale(802, "Zeta", "Ispettore", "REG802", "");
        ispettore.Qualifica = "Ispettore";
        foreach (var p in new[] { agente, ispettore }) repository.SavePersonale(p, true);
        var servizio = ServizioPeriodo(repository, new DateOnly(2026, 11, 16), "REG-320");
        servizio.ResponsabileServizioPerId = 802;
        var c = repository.GetCataloghiServizio();
        foreach (var p in new[] { agente, ispettore }) servizio.Partecipanti.Add(new ServizioPartecipante
        { PerId = p.PerId, Presente = true, GruppoOperativoId = c.GruppiOperativi.First().GruppoOperativoId });
        for (var n = 1; n <= 2; n++)
        {
            var dive = new ServizioImmersione
            {
                NumeroImmersione = n, OrarioInizio = new TimeOnly(8 + n, 0), OrarioFine = new TimeOnly(9 + n, 0),
                Note = n == 1 ? "NOTA-PRIMA" : "NOTA-SECONDA",
                LocalitaOperativaId = c.LocalitaOperative.Last().LocalitaOperativaId,
                ScopoImmersioneId = c.ScopiImmersione.Last().ScopoImmersioneId,
            };
            foreach (var p in new[] { agente, ispettore }) dive.Partecipazioni.Add(new ServizioPartecipanteImmersione
            {
                ServizioPartecipanteId = p.PerId,
                TipologiaImmersioneOperativaId = c.TipologieImmersione.Single(t => t.Codice == "ARA_ASAS").TipologiaImmersioneOperativaId,
                FasciaProfonditaId = c.FasceProfondita.First().FasciaProfonditaId,
                CategoriaContabileOreId = c.CategorieContabiliOre.Single(t => t.Codice == "ORD").CategoriaContabileOreId,
                ProfonditaMetri = 10, OreImmersione = n == 1 ? .5m : 1m,
            });
            servizio.Immersioni.Add(dive);
        }
        var id = repository.SaveServizioGiornaliero(servizio);
        return repository.GetServizioGiornalieroById(id)!;
    }

    private static void TestRegistroA4PerImmersione() => ConDatabaseServiziIsolato(repository =>
    {
        var servizio = PreparaRegistro(repository);
        var summary = repository.GetServiziGiornalieriRecenti().Single();
        AssertTrue(summary.ResponsabileServizioDescrizione.Contains("Zeta Ispettore"), "Responsabile assente dal riepilogo del fumetto.");
        var start = new DateOnly(2026, 11, 16);
        var print = new RegistroImmersioniMensilePrintService(repository);
        var output = Path.GetFullPath("artifacts/verifica-2026-11-16");
        Directory.CreateDirectory(output);
        foreach (var layout in Enum.GetValues<RegistroImmersioniMensilePrintLayout>())
        {
            var paginator = print.BuildPaginator(start, start, layout);
            AssertEqual(4, paginator.PageCount, "Copertina, due schede e riepilogo");
            for (var i = 0; i < paginator.PageCount; i++)
                AssertEqual(SingleSheetDocumentPaginator.A4, paginator.GetPage(i).Size, "Dimensioni A4");
            var texts = EsportaPagine(paginator, Path.Combine(output, $"registro-{layout}"));
            AssertTrue(texts[1].Contains("NOTA-PRIMA") && !texts[1].Contains("NOTA-SECONDA"), "Immersioni mischiate nella prima scheda.");
            AssertTrue(texts[2].Contains("NOTA-SECONDA") && !texts[2].Contains("NOTA-PRIMA"), "Immersioni mischiate nella seconda scheda.");
            AssertTrue(texts[1].IndexOf("Zeta Ispettore", StringComparison.Ordinal) < texts[1].IndexOf("Alfa Agente", StringComparison.Ordinal), "Operatori non ordinati per qualifica.");
            AssertTrue(texts[1].Contains("0,5") || texts[1].Contains("0.5"), "Ore della prima immersione assenti.");
        }
        var vm = new MainWindowViewModel { RegistroDataDal = "16/11/2026", RegistroDataAl = "16/11/2026" };
        AssertEqual(4, vm.RegistroImmersioniItems.Count, "Intervallo del registro inclusivo");
        VerificaSchermatePeriodi(vm, output);
        vm.RegistroDataAl = "15/11/2026";
        AssertEqual(0, vm.RegistroImmersioniItems.Count, "Intervallo registro invertito");
        servizio.Immersioni[0].Note = string.Join("\n", Enumerable.Range(1, 100).Select(n => $"Osservazione operativa {n:000}: verifica della strumentazione.")) + "\nFINE-NOTE-ESTESE";
        repository.SaveServizioGiornaliero(servizio);
        var extended = print.BuildPaginator(start, start);
        AssertEqual(4, extended.PageCount, "Note estese su una sola scheda");
        var extendedTexts = EsportaPagine(extended, Path.Combine(output, "registro-note-estese"));
        AssertTrue(extendedTexts[1].Contains("FINE-NOTE-ESTESE"), "Note estese tagliate.");
    });

    private static void TestOrdineRegistroDecorrenza() => ConDatabaseServiziIsolato(repository =>
    {
        (int Id, string Cognome, string Qualifica, DateOnly? Decorrenza)[] dati =
        [
            (821, "ZzzAnziano", "Ispettore", new DateOnly(2010, 1, 1)),
            (822, "AaaRecente", "Ispettore", new DateOnly(2020, 1, 1)),
            (823, "BbbSenzaData", "Ispettore", null),
            (824, "CccCommissario", "Commissario", null),
            (825, "DddAgente", "Agente", new DateOnly(1900, 1, 1)),
            (826, "YyyStessaData", "Ispettore", new DateOnly(2010, 1, 1)),
        ];
        var servizio = ServizioPeriodo(repository, new DateOnly(2026, 11, 16), "DECORRENZA");
        var dive = new ServizioImmersione { NumeroImmersione = 1 };
        foreach (var r in dati)
        {
            var persona = CreaPersonale(r.Id, r.Cognome, "Operatore", $"DEC{r.Id}", "");
            persona.Qualifica = r.Qualifica;
            persona.DataDecorrenzaQualifica = r.Decorrenza;
            repository.SavePersonale(persona, true);
            servizio.Partecipanti.Add(new ServizioPartecipante { PerId = r.Id, Presente = true, GruppoOperativoId = 1 });
            dive.Partecipazioni.Add(new ServizioPartecipanteImmersione
            { ServizioPartecipanteId = r.Id, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 1, CategoriaContabileOreId = 1, ProfonditaMetri = 10, OreImmersione = 1 });
        }
        servizio.Immersioni.Add(dive);
        repository.SaveServizioGiornaliero(servizio);
        int[] atteso = [824, 826, 821, 822, 823, 825];
        var righe = repository.GetRegistroImmersioniMensile(2026, 11);
        AssertEqual(string.Join(",", atteso), string.Join(",", righe.Select(r => r.PerId)), "Qualifica, decorrenza crescente, data assente in fondo alla qualifica");
        AssertEqual("01/01/2010", righe.Single(r => r.PerId == 821).DecorrenzaQualificaDisplay, "Data decorrenza letta dal registro");
        var vm = new MainWindowViewModel { RegistroDataDal = "16/11/2026", RegistroDataAl = "16/11/2026" };
        AssertEqual(string.Join(",", atteso), string.Join(",", vm.RegistroImmersioniItems.Select(r => r.PerId)), "Ordine nella pagina registro");
        var print = new RegistroImmersioniMensilePrintService(repository);
        foreach (var layout in Enum.GetValues<RegistroImmersioniMensilePrintLayout>())
        {
            var paginator = print.BuildPaginator(new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 16), layout);
            var text = EsportaPagine(paginator, Path.GetFullPath($"artifacts/verifica-2026-11-16/registro-decorrenza-{layout}"))[1];
            var previous = -1;
            foreach (var id in atteso)
            {
                var nome = dati.Single(r => r.Id == id).Cognome;
                var position = text.IndexOf(nome, StringComparison.Ordinal);
                AssertTrue(position > previous, $"Ordine errato per {nome} nella stampa {layout}.");
                previous = position;
            }
        }
    });

    private static void VerificaSchermatePeriodi(MainWindowViewModel vm, string output)
    {
        var shell = new SMZ.Conta.App.MainWindow(new AccessSession(0, "Verifica locale", AccessRole.Administrator, false));
        try
        {
            foreach (var width in new[] { 900, 1200 })
            {
                var reports = new SMZ.Conta.App.Views.ReportsView { DataContext = vm, Resources = shell.Resources };
                Layout(reports, width);
                var tabs = Discendenti(reports).OfType<TabControl>().Single();
                tabs.SelectedItem = tabs.Items.OfType<TabItem>().Single(t => (string)t.Header == "Registro immersioni");
                Layout(reports, width);
                foreach (var picker in Discendenti(reports).OfType<SMZ.Conta.App.Controls.DateInputPicker>())
                {
                    var path = BindingOperations.GetBindingExpression(picker, SMZ.Conta.App.Controls.DateInputPicker.TextProperty)?.ParentBinding.Path.Path;
                    if (path is not ("RegistroDataDal" or "RegistroDataAl")) continue;
                    var bounds = picker.TransformToAncestor(reports).TransformBounds(new Rect(picker.RenderSize));
                    AssertTrue(bounds.Left >= 0 && bounds.Right <= width + 1, "Filtro registro fuori dalla pagina.");
                }
                Snapshot(reports, Path.Combine(output, $"schermata-registro-{width}.png"), width);
                tabs.SelectedItem = tabs.Items.OfType<TabItem>().Single(t => (string)t.Header == "Libretto del sommozzatore");
                vm.LibrettoOperatore = vm.LibrettoOperatori.First();
                Layout(reports, width);
                Snapshot(reports, Path.Combine(output, $"schermata-libretto-{width}.png"), width);
                var servizi = new SMZ.Conta.App.Views.ServizioGiornalieroView { DataContext = vm, Resources = shell.Resources };
                Layout(servizi, width);
                foreach (var picker in Discendenti(servizi).OfType<SMZ.Conta.App.Controls.DateInputPicker>())
                {
                    var path = BindingOperations.GetBindingExpression(picker, SMZ.Conta.App.Controls.DateInputPicker.TextProperty)?.ParentBinding.Path.Path;
                    if (path is not ("ServiziDataDal" or "ServiziDataAl")) continue;
                    var bounds = picker.TransformToAncestor(servizi).TransformBounds(new Rect(picker.RenderSize));
                    AssertTrue(bounds.Left >= 0 && bounds.Right <= width + 1, "Filtro servizi fuori dalla pagina.");
                }
                foreach (var mode in new[] { 0, 1, 2 })
                {
                    vm.ServiziPeriodoModalita = mode;
                    Layout(servizi, width);
                    var filter = Discendenti(servizi).OfType<SMZ.Conta.App.Controls.ServicePeriodFilter>().Single();
                    AssertEqual(mode == 0 ? Visibility.Visible : Visibility.Collapsed, ((StackPanel)filter.FindName("MonthFields")).Visibility, "Visibilità mese");
                    AssertEqual(mode == 1 ? Visibility.Visible : Visibility.Collapsed, ((FrameworkElement)filter.FindName("DayField")).Visibility, "Visibilità giorno");
                    AssertEqual(mode == 2 ? Visibility.Visible : Visibility.Collapsed, ((StackPanel)filter.FindName("RangeFields")).Visibility, "Visibilità intervallo");
                    var bounds = filter.TransformToAncestor(servizi).TransformBounds(new Rect(filter.RenderSize));
                    AssertTrue(bounds.Left >= 0 && bounds.Right <= width + 1, "Filtro periodo fuori dalla pagina servizi.");
                    Snapshot(servizi, Path.Combine(output, $"schermata-servizi-modalita-{mode}-{width}.png"), width);
                    tabs.SelectedItem = tabs.Items.OfType<TabItem>().Single(t => (string)t.Header == "Servizi");
                    Layout(reports, width);
                    var reportFilter = Discendenti(reports).OfType<SMZ.Conta.App.Controls.ServicePeriodFilter>().Single();
                    AssertEqual(mode, ((ComboBox)reportFilter.FindName("ModeSelector")).SelectedIndex, "Filtro sincronizzato nella pagina Stampe");
                    bounds = reportFilter.TransformToAncestor(reports).TransformBounds(new Rect(reportFilter.RenderSize));
                    AssertTrue(bounds.Left >= 0 && bounds.Right <= width + 1, "Filtro periodo fuori dalla pagina Stampe.");
                    Snapshot(reports, Path.Combine(output, $"schermata-stampe-servizi-modalita-{mode}-{width}.png"), width);
                }
                vm.ClearServiziSearchCommand.Execute(null);
            }
        }
        finally { shell.Close(); }

        static void Layout(FrameworkElement element, int width)
        {
            element.Measure(new Size(width, 850));
            element.Arrange(new Rect(0, 0, width, 850));
            element.UpdateLayout();
        }
        static void Snapshot(FrameworkElement element, string path, int width)
        {
            var bitmap = new RenderTargetBitmap(width, 850, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(element);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(path);
            encoder.Save(file);
        }
    }

    private static IEnumerable<DependencyObject> Discendenti(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in Discendenti(child)) yield return descendant;
        }
    }

    private static void TestLibrettoSommozzatore() => ConDatabaseServiziIsolato(repository =>
    {
        var servizio = PreparaRegistro(repository);
        var old = ServizioPeriodo(repository, new DateOnly(2024, 12, 31), "VECCHIO");
        old.Partecipanti = [new ServizioPartecipante { PerId = 801, Presente = true, GruppoOperativoId = 1 }];
        old.Immersioni = [new ServizioImmersione { NumeroImmersione = 1, Partecipazioni = [new ServizioPartecipanteImmersione
        { ServizioPartecipanteId = 801, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 1, CategoriaContabileOreId = 1, ProfonditaMetri = 10, OreImmersione = 2 }] }];
        // Same PerID on an external service must never contaminate the personal book.
        old.OperatoriSubEsterni = [new ServizioOperatoreSubEsterno { PerId = 801, Nominativo = "Esterno omonimo", Reparto = "Altro reparto", GruppoOperativoId = 1 }];
        old.Immersioni[0].PartecipazioniEsterne.Add(new ServizioOperatoreSubEsternoImmersione
        { ServizioOperatoreSubEsternoId = 801, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 1, CategoriaContabileOreId = 1, ProfonditaMetri = 10, OreImmersione = 5 });
        repository.SaveServizioGiornaliero(old);
        var righe = repository.GetLibrettoSommozzatore(801);
        AssertEqual(3, righe.Count, "Storico completo personale");
        AssertEqual(3.5m, righe.Sum(r => r.OreImmersione), "Ore personali senza esterni omonimi");
        AssertEqual(new DateOnly(2024, 12, 31), righe[0].DataServizio, "Ordine cronologico storico");
        AssertEqual(2, repository.GetLibrettoSommozzatore(802).Count, "Altro operatore separato");
        var vm = new MainWindowViewModel();
        vm.LibrettoOperatore = vm.LibrettoOperatori.Single(p => p.PerId == 801);
        AssertEqual(3, vm.LibrettoImmersioni.Count, "Libretto nell'interfaccia");
        var print = new LibrettoSommozzatorePrintService();
        var paginator = print.BuildPaginator(repository.GetPersonaleById(801)!, righe);
        AssertEqual(2, paginator.PageCount, "Bozza libretto con anagrafica e storico");
        var texts = EsportaPagine(paginator, Path.GetFullPath("artifacts/verifica-2026-11-16/libretto"));
        AssertTrue(texts[1].Contains("31/12/2024"), "Immersione storica assente dalla stampa.");
        AssertTrue(!string.Join(" ", texts).Contains("Esterno omonimo"), "Esterno nel libretto personale.");
    });

    private static List<string> EsportaPagine(DocumentPaginator paginator, string prefix)
    {
        for (var i = 0; i < paginator.PageCount; i++)
        {
            var page = paginator.GetPage(i);
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(page.Size.Width), (int)Math.Ceiling(page.Size.Height), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(page.Visual);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create($"{prefix}-{i + 1}.png");
            encoder.Save(file);
        }
        var path = prefix + ".xps";
        using (var xps = new XpsDocument(path, FileAccess.Write)) XpsDocument.CreateXpsDocumentWriter(xps).Write(paginator);
        using var zip = ZipFile.OpenRead(path);
        var texts = new List<string>();
        foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".fpage")).OrderBy(e => int.Parse(Path.GetFileNameWithoutExtension(e.Name))))
        {
            using var stream = entry.Open();
            var xml = XDocument.Load(stream);
            texts.Add(string.Concat(xml.Descendants().Where(e => e.Name.LocalName == "Glyphs").Select(e => (string?)e.Attribute("UnicodeString"))));
        }
        return texts;
    }
}
