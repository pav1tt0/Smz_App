# Manuale utente SMZ

Questo manuale descrive l'uso operativo dell'applicazione SMZ per la gestione di personale, servizi giornalieri, immersioni, scadenze, archivio, backup, contabilita e report.

## 1. Avvio dell'applicazione

Avviare l'app con uno dei file presenti nella cartella principale:

- `Avvia-SMZ.cmd`
- `Avvia-SMZ.ps1`

All'avvio compare la welcome page con:

- pulsante **Entra nel gestionale** per accedere all'applicazione;
- pulsante audio con icona per attivare o disattivare il suono della welcome;
- pulsante **ESCI** per chiudere direttamente dalla welcome.

Se si chiude l'app dalla welcome non viene mostrato l'avviso sulle modifiche non salvate. Dopo l'ingresso nel gestionale, invece, la chiusura dell'app controlla eventuali modifiche non salvate.

## 2. Navigazione generale

La barra laterale permette di accedere alle sezioni principali:

- **Dashboard operativa**
- **Servizio giornaliero**
- **Personale**
- **Backup e archivio**
- **Contabilita**
- **Stampe e report**
- **Impostazioni operative**

La barra superiore mostra data, ora e pulsante **ESCI**. La barra inferiore mostra lo stato operativo e le informazioni sui backup.

## 3. Dashboard operativa

La dashboard raccoglie le informazioni principali:

- accesso rapido ai moduli;
- riepilogo scadenze visite mediche;
- elenco delle visite scadute o in scadenza;
- criticita operative non sanitarie;
- collegamenti rapidi agli elenchi interessati.

Il pulsante **Vai all'elenco** delle criticita operative e' abilitato solo quando sono presenti criticita.

## 4. Personale

La sezione **Personale** gestisce l'anagrafica degli operatori.

### Ricerca

Sono disponibili filtri per:

- cognome;
- abilitazione;
- visite entro una certa data.

Usare **Cerca** per applicare i filtri e **Pulisci** per azzerarli.

### Scheda personale

Con **Nuovo** si apre una nuova scheda. Con **Apri scheda** si modifica il nominativo selezionato.

La scheda contiene dati anagrafici, recapiti, stato di servizio, qualifiche, abilitazioni, visite mediche e attagliamento.

Usare **Salva** per registrare le modifiche. L'app segnala eventuali modifiche non salvate in chiusura.

### Cessazione ed eliminazione

Il comando **Cessa da oggi** archivia operativamente il personale. L'eliminazione definitiva va usata con cautela perche rimuove stabilmente la scheda.

## 5. Servizio giornaliero

La sezione **Servizio giornaliero** consente di creare, modificare, duplicare, stampare ed eliminare servizi.

### Elenco servizi

La parte iniziale mostra i servizi compilati. La ricerca consente di filtrare per:

- localita;
- numero servizio;
- data o mese.

Comandi principali:

- **Nuovo**: apre una nuova bozza servizio;
- **Duplica**: crea una nuova bozza partendo dal servizio selezionato;
- **Elimina**: rimuove il servizio selezionato;
- **Importa**: importa un pacchetto servizio;
- **Apri**: apre il servizio selezionato.

### Compilazione servizio

Nel dettaglio servizio compilare:

- data;
- numero ordine;
- orario servizio;
- eventuale deroga oraria;
- tipo servizio;
- localita;
- unita navale;
- scopo immersione;
- responsabile servizio;
- indennita;
- attivita svolta e note.

### Personale impiegato

La tabella **Personale impiegato** permette di indicare presenze, gruppo operativo, ruolo, contatti e note.

Sono presenti anche sezioni dedicate a:

- operatori sub di altri reparti;
- assistenza SMZ occasionale.

Queste righe sono legate al singolo servizio e non necessariamente all'anagrafica principale.

### Immersioni

La sezione **Immersioni del servizio** permette di aggiungere una o piu immersioni.

Per ogni immersione si compilano:

- orario inizio/fine;
- direttore immersione;
- operatore soccorso;
- assistenza BLSD;
- assistenza sanitaria;
- note immersione.

Il dettaglio contabile SMZ consente di indicare apparato, profondita, fascia, ore, categoria, tariffa e importo.

### Salvataggio e stampa

Usare **Salva** per registrare il servizio. Usare **Stampa servizio** per produrre il documento operativo.

## 6. Backup e archivio

La sezione **Backup e archivio** serve per proteggere e recuperare i dati.

### Backup

Comandi disponibili:

