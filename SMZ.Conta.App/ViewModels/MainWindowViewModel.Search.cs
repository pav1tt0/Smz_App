using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Printing;

namespace SMZ.Conta.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private void AggiornaSuggerimentiRicerca()
    {
        SearchSuggestions.Clear();
        SelectedSearchSuggestion = null;

        if (string.IsNullOrWhiteSpace(FiltroCognome))
        {
            IsSearchSuggestionsOpen = false;
            return;
        }

        var testo = FiltroCognome.Trim();
        var testoNormalizzato = NormalizzaTestoRicerca(testo);
        var partiRicerca = testoNormalizzato
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var suggerimenti = _allSearchSuggestions
            .Select(item => new
            {
                Valore = item,
                Punteggio = CalcolaPunteggioSuggerimento(item, testoNormalizzato, partiRicerca),
            })
            .Where(item => item.Punteggio < int.MaxValue)
            .OrderBy(item => item.Punteggio)
            .ThenBy(item => item.Valore)
            .Take(8)
            .Select(item => item.Valore)
            .ToList();

        foreach (var suggerimento in suggerimenti)
        {
            SearchSuggestions.Add(suggerimento);
        }

        IsSearchSuggestionsOpen = SearchSuggestions.Count > 0;
    }

    private static int CalcolaPunteggioSuggerimento(string valore, string testoRicerca, string[] partiRicerca)
    {
        var valoreNormalizzato = NormalizzaTestoRicerca(valore);
        var parole = valoreNormalizzato.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (valoreNormalizzato.Equals(testoRicerca, StringComparison.Ordinal))
        {
            return 0;
        }

        if (valoreNormalizzato.StartsWith(testoRicerca, StringComparison.Ordinal))
        {
            return 1;
        }

        if (partiRicerca.Length > 0 && partiRicerca.All(parte => parole.Any(parola => parola.StartsWith(parte, StringComparison.Ordinal))))
        {
            return 2;
        }

        if (partiRicerca.Length > 0 && partiRicerca.All(parte => valoreNormalizzato.Contains(parte, StringComparison.Ordinal)))
        {
            return 3;
        }

        return int.MaxValue;
    }

    private static string NormalizzaTestoRicerca(string value)
    {
        var normalized = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private void RicaricaSuggerimentiRicerca()
    {
        _allSearchSuggestions.Clear();
        _allSearchSuggestions.AddRange(_repository.GetSearchSuggestions());
        AggiornaSuggerimentiRicerca();
    }

    private void PulisciEditorVisita()
    {
        if (SelectedVisita is not null)
        {
            CaricaEditorVisitaDaSelezione();
            return;
        }

        if (VisiteMediche.Count > 0)
        {
            SelectedVisita = VisiteMediche[0];
            return;
        }

        VisitaTipoSelezionato = null;
        VisitaDataUltimaVisita = string.Empty;
        VisitaEsito = string.Empty;
        VisitaNote = string.Empty;
    }

    private void CaricaEditorVisitaDaSelezione()
    {
        if (SelectedVisita is null)
        {
            if (VisiteMediche.Count > 0)
            {
                SelectedVisita = VisiteMediche[0];
                return;
            }

            VisitaTipoSelezionato = null;
            VisitaDataUltimaVisita = string.Empty;
            VisitaEsito = string.Empty;
            VisitaNote = string.Empty;
            return;
        }

        VisitaTipoSelezionato = TipiVisitaMedicaCatalogo.FirstOrDefault(tipo => tipo.Descrizione == SelectedVisita.TipoVisita);
        VisitaDataUltimaVisita = SelectedVisita.DataUltimaVisita;
        VisitaEsito = SelectedVisita.Esito;
        VisitaNote = SelectedVisita.Note;
    }

    private void AggiornaRiepilogoScheda()
    {
        OnPropertyChanged(nameof(SchedaRiepilogoTitolo));
        OnPropertyChanged(nameof(SchedaRiepilogoPerId));
        OnPropertyChanged(nameof(StatoServizioSchedaSintesi));
        OnPropertyChanged(nameof(SchedaAbilitazioniTotali));
        OnPropertyChanged(nameof(SchedaAbilitazioniPrincipali));
        OnPropertyChanged(nameof(SchedaAbilitazioniPrincipaliFooter));
        OnPropertyChanged(nameof(SchedaScadenzeTotali));
        OnPropertyChanged(nameof(SchedaScaduteTotali));
        OnPropertyChanged(nameof(SchedaHaScadute));
        OnPropertyChanged(nameof(SchedaScaduteTitolo));
        OnPropertyChanged(nameof(SchedaScaduteHighlight));
        OnPropertyChanged(nameof(SchedaScaduteDettaglio));
        OnPropertyChanged(nameof(SchedaVisiteTotali));
        OnPropertyChanged(nameof(SchedaProssimaScadenza));
        OnPropertyChanged(nameof(SchedaProssimaScadenzaDettaglio));
    }
}
