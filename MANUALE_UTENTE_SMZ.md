# Manuale utente SMZ

Questo manuale descrive l'uso operativo dell'applicazione SMZ per la gestione di personale, servizi giornalieri, immersioni, scadenze, archivio, backup, contabilita e report.

## 1. Avvio dell'applicazione

Avviare l'app con uno dei file presenti nella cartella principale:

- `Avvia-SMZ.cmd`
- `Avvia-SMZ.ps1`

All'avvio compare la welcome page. Premendo **Entra nel gestionale** viene aperta la pagina di accesso sopra la welcome, che resta visibile e temporaneamente bloccata fino alla chiusura del login.

Al primo accesso, se non esistono account, viene richiesto di creare il primo amministratore indicando il proprio **PerID** e una password di almeno 10 caratteri. La password non viene salvata in chiaro.

Agli avvii successivi viene mostrata la pagina di accesso. Inserire:

- il proprio **PerID**;
- la propria password.

Una password temporanea assegnata da un amministratore deve essere cambiata prima di entrare nel gestionale.

La welcome page contiene:

- pulsante **Entra nel gestionale** per aprire il login;
- pulsante audio con icona per attivare o disattivare il suono della welcome;
- pulsante **ESCI** per chiudere direttamente dalla welcome.

Dopo un accesso riuscito si entra direttamente nell'area autorizzata, senza mostrare nuovamente la welcome. Il comando **Disconnetti** torna alla welcome page.

Se si chiude l'app dalla welcome non viene mostrato l'avviso sulle modifiche non salvate. Dopo l'ingresso nel gestionale, invece, la chiusura dell'app controlla eventuali modifiche non salvate.

## 2. Navigazione generale

La barra laterale permette di accedere alle sezioni principali:

- **Dashboard operativa**
- **Servizio giornaliero**
- **Personale**
- **Backup e archivio**
- **Contabilita**
- **Stampe e report**
- **Amministrazione**

La barra superiore mostra data, ora e pulsante **ESCI**. La barra inferiore mostra lo stato operativo e le informazioni sui backup.

I controlli **A−**, **percentuale** e **A+** regolano la dimensione dell'interfaccia dal 70% al 150%. La preferenza viene ricordata sul PC e nel profilo dati in uso. Il pulsante con la percentuale ripristina il 100%; sono disponibili anche **Ctrl e −**, **Ctrl e +**, **Ctrl e 0**.

Quando lo spazio diminuisce, schede e campi si dispongono su piu righe e il menu laterale puo mostrare solo le icone: passando il mouse si legge il nome della sezione. In questa modalita **PW** apre il cambio password e la freccia permette di disconnettersi. Pagine e finestre di dialogo scorrono verticalmente; le tabelle mantengono colonne leggibili con scorrimento orizzontale.

La barra laterale mostra anche il nominativo, il PerID e il profilo dell'utente corrente. I comandi **Cambia password** e **Disconnetti** permettono rispettivamente di aggiornare la propria password o tornare alla pagina di accesso.

Il profilo **Base** entra direttamente nella sezione **Personale** e puo consultare, creare e modificare le anagrafiche. Non puo cessare o eliminare personale e non vede le altre aree operative.

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

Ogni tipo di abilitazione puo essere inserito una sola volta per persona. Per cambiare livello, date o note, selezionare la riga esistente e usare **Aggiorna abilitazione**. Un nuovo inserimento dello stesso tipo, oppure il cambio di una riga verso un tipo gia presente, viene rifiutato. Eventuali doppioni gia registrati restano consultabili: prima di salvare quella scheda occorre mantenere una sola riga per tipo, verificando quali dati conservare.

Le visite mediche operative sono il mantenimento brevetto M.M. (24 mesi) e la visita D.Lgs. 81/08 (12 mesi). Sono facoltative: visite mancanti o scadute non bloccano salvataggio, servizi o contabilita. La visita bimestrale e disattivata e non compare nei menu, nella scheda o nello scadenzario; eventuali dati gia registrati restano conservati nel database.

Usare **Salva** per registrare le modifiche. L'app segnala eventuali modifiche non salvate in chiusura.

### Cessazione ed eliminazione

Il comando **Cessa da oggi** archivia operativamente il personale. L'eliminazione definitiva va usata con cautela perche rimuove stabilmente la scheda.

## 5. Servizio giornaliero

La sezione **Servizio giornaliero** consente di creare, modificare, duplicare, stampare ed eliminare servizi.

Gli elenchi del personale sono ordinati per qualifica, dalla piu alta alla piu bassa. Nell'anagrafica e nei selettori del servizio, a parita di qualifica si considera prima la decorrenza piu anziana, poi cognome e nome.

