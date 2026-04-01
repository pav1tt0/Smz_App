using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class PersonaleAttagliamentoRowViewModel : ObservableObject
{
    private int? _personaleAttagliamentoId;
    private int _ordineScheda;
    private string _numeroScheda = string.Empty;
    private string _etichettaScheda = string.Empty;
    private string _unitaScheda = string.Empty;
    private bool _isPredefinita;
    private string _voce = string.Empty;
    private string _tagliaMisura = string.Empty;
    private string _note = string.Empty;

    public int? PersonaleAttagliamentoId
    {
        get => _personaleAttagliamentoId;
        set => SetProperty(ref _personaleAttagliamentoId, value);
    }

    public int OrdineScheda
    {
        get => _ordineScheda;
        set => SetProperty(ref _ordineScheda, value);
    }

    public string NumeroScheda
    {
        get => _numeroScheda;
        set => SetProperty(ref _numeroScheda, value);
    }

    public string EtichettaScheda
    {
        get => _etichettaScheda;
        set => SetProperty(ref _etichettaScheda, value);
    }

    public string UnitaScheda
    {
        get => _unitaScheda;
        set => SetProperty(ref _unitaScheda, value);
    }

    public bool IsPredefinita
    {
        get => _isPredefinita;
        set => SetProperty(ref _isPredefinita, value);
    }

    public string Voce
    {
        get => _voce;
        set => SetProperty(ref _voce, value);
    }

    public string TagliaMisura
    {
        get => _tagliaMisura;
        set => SetProperty(ref _tagliaMisura, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    public string Sintesi =>
        string.IsNullOrWhiteSpace(TagliaMisura)
            ? (string.IsNullOrWhiteSpace(Note) ? "Non indicata" : Note)
            : TagliaMisura;

    public static PersonaleAttagliamentoRowViewModel FromModel(PersonaleAttagliamento model)
    {
        var definizione = CatalogoAttagliamento.TrovaPerVoce(model.Voce);
        return new PersonaleAttagliamentoRowViewModel
        {
            PersonaleAttagliamentoId = model.PersonaleAttagliamentoId,
            OrdineScheda = definizione?.OrdineScheda ?? int.MaxValue,
            NumeroScheda = definizione?.NumeroScheda ?? string.Empty,
            EtichettaScheda = definizione?.EtichettaScheda ?? model.Voce,
            UnitaScheda = definizione?.UnitaScheda ?? string.Empty,
            IsPredefinita = definizione is not null,
            Voce = model.Voce,
            TagliaMisura = model.TagliaMisura,
            Note = model.Note,
        };
    }

    public static PersonaleAttagliamentoRowViewModel FromDefinition(
        MisuraAttagliamentoDefinizione definizione,
        PersonaleAttagliamentoRowViewModel? existing = null)
    {
        return new PersonaleAttagliamentoRowViewModel
        {
            PersonaleAttagliamentoId = existing?.PersonaleAttagliamentoId,
            OrdineScheda = definizione.OrdineScheda,
            NumeroScheda = definizione.NumeroScheda,
            EtichettaScheda = definizione.EtichettaScheda,
            UnitaScheda = definizione.UnitaScheda,
            IsPredefinita = true,
            Voce = definizione.Voce,
            TagliaMisura = existing?.TagliaMisura ?? string.Empty,
            Note = existing?.Note ?? string.Empty,
        };
    }

    public static PersonaleAttagliamentoRowViewModel FromDraft(
        int? personaleAttagliamentoId,
        string voce,
        string tagliaMisura,
        string note)
    {
        var definizione = CatalogoAttagliamento.TrovaPerVoce(voce);
        return new PersonaleAttagliamentoRowViewModel
        {
            PersonaleAttagliamentoId = personaleAttagliamentoId,
            OrdineScheda = definizione?.OrdineScheda ?? int.MaxValue,
            NumeroScheda = definizione?.NumeroScheda ?? string.Empty,
            EtichettaScheda = definizione?.EtichettaScheda ?? voce,
            UnitaScheda = definizione?.UnitaScheda ?? string.Empty,
            IsPredefinita = definizione is not null,
            Voce = voce,
            TagliaMisura = tagliaMisura,
            Note = note,
        };
    }
}
