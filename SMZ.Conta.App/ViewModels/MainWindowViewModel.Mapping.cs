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
    private void ApplicaOrarioServizioSalvato(string value)
    {
        var valore = value.Trim();
        _servizioOrarioFissoSelezionato = string.Empty;
        _servizioOrarioDerogaAttiva = false;
        _servizioOrarioDerogaInizio = string.Empty;
        _servizioOrarioDerogaFine = string.Empty;

        if (!string.IsNullOrWhiteSpace(valore))
        {
            var orarioFisso = OrariServizioFissiDisponibili
                .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item) && IsMatchingTimeRange(item, valore));

            if (!string.IsNullOrWhiteSpace(orarioFisso))
            {
                _servizioOrarioFissoSelezionato = orarioFisso;
            }
            else
            {
                _servizioOrarioDerogaAttiva = true;
                if (TryExtractTimeRange(valore, out var inizio, out var fine))
                {
                    _servizioOrarioDerogaInizio = inizio;
                    _servizioOrarioDerogaFine = fine;
                }
            }
        }

        OnPropertyChanged(nameof(ServizioOrarioFissoSelezionato));
        OnPropertyChanged(nameof(ServizioOrarioDerogaAttiva));
        OnPropertyChanged(nameof(ServizioOrarioDerogaInizio));
        OnPropertyChanged(nameof(ServizioOrarioDerogaFine));
    }

    private void AggiornaValoreOrarioServizio()
    {
        var nuovoValore = BuildServizioOrarioValue();
        if (!string.Equals(_servizioOrario, nuovoValore, StringComparison.Ordinal))
        {
            _servizioOrario = nuovoValore;
        }

        AggiornaRiepilogoBozzaServizio();
    }

    private string BuildServizioOrarioValue()
    {
        if (ServizioOrarioDerogaAttiva)
        {
            return BuildTimeRangeDisplay(ServizioOrarioDerogaInizio, ServizioOrarioDerogaFine);
        }

        return ServizioOrarioFissoSelezionato?.Trim() ?? string.Empty;
    }

    private static string BuildTimeRangeDisplay(string start, string end)
    {
        var normalizedStart = NormalizeTimeTextForStorage(start);
        var normalizedEnd = NormalizeTimeTextForStorage(end);

        if (string.IsNullOrWhiteSpace(normalizedStart) && string.IsNullOrWhiteSpace(normalizedEnd))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(normalizedStart) || string.IsNullOrWhiteSpace(normalizedEnd))
        {
            return string.Empty;
        }

        return $"{normalizedStart.Replace(':', '.')}/{normalizedEnd.Replace(':', '.')}";
    }

    private static string NormalizeTimeTextForStorage(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return TryNormalizeTimeInput(value, out var normalized) ? normalized : value.Trim();
    }

    private static bool TryNormalizeTimeInput(string value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = new string(value.Where(char.IsDigit).Take(4).ToArray());
        if (digits.Length == 3)
        {
            digits = $"0{digits}";
        }

        if (digits.Length != 4)
        {
            var sanitized = value.Trim().Replace('.', ':');
            if (string.Equals(sanitized, "24:00", StringComparison.Ordinal))
            {
                normalized = sanitized;
                return true;
            }

            if (TimeOnly.TryParse(sanitized, out var parsedFallback))
            {
                normalized = parsedFallback.ToString("HH:mm");
                return true;
            }

            return false;
        }

        var hours = int.Parse(digits[..2], CultureInfo.InvariantCulture);
        var minutes = int.Parse(digits[2..], CultureInfo.InvariantCulture);

        if (hours == 24 && minutes == 0)
        {
            normalized = "24:00";
            return true;
        }

        if (hours is < 0 or > 23 || minutes is < 0 or > 59)
        {
            return false;
        }

        normalized = $"{hours:00}:{minutes:00}";
        return true;
    }

    private static bool TryExtractTimeRange(string value, out string start, out string end)
    {
        start = string.Empty;
        end = string.Empty;

        var parts = Regex.Matches(value, @"\d{1,2}[\.:]?\d{2}")
            .Select(match => match.Value)
            .Take(2)
            .ToList();

        if (parts.Count < 2)
        {
            return false;
        }

        if (!TryNormalizeTimeInput(parts[0], out start) || !TryNormalizeTimeInput(parts[1], out end))
        {
            start = string.Empty;
            end = string.Empty;
            return false;
        }

        return true;
    }

    private static bool IsMatchingTimeRange(string expected, string actual)
    {
        return TryExtractTimeRange(expected, out var expectedStart, out var expectedEnd)
               && TryExtractTimeRange(actual, out var actualStart, out var actualEnd)
               && string.Equals(expectedStart, actualStart, StringComparison.Ordinal)
               && string.Equals(expectedEnd, actualEnd, StringComparison.Ordinal);
    }

    private static decimal? CalcolaDurataOre(string start, string end)
    {
        if (!TryNormalizeTimeInput(start, out var normalizedStart) || !TryNormalizeTimeInput(end, out var normalizedEnd))
        {
            return null;
        }

        var startMinutes = ToMinutes(normalizedStart);
        var endMinutes = ToMinutes(normalizedEnd);
        if (startMinutes is null || endMinutes is null)
        {
            return null;
        }

        var durationMinutes = endMinutes.Value - startMinutes.Value;
        if (durationMinutes < 0)
        {
            durationMinutes += 24 * 60;
        }

        return durationMinutes / 60m;
    }

    private static int? ToMinutes(string normalizedTime)
    {
        if (string.IsNullOrWhiteSpace(normalizedTime))
        {
            return null;
        }

        if (string.Equals(normalizedTime, "24:00", StringComparison.Ordinal))
        {
            return 24 * 60;
        }

        if (!TimeOnly.TryParseExact(normalizedTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return null;
        }

        return parsed.Hour * 60 + parsed.Minute;
    }

    private void SalvaAttagliamentoInEditor()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(AttagliamentoVoce))
            {
                throw new InvalidOperationException("La voce attagliamento e obbligatoria.");
            }

            if (string.IsNullOrWhiteSpace(AttagliamentoTagliaMisura) && string.IsNullOrWhiteSpace(AttagliamentoNote))
            {
                throw new InvalidOperationException("Indica almeno una taglia/misura oppure una nota.");
            }

            var nuovaRiga = PersonaleAttagliamentoRowViewModel.FromDraft(
                SelectedAttagliamento?.PersonaleAttagliamentoId,
                AttagliamentoVoce.Trim(),
                AttagliamentoTagliaMisura.Trim(),
                AttagliamentoNote.Trim());

            if (SelectedAttagliamento is null)
            {
                Attagliamento.Add(nuovaRiga);
            }
            else
            {
                var index = Attagliamento.IndexOf(SelectedAttagliamento);
                if (index >= 0)
                {
                    Attagliamento[index] = nuovaRiga;
                    SelectedAttagliamento = Attagliamento[index];
                }
            }

            PulisciEditorAttagliamento();
            Stato = "Attagliamento pronto in scheda. Salvare il personale per registrarlo nel database.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Attagliamento", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Attagliamento non aggiunto";
        }
    }

    private void PulisciEditorAttagliamento()
    {
        _selectedAttagliamento = null;
        OnPropertyChanged(nameof(SelectedAttagliamento));
        OnPropertyChanged(nameof(AzioneAttagliamentoLabel));

        AttagliamentoVoce = string.Empty;
        AttagliamentoTagliaMisura = string.Empty;
        AttagliamentoNote = string.Empty;
    }

    private void CaricaEditorAttagliamentoDaSelezione()
    {
        if (SelectedAttagliamento is null)
        {
            AttagliamentoVoce = string.Empty;
            AttagliamentoTagliaMisura = string.Empty;
            AttagliamentoNote = string.Empty;
            return;
        }

        AttagliamentoVoce = SelectedAttagliamento.Voce;
        AttagliamentoTagliaMisura = SelectedAttagliamento.TagliaMisura;
        AttagliamentoNote = SelectedAttagliamento.Note;
    }

    private void RimuoviAttagliamentoRiga()
    {
        if (SelectedAttagliamento is null)
        {
            return;
        }

        Attagliamento.Remove(SelectedAttagliamento);
        PulisciEditorAttagliamento();
    }

    private ServizioGiornaliero BuildServizioGiornalieroModel()
    {
        var dataServizio = ParseDate(ServizioData, "Data servizio")
            ?? throw new InvalidOperationException("La data servizio e obbligatoria.");
        var orarioServizio = BuildServizioOrarioValue();

        if (!TipiServizioDisponibili.Any(item => string.Equals(item, ServizioTipoSelezionato, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Seleziona un tipo servizio valido.");
        }

        if (ServizioOrarioDerogaAttiva)
        {
            if (string.IsNullOrWhiteSpace(ServizioOrarioDerogaInizio) || string.IsNullOrWhiteSpace(ServizioOrarioDerogaFine))
            {
                throw new InvalidOperationException("Per l'orario in deroga indica sia l'inizio sia la fine.");
            }

            if (!TryNormalizeTimeInput(ServizioOrarioDerogaInizio, out _) || !TryNormalizeTimeInput(ServizioOrarioDerogaFine, out _))
            {
                throw new InvalidOperationException("Orario in deroga non valido. Usa il formato HH:mm.");
            }
        }

        if (ServizioStraordinarioAttivo)
        {
            if (string.IsNullOrWhiteSpace(ServizioStraordinarioInizio) || string.IsNullOrWhiteSpace(ServizioStraordinarioFine))
            {
                throw new InvalidOperationException("Per il lavoro straordinario indica sia l'inizio sia la fine.");
            }

            if (CalcolaDurataOre(ServizioStraordinarioInizio, ServizioStraordinarioFine) is null)
            {
                throw new InvalidOperationException("Orari straordinario non validi. Usa il formato HH:mm.");
            }
        }

        if (ServizioFuoriSede && ServizioIndennitaOrdinePubblico)
        {
            throw new InvalidOperationException("Indennita fuori sede e indennita ordine pubblico non sono compatibili tra loro.");
        }

        var partecipanti = BuildServizioPartecipanti();
        var operatoriSubEsterni = BuildOperatoriSubEsterni();
        var supportiOccasionali = BuildSupportiOccasionali();

        if (partecipanti.Count == 0 && operatoriSubEsterni.Count == 0 && supportiOccasionali.Count == 0)
        {
            throw new InvalidOperationException("Inserisci almeno un partecipante o una Assistenza SMZ nel servizio.");
        }

        var immersioni = BuildServizioImmersioni(partecipanti, operatoriSubEsterni);

        return new ServizioGiornaliero
        {
            ServizioGiornalieroId = _servizioGiornalieroId,
            DataServizio = dataServizio,
            NumeroOrdineServizio = ServizioNumeroOrdine.Trim(),
            OrarioServizio = orarioServizio,
            StraordinarioAttivo = ServizioStraordinarioAttivo,
            StraordinarioInizio = ServizioStraordinarioAttivo ? NormalizeTimeTextForStorage(ServizioStraordinarioInizio) : string.Empty,
            StraordinarioFine = ServizioStraordinarioAttivo ? NormalizeTimeTextForStorage(ServizioStraordinarioFine) : string.Empty,
            TipoServizio = ServizioTipoSelezionato,
            LocalitaOperativaId = ServizioLocalitaSelezionata?.LocalitaOperativaId,
            ScopoImmersioneId = ServizioScopoSelezionato?.ScopoImmersioneId,
            UnitaNavaleId = ServizioUnitaNavaleSelezionata is { UnitaNavaleId: > 0 } unita ? unita.UnitaNavaleId : null,
            ResponsabileServizioPerId = GetPerIdOperatoreSelezionato(ServizioResponsabileSelezionato),
            FuoriSede = ServizioFuoriSede,
            IndennitaOrdinePubblico = ServizioIndennitaOrdinePubblico,
            AttivitaSvolta = ServizioAttivitaSvolta.Trim(),
            Note = ServizioNote.Trim(),
            Partecipanti = partecipanti,
            OperatoriSubEsterni = operatoriSubEsterni,
            Immersioni = immersioni,
            SupportiOccasionali = supportiOccasionali,
        };
    }

    private List<ServizioPartecipante> BuildServizioPartecipanti()
    {
        var items = new List<ServizioPartecipante>();

        foreach (var row in ServizioPartecipantiBozza)
        {
            var includeRow = IsPartecipanteInternoCompilato(row);

            if (!includeRow)
            {
                continue;
            }

            if (row.GruppoOperativo is null)
            {
                throw new InvalidOperationException($"{row.Nominativo}: selezionare il gruppo operativo.");
            }

            items.Add(new ServizioPartecipante
            {
                PerId = row.PerId,
                GruppoOperativoId = row.GruppoOperativo.GruppoOperativoId,
                Presente = row.Presente,
                RuoloOperativoId = row.RuoloOperativo?.RuoloOperativoId,
                Note = row.Note.Trim(),
            });
        }

        return items;
    }

    private List<ServizioSupportoOccasionale> BuildSupportiOccasionali()
    {
        var items = new List<ServizioSupportoOccasionale>();

        foreach (var row in ServizioSupportiOccasionaliBozza)
        {
            var includeRow = IsSupportoOccasionaleCompilato(row);
            if (!includeRow)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Nominativo))
            {
                throw new InvalidOperationException("Per ogni supporto occasionale il nominativo e obbligatorio.");
            }

            items.Add(new ServizioSupportoOccasionale
            {
                Nominativo = row.Nominativo.Trim(),
                Qualifica = row.Qualifica.Trim(),
                Ruolo = row.Ruolo.Trim(),
                Presente = row.Presente,
                Contatti = row.Contatti.Trim(),
                Note = row.Note.Trim(),
            });
        }

        return items;
    }

    private List<ServizioOperatoreSubEsterno> BuildOperatoriSubEsterni()
    {
        var items = new List<ServizioOperatoreSubEsterno>();
        var perIds = new HashSet<int>();

        foreach (var row in ServizioOperatoriSubEsterniBozza)
        {
            if (!IsOperatoreSubEsternoCompilato(row))
            {
                continue;
            }

            var perId = ParseNullableInt(row.PerId, "PerID operatore sub esterno");
            if (perId is null || perId <= 0)
            {
                throw new InvalidOperationException("Per ogni operatore sub esterno il PerID ministeriale e obbligatorio.");
            }

            if (!perIds.Add(perId.Value))
            {
                throw new InvalidOperationException($"PerID esterno duplicato nel servizio: {perId.Value}.");
            }

            if (ServizioPartecipantiBozza.Any(item => item.PerId == perId.Value && item.Presente))
            {
                throw new InvalidOperationException($"PerID {perId.Value}: l'operatore e gia presente tra il personale interno.");
            }

            if (string.IsNullOrWhiteSpace(row.Nominativo))
            {
                throw new InvalidOperationException("Per ogni operatore sub esterno il nominativo e obbligatorio.");
            }

            if (string.IsNullOrWhiteSpace(row.Reparto))
            {
                throw new InvalidOperationException($"{row.Nominativo}: indicare il reparto di appartenenza.");
            }

            if (row.GruppoOperativo is null)
            {
                throw new InvalidOperationException($"{row.Nominativo}: selezionare il gruppo operativo.");
            }

            items.Add(new ServizioOperatoreSubEsterno
            {
                PerId = perId.Value,
                Qualifica = row.Qualifica.Trim(),
                Nominativo = row.Nominativo.Trim(),
                Reparto = row.Reparto.Trim(),
                GruppoOperativoId = row.GruppoOperativo.GruppoOperativoId,
                Note = row.Note.Trim(),
            });
        }

        return items;
    }

    private List<ServizioImmersione> BuildServizioImmersioni(
        IReadOnlyCollection<ServizioPartecipante> partecipanti,
        IReadOnlyCollection<ServizioOperatoreSubEsterno> operatoriSubEsterni)
    {
        var items = new List<ServizioImmersione>();
        var presentiPerId = partecipanti
            .Where(item => item.Presente)
            .Select(item => item.PerId)
            .ToHashSet();
        var esterniPerId = operatoriSubEsterni
            .Select(item => item.PerId)
            .ToHashSet();

        foreach (var row in ServizioImmersioniBozza)
        {
            var partecipazioniImmersione = BuildServizioPartecipazioniImmersione(row, presentiPerId, isEsterno: false);
            var partecipazioniEsterneImmersione = BuildServizioPartecipazioniImmersioneEsterne(row, esterniPerId);
            var includeRow = row.DirettoreImmersione is not null
                || row.OperatoreSoccorso is not null
                || row.AssistenteBlsd is not null
                || row.AssistenteSanitario is not null
                || !string.IsNullOrWhiteSpace(row.Note)
                || partecipazioniImmersione.Count > 0
                || partecipazioniEsterneImmersione.Count > 0;

            if (!includeRow)
            {
                continue;
            }

            ValidaOperatoreImmersione("direttore immersione", row.DirettoreImmersione, presentiPerId, row.NumeroImmersione);
            ValidaOperatoreImmersione("operatore soccorso", row.OperatoreSoccorso, presentiPerId, row.NumeroImmersione);
            ValidaOperatoreImmersione("assistenza BLSD", row.AssistenteBlsd, presentiPerId, row.NumeroImmersione);
            ValidaOperatoreImmersione("assistenza sanitaria", row.AssistenteSanitario, presentiPerId, row.NumeroImmersione);

            items.Add(new ServizioImmersione
            {
                NumeroImmersione = row.NumeroImmersione,
                OrarioInizio = null,
                OrarioFine = null,
                DirettoreImmersionePerId = GetPerIdOperatoreSelezionato(row.DirettoreImmersione),
                OperatoreSoccorsoPerId = GetPerIdOperatoreSelezionato(row.OperatoreSoccorso),
                AssistenteBlsdPerId = GetPerIdOperatoreSelezionato(row.AssistenteBlsd),
                AssistenteSanitarioPerId = GetPerIdOperatoreSelezionato(row.AssistenteSanitario),
                LocalitaOperativaId = ServizioLocalitaSelezionata?.LocalitaOperativaId,
                ScopoImmersioneId = ServizioScopoSelezionato?.ScopoImmersioneId,
                Note = row.Note.Trim(),
                Partecipazioni = partecipazioniImmersione,
                PartecipazioniEsterne = partecipazioniEsterneImmersione,
            });
        }

        return items;
    }

    private List<ServizioPartecipanteImmersione> BuildServizioPartecipazioniImmersione(
        ServizioImmersioneDraftViewModel immersione,
        IReadOnlySet<int> presentiPerId,
        bool isEsterno)
    {
        var items = new List<ServizioPartecipanteImmersione>();

        foreach (var row in immersione.Partecipazioni.Where(item => item.IsEsterno == isEsterno))
        {
            if (IsDirettoreImmersione(immersione, row.PerId))
            {
                continue;
            }

            var includeRow = IsPartecipazioneImmersioneCompilata(row);
            if (!includeRow)
            {
                continue;
            }

            if (!presentiPerId.Contains(row.PerId))
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: {row.Nominativo} non risulta presente nel servizio.");
            }

            if (row.TipologiaImmersioneOperativa is null)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: selezionare l'apparato per {row.Nominativo}.");
            }

            var profondita = ParseNullableInt(row.ProfonditaMetri, $"Immersione {immersione.NumeroImmersione} - profondita {row.Nominativo}");
            ValidaProfonditaPerTipologia(
                row.TipologiaImmersioneOperativa,
                profondita,
                $"Immersione {immersione.NumeroImmersione} - profondita {row.Nominativo}");
            var fascia = row.FasciaProfondita;
            if (fascia is null && profondita is not null)
            {
                fascia = FasceProfonditaCatalogo.FirstOrDefault(item => profondita.Value >= item.MetriDa && profondita.Value <= item.MetriA);
            }

            if (fascia is null)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: selezionare la fascia profondita per {row.Nominativo}.");
            }

            var ore = ParseNullableDecimal(row.OreImmersione, $"Immersione {immersione.NumeroImmersione} - ore {row.Nominativo}");
            if (ore is null || ore <= 0)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: indicare ore immersione valide per {row.Nominativo}.");
            }

            if (row.CategoriaContabileOre is null)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: selezionare la categoria contabile per {row.Nominativo}.");
            }

            items.Add(new ServizioPartecipanteImmersione
            {
                ServizioPartecipanteId = row.PerId,
                TipologiaImmersioneOperativaId = row.TipologiaImmersioneOperativa.TipologiaImmersioneOperativaId,
                ProfonditaMetri = profondita,
                FasciaProfonditaId = fascia.FasciaProfonditaId,
                OreImmersione = ore,
                CategoriaContabileOreId = row.CategoriaContabileOre.CategoriaContabileOreId,
                Note = row.Note.Trim(),
            });
        }

        return items;
    }

    private List<ServizioOperatoreSubEsternoImmersione> BuildServizioPartecipazioniImmersioneEsterne(
        ServizioImmersioneDraftViewModel immersione,
        IReadOnlySet<int> esterniPerId)
    {
        var items = new List<ServizioOperatoreSubEsternoImmersione>();

        foreach (var row in immersione.Partecipazioni.Where(item => item.IsEsterno))
        {
            var includeRow = IsPartecipazioneImmersioneCompilata(row);
            if (!includeRow)
            {
                continue;
            }

            if (!esterniPerId.Contains(row.PerId))
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: {row.Nominativo} non risulta tra gli operatori sub esterni del servizio.");
            }

            if (row.TipologiaImmersioneOperativa is null)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: selezionare l'apparato per {row.Nominativo}.");
            }

            var profondita = ParseNullableInt(row.ProfonditaMetri, $"Immersione {immersione.NumeroImmersione} - profondita {row.Nominativo}");
            ValidaProfonditaPerTipologia(
                row.TipologiaImmersioneOperativa,
                profondita,
                $"Immersione {immersione.NumeroImmersione} - profondita {row.Nominativo}");
            var fascia = row.FasciaProfondita;
            if (fascia is null && profondita is not null)
            {
                fascia = FasceProfonditaCatalogo.FirstOrDefault(item => profondita.Value >= item.MetriDa && profondita.Value <= item.MetriA);
            }

            if (fascia is null)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: selezionare la fascia profondita per {row.Nominativo}.");
            }

            var ore = ParseNullableDecimal(row.OreImmersione, $"Immersione {immersione.NumeroImmersione} - ore {row.Nominativo}");
            if (ore is null || ore <= 0)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: indicare ore immersione valide per {row.Nominativo}.");
            }

            if (row.CategoriaContabileOre is null)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: selezionare la categoria contabile per {row.Nominativo}.");
            }

            items.Add(new ServizioOperatoreSubEsternoImmersione
            {
                ServizioOperatoreSubEsternoId = row.PerId,
                TipologiaImmersioneOperativaId = row.TipologiaImmersioneOperativa.TipologiaImmersioneOperativaId,
                ProfonditaMetri = profondita,
                FasciaProfonditaId = fascia.FasciaProfonditaId,
                OreImmersione = ore,
                CategoriaContabileOreId = row.CategoriaContabileOre.CategoriaContabileOreId,
                Note = row.Note.Trim(),
            });
        }

        return items;
    }

    private Personale BuildModelFromEditor()
    {
        if (string.IsNullOrWhiteSpace(Cognome))
        {
            throw new InvalidOperationException("Il cognome e obbligatorio.");
        }

        if (string.IsNullOrWhiteSpace(Nome))
        {
            throw new InvalidOperationException("Il nome e obbligatorio.");
        }

        if (string.IsNullOrWhiteSpace(CodiceFiscale))
        {
            throw new InvalidOperationException("Il codice fiscale e obbligatorio.");
        }

        if (!ProfiliPersonaleDisponibili.Contains(ProfiloPersonale, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Seleziona un profilo personale valido.");
        }

        if (IsProfiloSanitario && string.IsNullOrWhiteSpace(RuoloSanitario))
        {
            throw new InvalidOperationException("Per il profilo sanitario seleziona il ruolo sanitario.");
        }

        var statoServizio = StatoServizioPersonaleCatalogo.Normalizza(StatoServizioPersonale);
        DateOnly? dataFineServizio = string.Equals(statoServizio, StatoServizioPersonaleCatalogo.Attivo, StringComparison.OrdinalIgnoreCase)
            ? null
            : ParseDate(DataFineServizio, "Data fine servizio")
                ?? throw new InvalidOperationException("Per personale trasferito o cessato indica la data di fine servizio.");

        return new Personale
        {
            PerId = ParseRequiredPerId(),
            Cognome = Cognome.Trim(),
            Nome = Nome.Trim(),
            Qualifica = Qualifica.Trim(),
            DataDecorrenzaQualifica = ParseDate(DataDecorrenzaQualifica, "Data decorrenza qualifica"),
            ProfiloPersonale = ProfiliPersonaleCatalogo.Normalizza(ProfiloPersonale),
            RuoloSanitario = IsProfiloSanitario ? RuoloSanitario.Trim() : string.Empty,
            CodiceFiscale = CodiceFiscale.Trim().ToUpperInvariant(),
            MatricolaPersonale = MatricolaPersonale.Trim(),
            NumeroBrevettoSmz = NumeroBrevettoSmz.Trim(),
            StatoServizio = statoServizio,
            DataFineServizio = dataFineServizio,
            DataNascita = ParseDate(DataNascita, "Data di nascita"),
            LuogoNascita = LuogoNascita.Trim(),
            ViaResidenza = ViaResidenza.Trim(),
            CapResidenza = CapResidenza.Trim(),
            CittaResidenza = CittaResidenza.Trim(),
            Telefono1 = Telefono1.Trim(),
            Telefono2 = Telefono2.Trim(),
            Mail1Utente = NormalizeMailUtente(Mail1Utente, "Mail Polizia"),
            Mail2Utente = Mail2Utente.Trim(),
            Abilitazioni = BuildAbilitazioni(),
            VisiteMediche = BuildVisite(),
            Attagliamento = BuildAttagliamento(),
        };
    }

    private List<PersonaleAbilitazione> BuildAbilitazioni()
    {
        var items = new List<PersonaleAbilitazione>();

        foreach (var row in Abilitazioni)
        {
            if (row.TipoAbilitazioneId is null)
            {
                throw new InvalidOperationException("Ogni riga abilitazione deve avere un tipo selezionato.");
            }

            var tipo = TipiAbilitazioneCatalogo.Single(item => item.TipoAbilitazioneId == row.TipoAbilitazioneId.Value);

            items.Add(new PersonaleAbilitazione
            {
                PersonaleAbilitazioneId = row.PersonaleAbilitazioneId ?? 0,
                PerId = PerId,
                TipoAbilitazioneId = tipo.TipoAbilitazioneId,
                Tipo = tipo,
                Livello = row.Livello.Trim(),
                ProfonditaMetri = ParseNullableInt(row.ProfonditaMetri, $"Profondita abilitazione {tipo.Descrizione}"),
                DataConseguimento = ParseDate(row.DataConseguimento, $"Data conseguimento {tipo.Descrizione}"),
                DataScadenza = ParseDate(row.DataScadenza, $"Data scadenza {tipo.Descrizione}"),
                Note = row.Note.Trim(),
            });
        }

        return items;
    }

    private List<VisitaMedica> BuildVisite()
    {
        if (IsProfiloSanitario)
        {
            return VisiteMediche
                .Where(row => !string.IsNullOrWhiteSpace(row.DataUltimaVisita))
                .Select(row =>
                {
                    var tipoVisita = row.TipoVisita.Trim();
                    var dataUltimaVisita = ParseDate(row.DataUltimaVisita, $"Data ultima visita {row.TipoVisita}");
                    var dataScadenzaManuale = ParseDate(row.DataScadenza, $"Data scadenza visita {row.TipoVisita}");
                    var dataScadenza = CalcolaScadenzaVisita(tipoVisita, dataUltimaVisita) ?? dataScadenzaManuale;

                    return new VisitaMedica
                    {
                        VisitaMedicaId = row.VisitaMedicaId ?? 0,
                        PerId = PerId,
                        TipoVisita = tipoVisita,
                        DataUltimaVisita = dataUltimaVisita,
                        DataScadenza = dataScadenza,
                        Esito = row.Esito.Trim(),
                        Note = row.Note.Trim(),
                    };
                })
                .ToList();
        }

        var items = new List<VisitaMedica>();
        var visitePerTipo = VisiteMediche.ToDictionary(
            row => row.TipoVisita.Trim(),
            row => row,
            StringComparer.OrdinalIgnoreCase);

        foreach (var tipo in TipiVisitaMedicaCatalogo)
        {
            if (!visitePerTipo.TryGetValue(tipo.Descrizione, out var row))
            {
                throw new InvalidOperationException($"{tipo.Descrizione}: visita obbligatoria non presente in scheda.");
            }

            if (string.IsNullOrWhiteSpace(row.DataUltimaVisita))
            {
                throw new InvalidOperationException($"{tipo.Descrizione}: la data ultima visita e obbligatoria.");
            }

            var tipoVisita = row.TipoVisita.Trim();
            var dataUltimaVisita = ParseDate(row.DataUltimaVisita, $"Data ultima visita {row.TipoVisita}");
            var dataScadenzaManuale = ParseDate(row.DataScadenza, $"Data scadenza visita {row.TipoVisita}");
            var dataScadenza = CalcolaScadenzaVisita(tipoVisita, dataUltimaVisita) ?? dataScadenzaManuale;

            if (CatalogoVisiteMediche.TrovaPerDescrizione(tipoVisita) is not null && dataUltimaVisita is null)
            {
                throw new InvalidOperationException($"{tipoVisita}: la data ultima visita e obbligatoria per calcolare la scadenza.");
            }

            items.Add(new VisitaMedica
            {
                VisitaMedicaId = row.VisitaMedicaId ?? 0,
                PerId = PerId,
                TipoVisita = tipoVisita,
                DataUltimaVisita = dataUltimaVisita,
                DataScadenza = dataScadenza,
                Esito = row.Esito.Trim(),
                Note = row.Note.Trim(),
            });
        }

        return items;
    }

    private List<PersonaleAttagliamento> BuildAttagliamento()
    {
        return Attagliamento
            .Where(row => !string.IsNullOrWhiteSpace(row.Voce))
            .Select(row => new PersonaleAttagliamento
            {
                PersonaleAttagliamentoId = row.PersonaleAttagliamentoId ?? 0,
                PerId = PerId,
                Voce = row.Voce.Trim(),
                TagliaMisura = row.TagliaMisura.Trim(),
                Note = row.Note.Trim(),
            })
            .ToList();
    }

    private static DateOnly? CalcolaScadenzaVisita(string tipoVisita, DateOnly? dataUltimaVisita)
    {
        if (dataUltimaVisita is null)
        {
            return null;
        }

        var tipo = CatalogoVisiteMediche.TrovaPerDescrizione(tipoVisita);
        if (tipo?.MesiValidita is null)
        {
            return null;
        }

        return dataUltimaVisita.Value.AddMonths(tipo.MesiValidita.Value);
    }

    private static void ValidaOperatoreImmersione(
        string ruolo,
        PersonaleListItemViewModel? operatore,
        IReadOnlySet<int> presentiPerId,
        int numeroImmersione)
    {
        if (operatore is null || operatore.PerId <= 0)
        {
            return;
        }

        if (!presentiPerId.Contains(operatore.PerId))
        {
            throw new InvalidOperationException(
                $"Immersione {numeroImmersione}: {operatore.Nominativo} e indicato come {ruolo}, ma non risulta presente nel servizio.");
        }
    }

    private static int? ParseNullableInt(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value, out var parsed))
        {
            throw new InvalidOperationException($"{fieldName}: valore numerico non valido.");
        }

        return parsed;
    }

    private static decimal? ParseNullableDecimal(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{fieldName}: valore numerico non valido.");
    }

    private static string NormalizeMailUtente(string value, string fieldName)
    {
        return MailPoliziaHelper.NormalizeUserPart(value, fieldName);
    }

    private static string FormatDecimal(decimal? value) =>
        value?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;

    private static DateOnly? ParseDate(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{fieldName}: usare una data valida, ad esempio 18/03/2026.");
    }

    private static string FormatDate(DateOnly? value) => value?.ToString("dd/MM/yyyy") ?? string.Empty;

    private static DateOnly GetOrdineDecorrenzaQualifica(DateOnly? value) => value ?? DateOnly.MaxValue;

    private static decimal? ParseDecimalSilenzioso(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        return null;
    }

    private static decimal CalcolaImportoRiepilogoImmersione(decimal tariffa, decimal ore) =>
        Math.Round(tariffa * ore, 2, MidpointRounding.AwayFromZero);
}