- **Crea backup locale**: salva una copia locale del database;
- **Crea backup esterno**: salva una copia nella cartella esterna configurata;
- **Configura cartella esterna**: imposta la destinazione dei backup esterni;
- **Ripristina da backup**: ripristina un file di backup.

Prima del ripristino viene creato un backup di sicurezza.

### Archivio personale

L'archivio contiene schede eliminate dalla parte operativa ma ancora recuperabili.

Comandi:

- **Ripristina**: riporta la scheda nell'elenco operativo;
- **Elimina**: elimina definitivamente la scheda archiviata.

## 7. Contabilita

La sezione **Contabilita** riepiloga i dati mensili per immersioni, sanitari e assistenza SMZ.

### Selezione periodo

Selezionare mese e anno, poi usare **Aggiorna**.

Il pulsante di salvataggio dell'elaborazione mensile conserva il riepilogo del periodo selezionato.

### Contabilita SMZ immersioni

La tabella mostra righe contabili separate per data e ordine di servizio.

Filtri disponibili:

- data;
- numero servizio;
- nominativo;
- apparato.

Usare **Pulisci filtri** per azzerare la ricerca.

### Sanitari impiegati

Mostra il riepilogo per personale sanitario, con giornate e trentesimi maturati.

### Assistenza SMZ

Mostra il riepilogo dell'assistenza SMZ aggregata per nominativo. Conviene usare sempre la stessa scrittura dei nominativi per ottenere conteggi coerenti.

## 8. Stampe e report

La sezione **Stampe e report** raccoglie le funzioni di stampa e consultazione.

### Servizi

Permette di cercare servizi salvati e stampare il servizio selezionato.

### Registro immersioni

Permette di consultare e stampare il registro immersioni mensile.

### Report personale

Permette di generare riepiloghi mensili o annuali sul personale.

Quando disponibili, usare i filtri per anno, mese, nominativo e categoria.

## 9. Impostazioni operative

La sezione **Impostazioni operative** contiene cataloghi e tariffe.

### Localita operative

Permette di aggiungere o modificare le localita usate nei servizi.

Campi principali:

- descrizione;
- provincia;
- ordine;
- attiva.

Usare **Salva localita** per registrare le modifiche.

### Mezzi nautici

Permette di aggiungere o modificare i mezzi disponibili nel servizio giornaliero.

Campi principali:

- descrizione;
- sigla;
- ordine;
- attiva.

Usare **Salva mezzi** per registrare le modifiche.

### Tariffe contabili

Permette di modificare le tariffe usate nei calcoli contabili.

Campi principali:

- apparato;
- fascia;
- categoria;
- tariffa;
- attiva.

Usare **Salva tariffe** per registrare le modifiche.

## 10. Date e campi calendario

I campi data accettano inserimento manuale e selezione tramite calendario.

Quando presente, il selettore data puo consentire anche la scelta del mese visualizzato, utile per le ricerche mensili.

## 11. Chiusura dell'app

Per chiudere l'app:

- usare **ESCI** nella welcome;
- usare **ESCI** nella barra superiore dopo l'accesso;
- oppure usare la **X** della finestra.

Dopo l'accesso al gestionale, se ci sono modifiche non salvate, l'app mostra un messaggio di conferma prima della chiusura.

## 12. Buone pratiche operative

- Salvare sempre dopo modifiche a personale, servizi, tariffe e cataloghi.
- Creare un backup esterno a fine giornata o prima di modifiche importanti.
- Prima di ripristinare un backup, verificare di avere scelto il file corretto.
- Usare descrizioni coerenti per assistenza SMZ occasionale, cosi i riepiloghi contabili restano corretti.
- Controllare la dashboard all'avvio per visite scadute, visite in scadenza e criticita operative.
- Verificare mese e anno prima di salvare elaborazioni contabili.

## 13. Risoluzione problemi rapida

### Non vedo l'ultima modifica grafica

Chiudere e riaprire l'applicazione. Se necessario, eseguire una nuova build.

### Non trovo un servizio vecchio

Usare la ricerca per data, mese, numero servizio o localita nella sezione **Servizio giornaliero** o in **Stampe e report**.

### Un conteggio contabile non torna

Controllare:

- presenza del personale nel servizio;
- ruoli immersione;
- dettaglio contabile SMZ;
- periodo selezionato in contabilita;
- eventuali filtri attivi.

### Devo recuperare personale archiviato

Andare in **Backup e archivio**, selezionare la scheda e usare **Ripristina**.

### Devo trasferire o proteggere i dati

Usare **Crea backup esterno** e verificare che la cartella esterna sia configurata correttamente.

