# Tabella personale nella scheda servizio — 29 ottobre 2026

La selezione del personale e stata riorganizzata per mostrare due persone per riga e accorciare l'elenco, mantenendo soltanto i cinque campi richiesti: qualifica, nome, presente, ruolo e gruppo.

## Correzione

- Due persone affiancate per ogni riga, con intestazioni ripetute sulle due meta dell'elenco.
- Fascia verticale centrale larga 14 pixel, con bordi azzurri, per separare visivamente le due colonne dall'intestazione fino all'ultima riga. La fascia non intercetta i clic.
- Qualifica abbreviata secondo l'elenco fornito dall'utente; nome esteso disponibile al passaggio del mouse.
- Presenza modificabile con una casella, ruolo e gruppo selezionabili direttamente dai menu.
- Contatti e note rimossi dalla presentazione del selettore; gli eventuali dati gia registrati non sono cancellati.
- Le selezioni agiscono direttamente sugli stessi oggetti di bozza del servizio, senza duplicare le persone per comporre le coppie.
- Ordinamento iniziale per gerarchia, decorrenza e nominativo mantenuto. Rimossa la precedente logica di ordinamento delle intestazioni DataGrid, sostituite dalle due intestazioni del selettore.
- Qualifiche estese conservate nell'anagrafica; la nuova abbreviazione e disponibile separatamente dall'ordinamento gerarchico.
- Le altre tabelle della scheda servizio mantengono lo stile piu leggibile introdotto nella prima correzione (altezza minima 40 pixel e righe alternate).

File principali: `SMZ.Conta.App/Views/ServizioGiornalieroView.xaml`, relativo code-behind, `ServizioPartecipanteDraftViewModel.cs` e `Models/QualificaFormatter.cs`. Le sigle sono documentate in `ABBREVIAZIONI_QUALIFICHE.md`.

## Verifica

Anteprime del controllo WPF reale con cinque persone di esempio, a larghezze di 860 e 1240 pixel: tre righe di 42 pixel, con l'ultima persona sola. Nessuno scorrimento orizzontale alle due larghezze verificate. Controllati disposizione delle coppie, caselle di presenza, collegamento ai cataloghi e selezioni iniziali di ruolo/gruppo.

Compilazione Debug della soluzione completata con zero errori e tutti i 14 test superati, incluso il nuovo controllo delle sigle e della gerarchia. Usato output isolato in `artifacts/verifica-selettore/bin` poiche l'eseguibile Debug ordinario era aperto e bloccato dal programma in esecuzione. Il controllo online NuGet delle vulnerabilita ha segnalato `NU1900` per le restrizioni di rete dell'ambiente.

Anteprima finale con separazione verticale: `artifacts/scheda-servizio/2026-10-29/separatore/`. La cartella `due-persone` conserva la versione prima della fascia centrale; `prima` e `dopo` conservano le immagini della precedente correzione di spaziatura. La cartella `artifacts` e esclusa da Git. Le anteprime usano un database dedicato di esempio, separato dai dati operativi e dalla distro.

La distribuzione gia consegnata non e stata rigenerata.

## Aggiornamento dell'eseguibile ordinario

Dopo la segnalazione che l'elenco appariva ancora uguale, verificato che `SMZ.Conta.App/bin/Debug/net8.0-windows/SMZ.App.dll` era ancora precedente alla copia verificata nell'output isolato. Con il programma chiuso, ricompilata la soluzione nell'output Debug ordinario e rieseguiti tutti i 14 test con esito positivo.

Verificata la coincidenza tra la DLL ordinaria e quella della copia di verifica. L'avvio dalla cartella del progetto tramite `Avvia-SMZ.cmd` utilizza la versione aggiornata; anche la successiva separazione verticale e stata compilata nell'output ordinario.
