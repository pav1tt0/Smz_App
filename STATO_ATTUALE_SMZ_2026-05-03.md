# Stato Attuale SMZ al 03/05/2026

Documento unico di riallineamento del progetto SMZ, aggiornato al codice presente nel repository il 03/05/2026.

Questo file sostituisce, come riferimento operativo corrente, le checklist precedenti che fotografavano stati parziali o ormai superati.

## Verifica Tecnica

Comando eseguito:

```powershell
dotnet build SMZ.Conta.sln
```

Esito:

- build completata correttamente
- 0 errori
- 0 avvisi

Nota Git: `git status --short` non mostra modifiche tracciate, ma Git segnala warning di permessi su `C:\Users\LENOVO\.config\git\ignore`. Il warning riguarda la configurazione globale utente, non il codice del progetto.

## Struttura Del Progetto

Il progetto e una applicazione desktop Windows in C# / WPF:

- soluzione: `SMZ.Conta.sln`
- progetto principale: `SMZ.Conta.App/SMZ.Conta.App.csproj`
- framework: `.NET 8`, target `net8.0-windows`
- database: SQLite tramite `Microsoft.Data.Sqlite`
- UI: WPF con pattern MVVM

Cartelle principali:

- `SMZ.Conta.App/Models`: modelli dominio
- `SMZ.Conta.App/ViewModels`: logica applicativa e stato UI
- `SMZ.Conta.App/Data`: database, repository, backup, import/export
- `SMZ.Conta.App/Infrastructure`: comandi, behavior input, helper
- `SMZ.Conta.App/Controls`: controlli WPF custom
- `SMZ.Conta.App/Assets`: immagini e audio
- `scripts`: script di pubblicazione
- `file per programma`: documenti Word/Excel di riferimento operativo

## Avvio E Distribuzione

Sono presenti script di avvio:

- `Avvia-SMZ.ps1`
- `Avvia-SMZ.cmd`

Lo script PowerShell avvia il progetto con:

```powershell
dotnet run --project SMZ.Conta.App\SMZ.Conta.App.csproj -c Debug
```

E presente anche lo script:

- `scripts/Publish-Test-Operatore.ps1`

che pubblica una build self-contained Windows, single file, in `dist`.

## Percorsi Dati Applicativi

I dati applicativi non sono salvati dentro la cartella del repository, ma in `%LocalAppData%`:

- cartella applicativa: `%LocalAppData%\SMZ\Conta`
- database: `%LocalAppData%\SMZ\Conta\smz-conta.db`
- export: `%LocalAppData%\SMZ\Conta\Export`
- backup locali: `%LocalAppData%\SMZ\Conta\Backups\Local`
- impostazioni backup: `%LocalAppData%\SMZ\Conta\backup-settings.json`

Questa scelta rende il programma adatto a uso locale su PC dedicato.

## Stato Funzionale Attuale

Il programma non e piu un prototipo. Allo stato attuale e un gestionale locale avanzato e compilabile, con molte funzioni operative gia presenti.

### Anagrafica Personale

Presente e ampia.

Gestisce:

- PerID
- cognome e nome
- qualifica
- profilo personale
- ruolo sanitario
- codice fiscale
- matricola
- numero brevetto SMZ
- dati di nascita
- residenza
- contatti
- stato di servizio
- abilitazioni
- visite mediche
- attagliamento

Funzioni presenti:

- ricerca
- suggerimenti
- filtri
- apertura scheda
- modifica
- salvataggio
- cessazione / archiviazione
- ripristino da archivio
- eliminazione definitiva da archivio

### Abilitazioni E Visite Mediche

Presenti e integrate con la scheda personale e lo scadenzario.

Le abilitazioni sono basate su catalogo e gestiscono, dove previsto:

- livello
- profondita
- data conseguimento
- data scadenza
- note

Le visite mediche gestiscono:

- tipo visita
- ultima visita
- scadenza
- esito
- note

### Scadenzario

Presente e integrato.

Mostra scadenze prossime ricavate da:

