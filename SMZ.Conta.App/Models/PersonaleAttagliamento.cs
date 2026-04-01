namespace SMZ.Conta.App.Models;

public sealed class PersonaleAttagliamento
{
    public int PersonaleAttagliamentoId { get; set; }

    public int PerId { get; set; }

    public string Voce { get; set; } = string.Empty;

    public string TagliaMisura { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}
