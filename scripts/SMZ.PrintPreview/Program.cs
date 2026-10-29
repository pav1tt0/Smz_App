using System.Globalization;
using System.IO;
using System.Reflection;
using System.IO.Compression;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Printing;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("it-IT");
        var output = Path.GetFullPath(args.Length > 0 ? args[0] : "artifacts/stampe/anteprima-servizio");
        Directory.CreateDirectory(output);
        Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, Path.Combine(output, "db-esempio"));
        Directory.CreateDirectory(DatabasePaths.AppDataDirectory);
        _ = new Application();
        DatabaseInitializer.EnsureDatabase();
        var repository = new PersonaleRepository();
        string[] cognomi = ["Rossi", "Bianchi", "De Santis", "Verdi"];
        string[] nomi = ["Mario", "Luca", "Giovanni", "Andrea"];
        for (int i = 0; i < 4; i++)
            repository.SavePersonale(new Personale
            {
                PerId = 9001 + i, Cognome = cognomi[i], Nome = nomi[i],
                Qualifica = i == 0 ? "Ispettore" : "Assistente Capo", CodiceFiscale = $"ESEMPIO{i}",
            }, !repository.ExistsPersonale(9001 + i));

        var cataloghi = repository.GetCataloghiServizio();
        var gruppo = cataloghi.GruppiOperativi.Single(item => item.Codice == "SMZ").GruppoOperativoId;
        var servizio = new ServizioGiornaliero
        {
            DataServizio = new DateOnly(2026, 10, 29), NumeroOrdineServizio = "302", OrarioServizio = "08.00/14.00",
            ResponsabileServizioPerId = 9001, FuoriSede = true,
            LocalitaOperativaId = cataloghi.LocalitaOperative.First().LocalitaOperativaId,
            ScopoImmersioneId = cataloghi.ScopiImmersione.First().ScopoImmersioneId,
            UnitaNavaleId = cataloghi.UnitaNavali.First().UnitaNavaleId,
            AttivitaSvolta = "Verifica delle attrezzature e predisposizione del dispositivo di sicurezza. Immersione addestrativa con ricognizione del fondale, esercitazione di ricerca e recupero e controllo delle procedure di assistenza. Al termine, rientro in sede e ripristino dell'efficienza dei materiali.",
            Note = "Servizio concluso regolarmente. Nessuna variazione rispetto alla pianificazione.",
            StraordinarioAttivo = true, StraordinarioInizio = "14.00", StraordinarioFine = "16.00",
        };
        for (int i = 0; i < 4; i++)
            servizio.Partecipanti.Add(new ServizioPartecipante
            {
                ServizioPartecipanteId = 9001 + i, PerId = 9001 + i, Presente = true, GruppoOperativoId = gruppo,
            });
        var immersione = new ServizioImmersione
        {
            NumeroImmersione = 1, OrarioInizio = new TimeOnly(9, 30), OrarioFine = new TimeOnly(10, 15),
            DirettoreImmersionePerId = 9001, OperatoreSoccorsoPerId = 9004, AssistenteBlsdPerId = 9004,
        };
        foreach (int perId in new[] { 9002, 9003 })
            immersione.Partecipazioni.Add(new ServizioPartecipanteImmersione
            {
                ServizioPartecipanteId = perId,
                TipologiaImmersioneOperativaId = cataloghi.TipologieImmersione.Single(item => item.Codice == "ARA_ASAS").TipologiaImmersioneOperativaId,
                ProfonditaMetri = 18, OreImmersione = 0.75m,
            });
        servizio.Immersioni.Add(immersione);
        servizio.SupportiOccasionali.Add(new ServizioSupportoOccasionale
        {
            Nominativo = "Neri Paolo", Qualifica = "Ispettore tecnico", Ruolo = "Assistenza sanitaria", Presente = true,
        });
        Render(repository, servizio, output, "ordinario");
        Render(repository, servizio, output, "area-stampabile-ridotta", false, new Size(760, 1080));
        for (int i = 0; i < 12; i++)
            servizio.OperatoriSubEsterni.Add(new ServizioOperatoreSubEsterno
            {
                ServizioOperatoreSubEsternoId = 100 + i, Nominativo = $"Operatore esterno {i + 1:00} Cognome composto",
                Qualifica = "Assistente Capo", Reparto = "Reparto di supporto operativo", GruppoOperativoId = gruppo,
            });
        servizio.Note = string.Join("\n", Enumerable.Range(1, 18).Select(i => $"Fase {i:00}: verifica delle procedure operative, registrazione delle osservazioni e controllo delle attrezzature prima del successivo impiego."));
        Render(repository, servizio, output, "esteso");
        foreach (int linee in new[] { 0, 4, 8, 12, 24, 36 })
        {
            servizio.Note = string.Join("\n", Enumerable.Range(1, linee).Select(i => $"Fase {i:00}: verifica delle procedure operative, registrazione delle osservazioni e controllo delle attrezzature prima del successivo impiego."));
            Render(repository, servizio, output, $"verifica-{linee:00}", false);
        }
    }

    private static void Render(PersonaleRepository repository, ServizioGiornaliero servizio, string output, string name, bool images = true, Size? pageSize = null)
    {
        var method = typeof(ServizioGiornalieroPrintService).GetMethod("BuildDocument", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var document = (FlowDocument)method.Invoke(new ServizioGiornalieroPrintService(repository), [servizio])!;
        document.PageWidth = pageSize?.Width ?? 210 / 25.4 * 96;
        document.PageHeight = pageSize?.Height ?? 297 / 25.4 * 96;
        document.ColumnWidth = document.PageWidth - document.PagePadding.Left - document.PagePadding.Right;
        var buildPaginator = typeof(ServizioGiornalieroPrintService).GetMethod("BuildPaginator", BindingFlags.NonPublic | BindingFlags.Instance);
        var paginator = buildPaginator is null
            ? ((IDocumentPaginatorSource)document).DocumentPaginator
            : (DocumentPaginator)buildPaginator.Invoke(new ServizioGiornalieroPrintService(repository), [servizio, new Size(document.PageWidth, document.PageHeight)])!;
        paginator.ComputePageCount();
        Console.WriteLine($"{name}: {paginator.PageCount} pagine; corpo {document.FontSize}; margini {document.PagePadding}");
        for (int index = 0; images && index < paginator.PageCount; index++)
        {
            var page = paginator.GetPage(index);
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.White, null, new Rect(new Point(), page.Size));
                context.DrawRectangle(new VisualBrush(page.Visual), null, new Rect(new Point(), page.Size));
            }
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(page.Size.Width * 1.5), (int)Math.Ceiling(page.Size.Height * 1.5), 144, 144, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(output, $"{name}-{index + 1}.png"));
            encoder.Save(stream);
        }
        var xpsPath = Path.Combine(output, $"{name}.xps");
        using (var xps = new XpsDocument(xpsPath, FileAccess.Write))
            XpsDocument.CreateXpsDocumentWriter(xps).Write(paginator);
        using (var zip = ZipFile.OpenRead(xpsPath))
        {
            var pageTexts = new List<string>();
            foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".fpage")).OrderBy(e => int.Parse(Path.GetFileNameWithoutExtension(e.FullName))))
            {
                using var content = entry.Open();
                var xml = XDocument.Load(content);
                var text = string.Join(" ", xml.Descendants().Where(e => e.Name.LocalName == "Glyphs").Select(e => (string?)e.Attribute("UnicodeString")));
                pageTexts.Add(text);
                if (buildPaginator is not null && !text.Contains($"Pagina {pageTexts.Count} di {paginator.PageCount}"))
                    throw new InvalidOperationException($"{name}: numerazione pagina assente o errata.");
                if (text.Contains("Il Responsabile del Team") && !text.Contains("Premesso quanto sopra"))
                    throw new InvalidOperationException($"{name}: firma separata dal riepilogo.");
            }
            var fullText = Regex.Replace(string.Join(" ", pageTexts), @"\s+", string.Empty);
            foreach (var required in new[] { servizio.AttivitaSvolta, "Il Responsabile del Team", "Rossi Mario", "Bianchi Luca", "De Santis Giovanni", "Verdi Andrea", "Neri Paolo" }.Concat(servizio.Note.Split('\n')).Concat(servizio.OperatoriSubEsterni.Select(item => item.Nominativo)))
                if (!fullText.Contains(Regex.Replace(required, @"\s+", string.Empty)))
                    throw new InvalidOperationException($"{name}: contenuto mancante: {required}");
            if (name == "ordinario" && paginator.PageCount != 1)
                throw new InvalidOperationException("Il caso ordinario deve rimanere su una pagina.");
            File.WriteAllText(Path.Combine(output, $"{name}-pagine.txt"), string.Join("\n\nCAMBIO PAGINA\n\n", pageTexts));
        }
        File.WriteAllText(Path.Combine(output, $"{name}-testo.txt"), new TextRange(document.ContentStart, document.ContentEnd).Text);
    }
}