- visite mediche
- abilitazioni

E collegato alla dashboard e alla ricerca.

### Servizi Giornalieri

Presente e persistito su database.

Gestisce:

- data servizio
- numero ordine servizio
- orario servizio
- straordinario
- tipo servizio
- localita
- scopo immersione
- unita navale
- fuori sede
- indennita ordine pubblico
- attivita svolta
- note

Vincolo presente:

- `Fuori sede` e `Indennita ordine pubblico` sono mutuamente esclusivi.

Funzioni presenti:

- nuovo servizio
- salvataggio
- apertura servizio salvato
- apertura rapida da elenco
- eliminazione
- elenco servizi recenti

### Partecipanti E Supporti Occasionali

Per ogni servizio sono presenti:

- partecipanti interni SMZ
- stato presenza
- gruppo operativo
- ruolo operativo
- note
- supporti occasionali / assistenza SMZ non inseriti in anagrafica stabile

### Immersioni

Il modello dati e il database supportano immersioni multiple per servizio.

Per ogni immersione sono presenti nel modello:

- numero immersione
- orario inizio
- orario fine
- direttore immersione
- operatore soccorso
- assistenza BLSD
- assistenza sanitaria
- localita specifica immersione
- scopo specifico immersione
- note

Per ogni partecipante in immersione sono presenti:

- flag ingresso in immersione
- apparato / tipologia immersione
- profondita
- fascia profondita
- ore immersione
- categoria contabile ore
- tariffa proposta
- importo stimato
- note

Comportamenti presenti:

- propagazione iniziale di apparato/categoria/profondita/ore
- calcolo fascia da profondita
- controllo profondita rispetto all'apparato
- calcolo importo come tariffa x ore

Limite attuale importante:

- il modello supporta localita e scopo per singola immersione, ma il draft usato dalla UI principale non espone ancora questi campi
- la UI immersioni non risulta ancora completamente dinamica per aggiunta/rimozione libera da interfaccia

### Cataloghi E Regole Contabili

Sono presenti cataloghi su database per:

- categorie registro
- localita operative
- scopi immersione
- unita navali
- tipologie immersione
- fasce profondita
- categorie contabili ore
- gruppi operativi
- ruoli operativi
- regole contabili immersione

Le tariffe contabili sono modificabili da interfaccia e persistite.

Limite:

- non tutti i cataloghi hanno ancora una vera UI amministrativa completa
- alcuni cataloghi restano inizializzati o ordinati da codice

### Contabilita Mensile

Presente e operativa.

Produce riepiloghi mensili per:

- SMZ immersioni
- sanitari
- supporti occasionali / assistenza SMZ

Sono presenti:

- ore ordinarie
- ore aggiuntive
- ore sperimentali
- ore camera iperbarica
- importi
- giornate utili per sanitari e supporti

### Elaborazione Mensile Persistita

Presente nel codice attuale.

Sono presenti tabelle e funzioni per:

- `ElaborazioniMensili`
- `ElaborazioneMensileRighe`
- salvataggio snapshot mensile
- ricarica snapshot salvato

Questo supera la vecchia checklist del 31/03/2026, dove la chiusura mensile risultava ancora mancante.

### Registro Immersioni Mensile

Presente.

Viene derivato dai servizi salvati e mostra:

- dettaglio servizio
- immersione
- operatore
- localita
- scopo
- apparato
- fascia
- ore
- categoria registro

Limite:

- manca ancora un output documentale finale formalizzato.

### Export Contabilita

Presente export CSV della contabilita mensile.

Limite:

- mancano ancora export finali strutturati in PDF, Excel o Word secondo formato definitivo.

### Import / Export Pacchetto Servizio

Presente base tecnica reale per scambio servizi tramite file `.smzsvc`.

Il pacchetto contiene:

- testata servizio
- partecipanti
- ruoli
- immersioni
- partecipazioni immersione
- supporti occasionali
- riferimenti cataloghi per codice o descrizione
- riferimenti personale tramite codice fiscale, matricola, brevetto, PerID e nominativo

