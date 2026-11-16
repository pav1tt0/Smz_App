using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.ViewModels;
using SMZ.Conta.App.Views;

namespace SMZ.Conta.Tests;

internal static partial class Program
{
    private static void TestCalendarioPersistenza() => ConDatabaseServiziIsolato(repository =>
    {
        var data = DateTime.Today;
        var evento = new EventoCalendario { Titolo = "Riunione d'ufficio; DROP TABLE Personale;", Data = data,
            Note = "Note libere\nseconda riga", Ora = new TimeSpan(9, 30, 0) };
        var id = repository.SaveEventoCalendario(evento);
        AssertTrue(id > 0, "Identificativo evento automatico");
        SqliteConnection.ClearAllPools();
        DatabaseInitializer.EnsureDatabase();
        repository = new PersonaleRepository();
        var letto = repository.GetEventiCalendario(data, data).Single();
        AssertEqual(evento.Titolo, letto.Titolo, "Titolo letterale dopo riapertura");
        AssertEqual(evento.Note, letto.Note, "Note multilinea persistenti");
        AssertEqual(evento.Ora, letto.Ora, "Orario persistente");
        letto.Categoria = "Scadenza";
        letto.Completata = true;
        letto.Data = data.AddDays(-2);
        letto.Ora = null;
        repository.SaveEventoCalendario(letto);
        var aggiornato = repository.GetEventiCalendario(data.AddDays(-2), data).Single();
        AssertTrue(aggiornato.Completata && !aggiornato.IsScaduta && aggiornato.Ora is null, "Modifica e completamento persistenti");
        AssertEqual(0, repository.GetEventiCalendario(data, data).Count, "Evento spostato dalla vecchia data");
        aggiornato.Titolo = " ";
        AssertThrows(() => repository.SaveEventoCalendario(aggiornato), "Titolo vuoto accettato");
        aggiornato.Titolo = "Modifica non valida";
        aggiornato.Ora = TimeSpan.FromHours(24);
        AssertThrows(() => repository.SaveEventoCalendario(aggiornato), "Ora non valida accettata");
        AssertEqual(evento.Titolo, repository.GetEventiCalendario(data.AddDays(-2), data).Single().Titolo, "Errore conserva il dato salvato");
        aggiornato.Ora = null;
        aggiornato.Categoria = "Appuntamento";
        repository.SaveEventoCalendario(aggiornato);
        AssertTrue(!repository.GetEventiCalendario(data.AddDays(-2), data).Single().Completata, "Appuntamento senza completamento scadenza");
        repository.DeleteEventoCalendario(id);
        AssertEqual(0, new PersonaleRepository().GetEventiCalendario(data.AddDays(-2), data).Count, "Eliminazione persistente");
        AssertThrows(() => repository.SaveEventoCalendario(aggiornato), "Evento eliminato ricreato da una modifica obsoleta");
    });

