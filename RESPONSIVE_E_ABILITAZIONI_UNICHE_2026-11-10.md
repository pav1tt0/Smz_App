# Interfaccia adattabile e abilitazioni uniche - 10 novembre 2026

## Uso

La barra superiore contiene A−, percentuale e A+: dimensione dal 70% al 150%, con ripristino al 100% cliccando la percentuale. Funzionano anche Ctrl e meno, Ctrl e piu, Ctrl e zero, compreso il tastierino numerico. La preferenza viene salvata in `display-settings.json` nel profilo dati locale; un file mancante o non leggibile ripristina il 100%.

Menu, campi e schede si adattano allo spazio logico disponibile dopo l'ingrandimento di Windows e quello dell'app. Il menu si riduce a icone nei layout stretti; nomi delle sezioni e comandi account restano disponibili nei suggerimenti e tramite UI Automation. I campi passano a una o piu colonne e le barre dei comandi vanno a capo.

La finestra principale puo scendere a 640 x 360 unita logiche. La pagina scorre verticalmente, mentre le tabelle conservano le larghezze minime introdotte nella verifica precedente. Le tabelle lunghe hanno uno scorrimento verticale interno; la rotellina passa alla pagina quando raggiunge il bordo della tabella. Le finestre di accesso e gestione account si limitano all'area utile del monitor e consentono lo scorrimento del contenuto.

L'eseguibile dichiara PerMonitorV2 nel manifest. Riferimenti Microsoft: [consapevolezza DPI del processo](https://learn.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process) e [area utile del monitor](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-monitorinfo).

## Abilitazioni

Ogni tipo puo comparire una sola volta per persona. L'editor rifiuta nuovi doppioni e cambi di tipo in conflitto, senza sovrascrivere la riga originale. La riga esistente puo essere modificata, rimossa e successivamente reinserita. La stessa abilitazione resta assegnabile a persone diverse.

Il repository verifica l'unicita prima di aprire la transazione: un salvataggio con doppioni non crea schede parziali e non modifica dati gia salvati. I doppioni pregressi non vengono eliminati: chi usa la scheda deve verificare quali dati conservare e mantenere una sola riga per tipo prima del salvataggio.

## Verifiche

- 20 test automatici superati: includono ridisposizione e ripristino dei campi, conservazione dei binding, numero di schede per riga, persistenza e limiti dello zoom, abilitazioni uniche nell'editor e nel repository.
- 270 configurazioni della finestra principale: sei dimensioni logiche (1920 x 1000, 1366 x 720, 1093 x 570, 911 x 470, 768 x 520, 640 x 360), zoom 70%, 100% e 150%, tutte le sezioni e le schede interne di personale e report. Nessun comando o campo rilevato fuori dal contenuto raggiungibile.
- Tre verifiche della welcome page; quattro finestre di dialogo provate a 420 x 340, con controllo dello scorrimento fino ai comandi finali.
- Il contesto DPI nativo della finestra di verifica risulta PerMonitorV2. Le larghezze ridotte simulano lo spazio utile con ingrandimenti diversi; non sostituiscono una prova su ciascun monitor fisico degli altri PC.
- Evidenze locali: `artifacts/responsive-audit/layout-results.txt` e immagini nella stessa cartella.

Il pacchetto portatile aggiornato e `dist/SMZ-Responsive-2026-11-10-win-x64.zip`, compilato in Release con runtime Windows x64 incluso. Contiene l'applicazione e le istruzioni; i lanciatori distinguono dati ordinari e precedente profilo di prova Mario Rossi. Non include un database iniziale. Le versioni precedenti in `dist` restano distinte.

Gli avvisi NU1900 durante restore e compilazione indicano che la verifica online delle vulnerabilita NuGet non era disponibile nella rete di lavoro.
