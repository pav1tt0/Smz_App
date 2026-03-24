using System.Windows.Media;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class ScadenzaItemViewModel
{
    public int PerId { get; init; }

    public string Nominativo { get; init; } = string.Empty;

    public string Origine { get; init; } = string.Empty;

    public string Titolo { get; init; } = string.Empty;

    public string Dettaglio { get; init; } = string.Empty;

    public string DataScadenza { get; init; } = string.Empty;

    public string GiorniResidui { get; init; } = string.Empty;

    public int GiorniResiduiNumero { get; init; }

    public bool IsExpired => GiorniResiduiNumero < 0;

    public bool IsUrgent => GiorniResiduiNumero >= 0 && GiorniResiduiNumero <= 7;

    public bool IsSoon => GiorniResiduiNumero > 7;

    public string StatoSintesi => GiorniResiduiNumero switch
    {
        < 0 => "Scaduta",
        <= 7 => "Urgente",
        _ => "Da monitorare",
    };

    public Brush CardBackground => GiorniResiduiNumero switch
    {
        < 0 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBEAEA")),
        <= 7 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF4DB")),
        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FBF7")),
    };

    public Brush BadgeBackground => GiorniResiduiNumero switch
    {
        < 0 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B94141")),
        <= 7 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1A344")),
        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6EFEA")),
    };

    public Brush BadgeForeground => GiorniResiduiNumero switch
    {
        < 0 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7F3")),
        <= 7 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5B430F")),
        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#37515B")),
    };

    public Brush TitoloForeground => GiorniResiduiNumero switch
    {
        < 0 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B2E2E")),
        <= 7 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7A5610")),
        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#395058")),
    };

    public static ScadenzaItemViewModel FromModel(ScadenzaProgrammata model)
    {
        return new ScadenzaItemViewModel
        {
            PerId = model.PerId,
            Nominativo = model.Nominativo,
            Origine = model.Origine,
            Titolo = model.Titolo,
            Dettaglio = string.IsNullOrWhiteSpace(model.Dettaglio) ? "Nessun dettaglio" : model.Dettaglio,
            DataScadenza = model.DataScadenza.ToString("dd/MM/yyyy"),
            GiorniResiduiNumero = model.GiorniResidui,
            GiorniResidui = model.GiorniResidui switch
            {
                < 0 => $"Scaduto da {Math.Abs(model.GiorniResidui)} gg",
                0 => "Scade oggi",
                _ => $"Tra {model.GiorniResidui} gg",
            }
        };
    }
}
