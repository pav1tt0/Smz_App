using System.Collections.ObjectModel;
using System.Windows;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Printing;

namespace SMZ.Conta.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private PersonaleListItemViewModel? _librettoOperatore;
    public ObservableCollection<PersonaleListItemViewModel> LibrettoOperatori { get; } = [];
    public ObservableCollection<RegistroImmersioneRiga> LibrettoImmersioni { get; } = [];
    public PersonaleListItemViewModel? LibrettoOperatore
    {
        get => _librettoOperatore;
        set { if (SetProperty(ref _librettoOperatore, value)) CaricaLibretto(); }
    }
    public string LibrettoAnagrafica { get; private set; } = "Seleziona un operatore.";
    public string LibrettoRiepilogo => $"{LibrettoImmersioni.Select(item => item.ServizioImmersioneId).Distinct().Count()} immersioni · {LibrettoImmersioni.Sum(item => item.OreImmersione):0.##} ore · intero archivio";
    public RelayCommand AggiornaLibrettoCommand => new(InizializzaLibretto);
    public RelayCommand StampaLibrettoCommand => new(StampaLibretto);

    private void InizializzaLibretto()
    {
        var selezionato = LibrettoOperatore?.PerId;
        LibrettoOperatori.Clear();
        foreach (var persona in _repository.SearchPersonale("", null, null).Where(p => !p.IsProfiloSanitario))
            LibrettoOperatori.Add(PersonaleListItemViewModel.FromModel(persona));
        LibrettoOperatore = LibrettoOperatori.FirstOrDefault(p => p.PerId == selezionato);
        CaricaLibretto();
    }

    private void CaricaLibretto()
    {
        LibrettoImmersioni.Clear();
        var persona = LibrettoOperatore is null ? null : _repository.GetPersonaleById(LibrettoOperatore.PerId);
        LibrettoAnagrafica = persona is null ? "Seleziona un operatore." :
            $"{persona.Qualifica} {persona.NominativoCompleto}\nPerID {persona.PerId} · Matricola {persona.MatricolaPersonale} · Brevetto SMZ {persona.NumeroBrevettoSmz}\nNato il {persona.DataNascita:dd/MM/yyyy} a {persona.LuogoNascita}";
        if (persona is not null)
            foreach (var riga in _repository.GetLibrettoSommozzatore(persona.PerId)) LibrettoImmersioni.Add(riga);
        OnPropertyChanged(nameof(LibrettoAnagrafica));
        OnPropertyChanged(nameof(LibrettoRiepilogo));
    }

    private void StampaLibretto()
    {
        if (LibrettoOperatore is null) { Stato = "Seleziona l'operatore per stampare il libretto."; return; }
        try
        {
            CaricaLibretto();
            var persona = _repository.GetPersonaleById(LibrettoOperatore.PerId)
                ?? throw new InvalidOperationException("Operatore non trovato.");
            new LibrettoSommozzatorePrintService().Print(persona, LibrettoImmersioni.ToList());
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Libretto del sommozzatore", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