La selezione **Personale impiegato** mostra due colonne, ciascuna con **Qualifica**, **Nome**, **Presente**, **Ruolo** e **Gruppo**. L'ordine si legge dall'alto in basso nella prima colonna, poi nella seconda. Spuntare i presenti e, quando necessario, cambiare ruolo o gruppo dai menu. La qualifica usa la sigla breve: passando il mouse sulla sigla si legge il nome esteso. Contatti e note non sono mostrati in questo selettore. Le sigle sono elencate in `ABBREVIAZIONI_QUALIFICHE.md`.

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
- due campi natante nello stesso ordine di servizio (secondo facoltativo), affiancati alla localita;
- scopo immersione;
- responsabile servizio;
- indennita;
- attivita svolta e note.

La scheda usa righe compatte: **Orario in deroga** e affiancato a **Orario servizio**; **Lavoro straordinario**, **Indennita fuori sede** e **Indennita OP** sono sulla stessa riga. Gli orari aggiuntivi compaiono quando si attiva la relativa opzione. Nelle finestre molto strette le righe di questi campi scorrono orizzontalmente.

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

Nel dettaglio contabile SMZ in fondo pagina, le colonne **Immersione** e **Ruolo** riportano automaticamente gli incarichi scelti nei menu superiori. Il numero compare nella prima colonna; nella seconda compare soltanto la descrizione completa: **Direttore immersione**, **Operatore soccorso**, **Assistenza BLSD** o **Assistenza sanitaria**. Se una persona ha piu incarichi, numeri e descrizioni sono riportati su righe corrispondenti. I sanitari incaricati sono mostrati per consultazione.

Per gli operatori senza incarico resta disponibile la scelta del numero dell'immersione. Si compilano poi apparato, profondita, fascia, ore e categoria. Se una persona svolge un incarico in un'immersione e si immerge in un'altra, dopo la compilazione del dettaglio contabile compare anche la scelta dell'immersione come **Operatore subacqueo**, separata dall'incarico. Le immersioni in cui svolge direzione o assistenza sono escluse da questa scelta. Numeri e ruoli vengono riproposti alla riapertura del servizio.

### Salvataggio e stampa

Usare **Salva** in alto oppure **Salva servizio** in fondo pagina per registrare il servizio. Usare **Stampa servizio** per produrre il documento operativo. Tutti i natanti scelti vengono salvati e riportati nelle stampe.

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

Se il mese e gia chiuso, le tabelle mostrano gli importi salvati. Dopo una correzione ai calcoli o alle tariffe, usare **Rigenera chiusura** per ricalcolare quel mese dai servizi e registrare i nuovi importi. Il programma chiede conferma prima di sostituire la chiusura precedente.

### Contabilita SMZ immersioni

La tabella mostra righe contabili separate per data e ordine di servizio.

L'importo si calcola moltiplicando la tariffa oraria finale della categoria per le ore contabili inserite. Le tariffe ADD comprendono gia la riduzione del 50%, le tariffe C.I. quella del 20% e le tariffe SPER la maggiorazione del 25%: riepilogo del servizio e contabilita mensile non applicano nuovamente queste percentuali. Le ore restano quelle registrate nel servizio.

Esempi per un'ora nella fascia 00/12: A.R.A. addestrativa 2,50 euro, Camera Iperbarica 4,00 euro, A.R.A. sperimentale 6,25 euro, con le tariffe iniziali del programma.

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

Il foglio di servizio usa il formato A4 verticale, con riepilogo di straordinario, presenze e immersioni e una tabella separata per il fuori sede. Ogni pagina riporta il riferimento all'ordine di servizio e la numerazione. Riepilogo e firma restano nello stesso blocco di stampa.

### Registro immersioni

Permette di consultare e stampare il registro immersioni mensile.

### Report personale

Permette di generare riepiloghi mensili o annuali sul personale.

Quando disponibili, usare i filtri per anno, mese, nominativo e categoria.

## 9. Amministrazione

La sezione **Amministrazione** contiene la gestione utenti, i cataloghi operativi e le tariffe. La gestione degli accessi e mostrata per prima.

### Gestione utenti

La gestione utenti e disponibile esclusivamente agli amministratori. Ogni account e identificato dal PerID di una persona presente in anagrafica.

- **Nuovo account** crea un profilo Base o Amministratore con password temporanea.
- **Reset password** assegna una password temporanea e obbliga l'utente a cambiarla al prossimo accesso.
- **Sospendi/Riattiva** impedisce o ripristina l'accesso senza cancellare lo storico dell'account.
- **Imposta Base/Rendi amministratore** modifica il livello di operativita.

Il programma impedisce di sospendere o declassare l'account in uso e di rimuovere l'ultimo amministratore attivo. Quando una persona viene cessata, viene proposto di sospendere anche il relativo account.

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

La tariffa da inserire e l'importo orario finale della categoria, comprensivo della relativa riduzione o maggiorazione. Le tariffe personalizzate vengono conservate all'avvio. I vecchi cataloghi che coincidono interamente con i valori iniziali del programma vengono aggiornati automaticamente al nuovo criterio.

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
