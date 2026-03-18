namespace SMZ.Conta.App.Models;

public sealed class PersonaleAbilitazione
{
    public int PersonaleAbilitazioneId { get; set; }

    public int PerId { get; set; }

    public int TipoAbilitazioneId { get; set; }

    public TipoAbilitazione? Tipo { get; set; }

    public string Livello { get; set; } = string.Empty;

    public int? ProfonditaMetri { get; set; }

    public DateOnly? DataConseguimento { get; set; }

    public DateOnly? DataScadenza { get; set; }

    public string Note { get; set; } = string.Empty;
}
