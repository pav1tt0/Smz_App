using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class PersonaleAbilitazioneRowViewModel : ObservableObject
{
    private int? _personaleAbilitazioneId;
    private int? _tipoAbilitazioneId;
    private string _tipoDescrizione = string.Empty;
    private string _categoria = string.Empty;
    private string _livello = string.Empty;
    private string _profonditaMetri = string.Empty;
    private string _dataConseguimento = string.Empty;
    private string _dataScadenza = string.Empty;
    private string _note = string.Empty;

    public int? PersonaleAbilitazioneId
    {
        get => _personaleAbilitazioneId;
        set => SetProperty(ref _personaleAbilitazioneId, value);
    }

    public int? TipoAbilitazioneId
    {
        get => _tipoAbilitazioneId;
        set => SetProperty(ref _tipoAbilitazioneId, value);
    }

    public string TipoDescrizione
    {
        get => _tipoDescrizione;
        set => SetProperty(ref _tipoDescrizione, value);
    }

    public string Categoria
    {
        get => _categoria;
        set => SetProperty(ref _categoria, value);
    }

    public string Livello
    {
        get => _livello;
        set
        {
            if (SetProperty(ref _livello, value))
            {
                OnPropertyChanged(nameof(DettaglioSintesi));
            }
        }
    }

    public string ProfonditaMetri
    {
        get => _profonditaMetri;
        set
        {
            if (SetProperty(ref _profonditaMetri, value))
            {
                OnPropertyChanged(nameof(DettaglioSintesi));
            }
        }
    }

    public string DataConseguimento
    {
        get => _dataConseguimento;
        set => SetProperty(ref _dataConseguimento, value);
    }

    public string DataScadenza
    {
        get => _dataScadenza;
        set
        {
            if (SetProperty(ref _dataScadenza, value))
            {
                OnPropertyChanged(nameof(ScadenzaSintesi));
            }
        }
    }

    public string Note
    {
        get => _note;
        set
        {
            if (SetProperty(ref _note, value))
            {
                OnPropertyChanged(nameof(DettaglioSintesi));
            }
        }
    }

    public string DettaglioSintesi
    {
        get
        {
            var parti = new List<string>();

            if (!string.IsNullOrWhiteSpace(Livello))
            {
                parti.Add(Livello);
            }

            if (!string.IsNullOrWhiteSpace(ProfonditaMetri))
            {
                parti.Add($"{ProfonditaMetri} m");
            }

            if (!string.IsNullOrWhiteSpace(Note))
            {
                parti.Add(Note);
            }

            return parti.Count == 0 ? "Nessun dettaglio" : string.Join(" | ", parti);
        }
    }

    public string ScadenzaSintesi => string.IsNullOrWhiteSpace(DataScadenza) ? "Nessuna scadenza" : DataScadenza;

    public static PersonaleAbilitazioneRowViewModel FromModel(PersonaleAbilitazione model)
    {
        return new PersonaleAbilitazioneRowViewModel
        {
            PersonaleAbilitazioneId = model.PersonaleAbilitazioneId,
            TipoAbilitazioneId = model.TipoAbilitazioneId,
            TipoDescrizione = model.Tipo?.Descrizione ?? string.Empty,
            Categoria = model.Tipo?.Categoria ?? string.Empty,
            Livello = model.Livello,
            ProfonditaMetri = model.ProfonditaMetri?.ToString() ?? string.Empty,
            DataConseguimento = FormatDate(model.DataConseguimento),
            DataScadenza = FormatDate(model.DataScadenza),
            Note = model.Note,
        };
    }

    public static PersonaleAbilitazioneRowViewModel FromDraft(
        TipoAbilitazione tipo,
        int? personaleAbilitazioneId,
        string livello,
        string profonditaMetri,
        string dataConseguimento,
        string dataScadenza,
        string note)
    {
        return new PersonaleAbilitazioneRowViewModel
        {
            PersonaleAbilitazioneId = personaleAbilitazioneId,
            TipoAbilitazioneId = tipo.TipoAbilitazioneId,
            TipoDescrizione = tipo.Descrizione,
            Categoria = tipo.Categoria,
            Livello = livello,
            ProfonditaMetri = profonditaMetri,
            DataConseguimento = dataConseguimento,
            DataScadenza = dataScadenza,
            Note = note,
        };
    }

    private static string FormatDate(DateOnly? value) => value?.ToString("dd/MM/yyyy") ?? string.Empty;
}
