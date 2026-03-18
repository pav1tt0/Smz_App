using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class ScadenzaItemViewModel
{
    public string Nominativo { get; init; } = string.Empty;

    public string Origine { get; init; } = string.Empty;

    public string Titolo { get; init; } = string.Empty;

    public string Dettaglio { get; init; } = string.Empty;

    public string DataScadenza { get; init; } = string.Empty;

    public string GiorniResidui { get; init; } = string.Empty;

    public int GiorniResiduiNumero { get; init; }

    public bool IsUrgent => GiorniResiduiNumero >= 0 && GiorniResiduiNumero <= 7;

    public static ScadenzaItemViewModel FromModel(ScadenzaProgrammata model)
    {
        return new ScadenzaItemViewModel
        {
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