    private static void TestCalendarioBackup() => ConDatabaseServiziIsolato(repository =>
    {
        var persona = CreaPersonale(915, "Calendario", "Backup", "CALENDARIO915", "calendario915");
        repository.SavePersonale(persona, true);
        var backup = new BackupService();
        using (var connection = new SqliteConnection(DatabasePaths.ConnectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE EventiCalendario;";
            command.ExecuteNonQuery();
        }
        var precedente = backup.CreateLocalBackup("calendario-prima-migrazione");
        DatabaseInitializer.EnsureDatabase();
        var id = repository.SaveEventoCalendario(new EventoCalendario { Titolo = "Scadenza da recuperare", Categoria = "Scadenza",
            Data = DateTime.Today.AddDays(-1), Completata = true, Note = "Inclusa nel backup" });
        DatabaseInitializer.EnsureDatabase();
        AssertEqual(id, repository.GetEventiCalendario(DateTime.Today.AddDays(-1), DateTime.Today).Single().EventoId, "Migrazione idempotente");
        var corrente = backup.CreateLocalBackup("calendario-con-eventi");
        repository.DeleteEventoCalendario(id);
        backup.RestoreBackup(corrente.BackupPath);
        var recuperato = new PersonaleRepository().GetEventiCalendario(DateTime.Today.AddDays(-1), DateTime.Today).Single();
        AssertTrue(recuperato.Completata && recuperato.Note == "Inclusa nel backup", "Backup e ripristino comprendono gli eventi");
        backup.RestoreBackup(precedente.BackupPath);
        DatabaseInitializer.EnsureDatabase();
        AssertEqual(0, repository.GetEventiCalendario(DateTime.Today.AddDays(-1), DateTime.Today).Count, "Backup precedente al calendario ancora compatibile");
        AssertEqual("Calendario", repository.GetPersonaleById(915)!.Cognome, "Anagrafica conservata durante migrazione e ripristini");
    });

    private static void TestCalendarioNavigazione() => ConDatabaseServiziIsolato(repository =>
    {
        var vm = new CalendarioRepartoViewModel();
        vm.SelezionaData(new DateTime(2024, 12, 31));
        AssertEqual(DayOfWeek.Monday, vm.Giorni.First().Data!.Value.DayOfWeek, "Settimana da lunedì");
        vm.MeseSuccessivoCommand.Execute(null);
        AssertEqual(new DateTime(2025, 1, 1), vm.Mese, "Navigazione al nuovo anno");
        vm.MesePrecedenteCommand.Execute(null);
        AssertEqual(new DateTime(2024, 12, 1), vm.Mese, "Ritorno all'anno precedente");
        vm.SelezionaData(new DateTime(2024, 1, 31));
        vm.MeseSuccessivoCommand.Execute(null);
        AssertEqual(new DateTime(2024, 2, 29), vm.GiornoSelezionato, "Ultimo giorno del mese bisestile");
        vm.OggiCommand.Execute(null);
        AssertEqual(DateTime.Today, vm.GiornoSelezionato, "Ritorno a oggi");
        AssertTrue(vm.Giorni.Single(g => g.Data == DateTime.Today).IsOggi, "Oggi evidenziato");
        var data = DateTime.Today.AddDays(-1);
        repository.SaveEventoCalendario(new EventoCalendario { Titolo = "Senza orario", Data = data });
        repository.SaveEventoCalendario(new EventoCalendario { Titolo = "Appuntamento passato", Data = data, Ora = TimeSpan.FromHours(9) });
        repository.SaveEventoCalendario(new EventoCalendario { Titolo = "Scadenza passata", Data = data, Categoria = "Scadenza", Ora = TimeSpan.FromHours(10) });
        repository.SaveEventoCalendario(new EventoCalendario { Titolo = "Scadenza completata", Data = data, Categoria = "Scadenza", Completata = true });
        vm.SelezionaGiornoCommand.Execute(data);
        AssertEqual("Appuntamento passato", vm.EventiGiorno.First().Titolo, "Eventi ordinati per orario");
        AssertTrue(!vm.EventiGiorno.First().IsScaduta, "Appuntamenti passati non scaduti");
        AssertTrue(vm.EventiGiorno.Single(e => e.Titolo == "Scadenza passata").IsScaduta, "Scadenze precedenti non completate in rosso");
        AssertTrue(!vm.EventiGiorno.Single(e => e.Completata).IsScaduta, "Scadenza completata non superata");
        AssertEqual(3, vm.Giorni.Single(g => g.Data == data).Indicatori.Count, "Indicatori per categorie e stato");
        using (var connection = new SqliteConnection(DatabasePaths.ConnectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "ALTER TABLE EventiCalendario RENAME TO EventiCalendarioNonDisponibili;";
            command.ExecuteNonQuery();
            vm.Ricarica();
            AssertTrue(vm.Errore.Length > 0 && vm.EventiGiorno.Count == 4, "Errore di lettura visibile senza svuotare gli eventi");
            vm.SelezionaData(data.AddMonths(1));
            AssertEqual(data, vm.GiornoSelezionato, "Lettura fallita conserva la data degli eventi visualizzati");
            command.CommandText = "ALTER TABLE EventiCalendarioNonDisponibili RENAME TO EventiCalendario;";
            command.ExecuteNonQuery();
        }
        vm.Ricarica();
        AssertEqual(string.Empty, vm.Errore, "Ripresa della lettura");
    });

    private static IEnumerable<T> DiscendentiCalendario<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found) yield return found;
            foreach (var nested in DiscendentiCalendario<T>(child)) yield return nested;
        }
    }

    private static void TestDashboardCalendario() => ConDatabaseServiziIsolato(repository =>
    {
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Application.Current.Resources["BoolToVisibilityConverter"] = new BooleanToVisibilityConverter();
        var window = new SMZ.Conta.App.MainWindow(new AccessSession(0, "Collaudo", AccessRole.Administrator, false))
        { ShowInTaskbar = false, Left = -12000, Top = -12000, WindowState = WindowState.Normal, WindowStartupLocation = WindowStartupLocation.Manual };
        try
        {
            window.Show();
            window.UpdateLayout();
            Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            var vm = (MainWindowViewModel)window.DataContext;
            var homeAperta = DiscendentiCalendario<HomeView>(window).Single();
            var home = new HomeView { DataContext = vm };
            home.Resources.MergedDictionaries.Add(window.Resources);
            Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            home.Measure(new Size(1050, double.PositiveInfinity));
            home.Arrange(new Rect(0, 0, 1050, home.DesiredSize.Height));
            var cards = DiscendentiCalendario<AdaptiveUniformGrid>(home).Single();
            AssertEqual(6, cards.Children.Count, "Sei moduli nella dashboard");
            var parametri = cards.Children.OfType<Button>().Select(b => b.CommandParameter.ToString()).ToArray();
            AssertTrue(parametri.SequenceEqual(new[] { "1", "2", "4", "3", "5", "6" }), "Ordine e destinazioni moduli");
            foreach (var button in cards.Children.OfType<Button>())
            {
                button.Command.Execute(button.CommandParameter);
                AssertEqual(int.Parse(button.CommandParameter.ToString()!), vm.SezioneAttivaIndex, "Navigazione modulo");
            }
            vm.NavigateSectionCommand.Execute(0);
            window.UpdateLayout();
            var scadute = vm.DashboardVisiteScadutePersonale;
            var inScadenza = vm.DashboardVisiteInScadenzaPersonale;
            var calendarioAperto = DiscendentiCalendario<CalendarioRepartoView>(homeAperta).Single();
            var host = (StackPanel)calendarioAperto.FindName("CalendarioHost");
            AssertTrue(host.DataContext is CalendarioRepartoViewModel { Giorni.Count: 42, Errore.Length: 0 }, "Calendario caricato nell'app aperta");
            var calendario = DiscendentiCalendario<CalendarioRepartoView>(home).Single();
            ((StackPanel)calendario.FindName("CalendarioHost")).DataContext = host.DataContext;
            foreach (var width in new[] { 360d, 680d, 1000d, 1380d })
            foreach (var zoom in new[] { 0.7, 0.8, 0.9, 1.0, 1.5 })
            {
                home.Measure(new Size(width / zoom, double.PositiveInfinity));
                home.Arrange(new Rect(0, 0, width / zoom, home.DesiredSize.Height));
                home.UpdateLayout();
                AssertTrue(home.DesiredSize.Width <= width / zoom + 1, $"Dashboard entro la larghezza disponibile allo zoom: larghezza={width / zoom}, richiesta={home.DesiredSize.Width}");
                foreach (var button in DiscendentiCalendario<Button>(calendario))
                {
                    var bounds = button.TransformToAncestor(home).TransformBounds(new Rect(button.RenderSize));
                    AssertTrue(bounds.Left >= -1 && bounds.Right <= width / zoom + 1 && button.ActualWidth >= 16,
                        $"Pulsante calendario visibile entro i bordi: {button.Content}, larghezza={width / zoom}, destra={bounds.Right}");
                }
                if (width / zoom >= 1000) AssertEqual(3, cards.Columns, "Tre colonne su schermo ampio");
                if (width / zoom < 460) AssertEqual(1, cards.Columns, "Una colonna su schermo stretto");
            }
            foreach (var zoom in new[] { 0.8, 0.9, 1.0 })
                SalvaAnteprimaDashboard(home, zoom, $"dashboard-{zoom * 100:0}.png");
            SalvaAnteprimaDashboard(home, 1, "dashboard.png");
            var calendarioVm = (CalendarioRepartoViewModel)host.DataContext;
            calendarioVm.SelezionaData(DateTime.Today.AddDays(DateTime.Today.Day == 1 ? 1 : -1));
            SalvaAnteprimaDashboard(home, 1, "dashboard-selezione.png");
            calendarioVm.OggiCommand.Execute(null);
            AssertEqual(scadute, vm.DashboardVisiteScadutePersonale, "Conteggio scadute invariato");
            AssertEqual(inScadenza, vm.DashboardVisiteInScadenzaPersonale, "Conteggio in scadenza invariato");

            var editor = new EventoCalendarioWindow(new EventoCalendario { Data = DateTime.Today }) { Owner = window, ShowInTaskbar = false };
            editor.Dispatcher.BeginInvoke(() =>
            {
                ((TextBox)editor.FindName("TitoloInput")).Text = "Evento salvato dall'editor";
                typeof(EventoCalendarioWindow).GetMethod("Salva_Click", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(editor, new object[] { editor, new RoutedEventArgs() });
                if (editor.IsVisible) editor.Close();
            }, DispatcherPriority.ApplicationIdle);
            AssertTrue(editor.ShowDialog() == true, "Salvataggio dalla finestra evento");
            AssertEqual("Evento salvato dall'editor", repository.GetEventiCalendario(DateTime.Today, DateTime.Today).Single().Titolo, "Evento dell'editor persistente");
            repository.SaveEventoCalendario(new EventoCalendario { Titolo = "Verifica delle attrezzature del reparto", Data = DateTime.Today, Categoria = "Scadenza" });
            calendarioVm.Ricarica();
            SalvaAnteprimaDashboard(home, 1, "dashboard-eventi.png");

            var baseVm = new MainWindowViewModel(new AccessSession(0, "Base", AccessRole.Base, false));
            foreach (var index in new[] { 0, 1, 3, 4, 5, 6 })
            {
                baseVm.NavigateSectionCommand.Execute(index);
                AssertEqual(2, baseVm.SezioneAttivaIndex, "Accessi Base ancora limitati a Personale");
            }
            home.DataContext = baseVm;
            AssertEqual(Visibility.Collapsed, cards.Children.OfType<Button>().Single(b => b.CommandParameter.ToString() == "6").Visibility, "Card Amministrazione nascosta al profilo Base");
        }
        finally { window.Close(); }
    });

    private static void SalvaAnteprimaDashboard(HomeView home, double zoom, string nome)
    {
        home.Measure(new Size(1050 / zoom, double.PositiveInfinity));
        home.Arrange(new Rect(0, 0, 1050 / zoom, home.DesiredSize.Height));
        home.UpdateLayout();
        var bitmap = new RenderTargetBitmap(1050, (int)Math.Ceiling(home.ActualHeight * zoom), 96 * zoom, 96 * zoom, PixelFormats.Pbgra32);
        bitmap.Render(home);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var output = Path.Combine(Environment.CurrentDirectory, "artifacts", "dashboard-calendario");
        Directory.CreateDirectory(output);
        using var stream = File.Create(Path.Combine(output, nome));
        encoder.Save(stream);
    }
}
