using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Views;

public partial class EventoCalendarioWindow : Window
{
    private readonly PersonaleRepository _repository = new();
    private readonly long _id;
    public DateTime DataSalvata { get; private set; }

    public EventoCalendarioWindow(EventoCalendario evento)
    {
        InitializeComponent();
        _id = evento.EventoId;
        DataSalvata = evento.Data;
        Intestazione.Text = _id == 0 ? "Nuovo evento" : "Dettagli evento";
        TitoloInput.Text = evento.Titolo;
        CategoriaInput.SelectedIndex = evento.Categoria == "Scadenza" ? 1 : 0;
        DataInput.SelectedDate = evento.Data;
        OraInput.Text = evento.Ora?.ToString(@"hh\:mm") ?? string.Empty;
        NoteInput.Text = evento.Note;
        CompletataInput.IsChecked = evento.Completata;
        EliminaButton.Visibility = _id == 0 ? Visibility.Collapsed : Visibility.Visible;
        AggiornaCategoria();
    }

    private void Categoria_Changed(object sender, SelectionChangedEventArgs e) => AggiornaCategoria();
    private void AggiornaCategoria()
    {
        if (CompletataInput is null) return;
        CompletataInput.Visibility = CategoriaInput.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Salva_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataInput.SelectedDate is not { } data) throw new InvalidOperationException("Inserisci una data valida.");
            TimeSpan? ora = null;
            if (!string.IsNullOrWhiteSpace(OraInput.Text))
            {
                if (!TimeSpan.TryParseExact(OraInput.Text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var parsed)
                    || parsed >= TimeSpan.FromDays(1)) throw new InvalidOperationException("Inserisci un orario valido nel formato HH:mm (00:00–23:59).");
                ora = parsed;
            }
            _repository.SaveEventoCalendario(new EventoCalendario
            {
                EventoId = _id, Titolo = TitoloInput.Text, Data = data, Ora = ora, Note = NoteInput.Text,
                Categoria = CategoriaInput.SelectedIndex == 1 ? "Scadenza" : "Appuntamento",
                Completata = CategoriaInput.SelectedIndex == 1 && CompletataInput.IsChecked == true,
            });
            DataSalvata = data;
            DialogResult = true;
        }
        catch (Exception ex) { ErroreText.Text = $"Evento non salvato: {ex.Message}"; }
    }

    private void Elimina_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Eliminare definitivamente questo evento?", "Elimina evento",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        try
        {
            _repository.DeleteEventoCalendario(_id);
            DialogResult = true;
        }
        catch (Exception ex) { ErroreText.Text = $"Evento non eliminato: {ex.Message}"; }
    }
}
