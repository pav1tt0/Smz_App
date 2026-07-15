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
    private void PulisciFiltri()
    {
        FiltroCognome = string.Empty;
        FiltroAbilitazione = FiltroAbilitazioni.FirstOrDefault();
        FiltroVisiteEntro = string.Empty;
        SelectedPersonale = null;
        IsSearchSuggestionsOpen = false;
        CaricaElenco();
    }

    private void NavigaAllaSezione(object? parameter)
    {
        var parsed = parameter is int index
            ? index
            : parameter is not null && int.TryParse(parameter.ToString(), out var parsedValue)
                ? parsedValue
                : -1;
        if (parsed < 0) return;
        if (!IsAdministrator && parsed != PersonalSectionIndex)
        {
            SezioneAttivaIndex = PersonalSectionIndex;
            Stato = "Il profilo Base puo accedere esclusivamente alla sezione Personale.";
            return;
        }

        SezioneAttivaIndex = parsed;
    }
}