Limiti attuali:

- manca deduplica robusta di pacchetto gia importato
- manca gestione aggiornamento di un servizio gia importato
- manca tracciamento persistente del legame tra servizio locale e pacchetto sorgente
- gli errori di import sono gestiti, ma non ancora come workflow guidato completo

### Backup E Ripristino

Presente modulo reale.

Funzioni presenti:

- backup locale manuale
- backup esterno manuale
- backup locale automatico
- configurazione cartella esterna
- ripristino da backup
- backup di sicurezza prima del restore
- retention backup locali/esterni

Questo supera la vecchia checklist del 31/03/2026, dove il backup applicativo risultava ancora non presente.

## Stato Dei Documenti Esistenti

Documenti principali trovati:

- `SPEC_PROGRAMMA_COMPLETO_SMZ.md`
- `SPEC_ABILITAZIONI.md`
- `CHECKLIST_FINALE_MD_VS_STATO_REALE.md`
- `STATO_PROGETTO_SMZ_2026-04-08.md`
- `ISTRUZIONI_TEST_OPERATORE_SMZ.md`

Nota importante:

- `CHECKLIST_FINALE_MD_VS_STATO_REALE.md` fotografa uno stato precedente e contiene punti poi superati
- `STATO_PROGETTO_SMZ_2026-04-08.md` e piu vicino allo stato reale attuale
- questo documento del 03/05/2026 va considerato il riferimento operativo piu aggiornato

## Debito Tecnico

Il progetto e funzionante, ma alcuni file sono molto grandi:

- `SMZ.Conta.App/ViewModels/MainWindowViewModel.cs`: circa 5073 righe
- `SMZ.Conta.App/MainWindow.xaml`: circa 3934 righe
- `SMZ.Conta.App/Data/PersonaleRepository.cs`: circa 3040 righe

Rischi:

- modifiche future piu lente
- maggiore rischio regressioni
- difficolta nel testare singoli comportamenti
- maggiore difficolta nel separare responsabilita UI, dominio e persistenza

Non e necessario rifattorizzare tutto subito, ma conviene spezzare progressivamente quando si interviene su aree specifiche.

## Test Automatizzati

Non risulta presente un progetto test strutturato.

Priorita consigliata per eventuali test:

1. contabilita mensile
2. salvataggio e ricarica servizio giornaliero
3. salvataggio elaborazione mensile
4. import/export `.smzsvc`
5. backup/ripristino
6. regole contabili immersione

## Gap Funzionali Ancora Aperti

I principali gap residui sono:

1. UI immersioni completamente dinamica
2. localita e scopo specifici per singola immersione esposti in UI
3. prospetti finali formalizzati per indennita assistenza SMZ
4. prospetti finali formalizzati per fuori sede
5. prospetti finali formalizzati per ordine pubblico
6. export/stampe finali PDF, Excel o Word
7. import `.smzsvc` con deduplica e aggiornamento
8. editor amministrativo completo dei cataloghi
9. test automatici

## Priorita Consigliate

Ordine di lavoro consigliato:

1. chiudere UI immersioni dinamiche
2. aggiungere localita e scopo per singola immersione nella UI
3. completare prospetti finali di indennita
4. implementare export/stampe finali
5. rafforzare import `.smzsvc` con deduplica, aggiornamento e tracciamento origine
6. aggiungere test automatici sui flussi critici
7. rifattorizzare gradualmente ViewModel, XAML e repository

## Valutazione Finale

Alla data del 03/05/2026 il progetto SMZ e in stato avanzato e compilabile.

Il nucleo operativo e presente:

- anagrafica
- scadenze
- servizi
- immersioni
- contabilita
- registro
- chiusura mensile
- backup
- scambio pacchetti servizio

Le attivita residue non riguardano piu la base del gestionale, ma soprattutto:

- completamento UI su funzioni gia supportate dal modello
- output documentali ufficiali
- robustezza import/export
- test e manutenzione del codice

