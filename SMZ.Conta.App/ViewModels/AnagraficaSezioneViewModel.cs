using System.Collections.ObjectModel;

namespace SMZ.Conta.App.ViewModels;

public sealed class AnagraficaSezioneViewModel
{
    public AnagraficaSezioneViewModel(string titolo, IEnumerable<AnagraficaCampoViewModel> campi)
    {
        Titolo = titolo;
        Campi = new ObservableCollection<AnagraficaCampoViewModel>(campi);
    }

    public string Titolo { get; }

    public ObservableCollection<AnagraficaCampoViewModel> Campi { get; }
}

public sealed class AnagraficaCampoViewModel
{
    public AnagraficaCampoViewModel(string etichetta, string valore)
    {
        Etichetta = etichetta;
        Valore = string.IsNullOrWhiteSpace(valore) ? "Non valorizzato" : valore;
    }

    public string Etichetta { get; }

    public string Valore { get; }
}
