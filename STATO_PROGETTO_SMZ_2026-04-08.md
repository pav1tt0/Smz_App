# Stato Progetto SMZ al 08/04/2026

Documento di stato operativo del gestionale SMZ, aggiornato al codice reale presente nel repository alla data del 08/04/2026.

Riferimenti principali:

- `SPEC_PROGRAMMA_COMPLETO_SMZ.md`
- `SPEC_ABILITAZIONI.md`
- `CHECKLIST_FINALE_MD_VS_STATO_REALE.md`
- codice applicativo in `SMZ.Conta.App`

## Obiettivo Attuale Del Programma

Il programma e oggi pensato per lavorare in locale su un PC dedicato, con database SQLite locale, backup applicativi e gestione operativa completa di:

- anagrafica personale
- servizi giornalieri
- immersioni e dettaglio tecnico-contabile
- riepiloghi mensili
- chiusura mensile persistita
- backup / ripristino

L'inserimento dei servizi fuori sede e previsto al rientro in sede, ma il codice contiene gia una base tecnica per esportare e importare singoli servizi tramite un pacchetto dedicato, utile per un futuro modulo separato "fuori sede".

## Stato Generale

Il progetto e in uno stato avanzato e gia utilizzabile come gestionale operativo locale.

Le aree oggi realmente presenti nel codice sono:

- anagrafica personale estesa
- abilitazioni, visite mediche e scadenzario
- archivio personale con ripristino
- foglio di servizio giornaliero persistito
- partecipanti di servizio e supporti occasionali
- immersioni multiple con dettaglio per persona
- contabilita immersioni SMZ su dati salvati
- riepiloghi mensili per SMZ, sanitari e supporti
- salvataggio dell'elaborazione mensile
- export CSV contabilita
- registro immersioni mensile interno
- backup locale ed esterno con ripristino
- import/export di pacchetto servizio

## Moduli Gia Realizzati

### 1. Anagrafica Personale

Presente e ampia rispetto alla base iniziale.

Copre:

- PerID
- cognome, nome, qualifica
- profilo personale
- ruolo sanitario
- codice fiscale
- matricola personale
- numero brevetto SMZ
- dati anagrafici e contatti
- abilitazioni
- visite mediche
- attagliamento

Funzioni operative presenti:

- ricerca con filtri
- suggerimenti in ricerca
- apertura scheda
- modifica e salvataggio
- archiviazione
- ripristino da archivio
- eliminazione definitiva da archivio

### 2. Scadenzario

Presente e integrato con:

- dashboard
- ricerca
- scheda personale

Gestisce le scadenze derivanti soprattutto da visite mediche e abilitazioni.

### 3. Servizio Giornaliero

Il modulo servizio e oggi uno dei blocchi centrali dell'applicazione.

Dati gestiti:

- data servizio
- numero ordine servizio
- orario servizio
- orario in deroga
- lavoro straordinario
- tipo servizio
- localita
- scopo immersione
- unita navale
- indennita fuori sede
- indennita ordine pubblico
- attivita svolta
- note

Vincoli attivi:

- `Indennita fuori sede` e `Indennita ordine pubblico` sono mutuamente esclusive

Funzioni operative presenti:

- nuovo servizio
- salvataggio servizio
- apertura servizio salvato
- eliminazione servizio
- aggiornamento elenco servizi recenti
- apertura rapida dalla card del servizio registrato

### 4. Partecipanti E Supporti

Per ogni servizio il programma gestisce:

- personale SMZ / interno
- gruppo operativo
- presenza
- ruolo operativo
- note
- supporti occasionali non necessariamente presenti in anagrafica stabile

### 5. Immersioni

Il modello dati supporta piu immersioni nello stesso servizio.

Per ogni immersione sono gestiti:

- numero immersione
- orario inizio / fine
- direttore immersione
- operatore soccorso
- assistenza BLSD
- assistenza sanitaria
- note immersione

Per ogni partecipazione immersione sono gestiti:

- flag in immersione
- apparato
- profondita
- fascia profondita
- ore immersione
- categoria contabile ore
- tariffa proposta
- importo di riepilogo
- note

Comportamenti presenti:

- propagazione iniziale di profondita e ore agli altri partecipanti della stessa immersione
- aggiornamento fascia da profondita
- controllo profondita rispetto alla tipologia apparato
- ricalcolo live del riepilogo importo dalla tariffa e dalle ore

### 6. Cataloghi E Regole Contabili

Sono gia strutturati nel database e caricati dall'applicazione:

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

Le tariffe contabili sono modificabili da interfaccia e persistite nel database.

### 7. Camera Iperbarica

Aggiornata rispetto allo stato precedente.

Oggi `C.I.` supporta le fasce:

- `00/12`
- `13/25`
- `26/40`
- `41/55`

Sono state aggiunte anche le relative regole contabili per evitare buchi nel riepilogo/importo.

### 8. Contabilita Mensile

Il modulo contabile e operativo e produce riepiloghi mensili live per:

- SMZ immersioni
- sanitari
- supporti occasionali

Dati presenti:

- ore ordinarie
- ore aggiuntive
- ore sperimentali
- ore camera iperbarica
- importi
- giornate utili per sanitari e supporti

### 9. Elaborazione Mensile Persistita

A differenza della checklist storica iniziale, nel codice attuale esiste una vera persistenza della chiusura mensile.

Sono presenti:

- tabella `ElaborazioniMensili`
- tabella `ElaborazioneMensileRighe`
- funzioni di salvataggio snapshot mensile
- ricarica snapshot mensile salvato

Questo significa che il mese puo essere congelato e richiamato successivamente, anche se l'output documentale finale e ancora parziale.

### 10. Registro Immersioni Mensile

Presente in applicazione.

Il registro viene derivato dai servizi salvati e mostra:

- dettaglio per immersione
- dettaglio per operatore
- fascia
- apparato
- ore
- categoria registro

### 11. Export E Scambio Dati

Oggi esistono due famiglie diverse di export:

#### Export operativo interno

- export CSV della contabilita mensile

#### Scambio servizi

E stata aggiunta una base tecnica per lo scambio di singoli servizi:

- export del servizio salvato in pacchetto `.smzsvc`
- import del pacchetto nel programma principale

Il formato e pensato per un futuro modulo fuori sede e contiene:

- testata servizio
- partecipanti
- immersioni
- supporti occasionali
- riferimenti ai cataloghi tramite codici/descrizioni
- identificativi del personale tramite codice fiscale, matricola, numero brevetto, `PerId` e fallback nominativo

### 12. Backup E Ripristino

Presente come modulo reale applicativo.

Funzioni disponibili:

- backup locale manuale
- backup esterno manuale
- backup locale automatico
- ripristino da backup
- backup di sicurezza prima del restore

## Migliorie Recenti Gia Inserite

Le ultime evoluzioni entrate nel codice comprendono:

- apertura diretta di un servizio salvato dalla card in elenco
- correzione del riepilogo importo immersione come `tariffa x ore`
- correzione della propagazione profondita per evitare il caso `40` riportato come `4`
- aggiunta di `Indennita ordine pubblico`
- incompatibilita tra `Fuori sede` e `Ordine pubblico`
- aggiornamento delle fasce per `C.I.`
- base tecnica per import/export servizio fuori sede tramite pacchetto

## Limiti Attuali E Cose Ancora Da Fare

Il progetto e gia forte sul lato operativo interno, ma ci sono ancora blocchi da chiudere.

### 1. UI Immersioni Ancora Non Completamente Dinamica

Il database supporta piu immersioni, ma la UI non espone ancora in modo pieno:

- aggiunta libera di immersioni
- rimozione libera di immersioni
- gestione completamente dinamica senza limite percepito

### 2. Localita E Scopo Specifici Della Singola Immersione

Il modello dati li supporta, ma la UI attuale del servizio non li espone ancora in modo completo per ogni immersione.

### 3. Prospetti Finali Di Indennita

Mancano ancora prospetti finali dedicati e formalizzati per:

- indennita assistenza SMZ
- indennita fuori sede
- indennita ordine pubblico

Il dato del servizio c'e, ma il prospetto finale mensile strutturato non e ancora chiuso.

### 4. Output Finali Documentali

Sono ancora da completare in forma definitiva:

- stampa/export registro immersioni
- prospetti finali mensili formalizzati
- output PDF / Excel / Word secondo formato da decidere

Oggi e presente solo l'export CSV della contabilita e il pacchetto servizio per scambio dati.

### 5. Import Pacchetto Servizio Ancora Base

L'import/export `.smzsvc` e gia utile come base tecnica, ma non e ancora completo come workflow finale.

Manca ancora:

- deduplica pacchetto gia importato
- gestione aggiornamento di un servizio gia importato
- tracciamento del legame tra pacchetto sorgente e servizio locale
- report errori di import piu guidato

### 6. Cataloghi Ancora Poco Amministrabili Da UI

I cataloghi sono in database, ma non esiste ancora una UI amministrativa completa per gestire tutto senza toccare codice.

### 7. Test Automatizzati

Non emerge al momento un pacchetto strutturato di test automatici.

Per una fase piu matura conviene aggiungere almeno:

- test di import/export servizio
- test di contabilita
- test di persistenza elaborazione mensile

## Valutazione Sul Futuro Modulo Fuori Sede

Con lo stato attuale del codice, un programma separato "solo fuori sede" e realisticamente fattibile.

La strada consigliata e:

1. mantenere il gestionale principale come sistema centrale
2. usare il pacchetto `.smzsvc` come formato di scambio
3. costruire in futuro una versione leggera che compili il servizio fuori sede e generi quel pacchetto
4. importare poi il pacchetto al rientro in sede nel programma principale

Questa strada e migliore del trasferimento del database intero perche:

- riduce i conflitti
- evita dipendenza dal file SQLite locale
- separa bene sorgente dati e sistema centrale
- permette validazioni piu controllate

## Priorita Consigliate Da Qui In Poi

Ordine consigliato di lavoro:

1. chiudere la UI immersioni con numero dinamico di immersioni
2. esporre localita e scopo specifici per singola immersione
3. completare i prospetti finali di indennita
4. completare stampa/export dei report finali
5. rendere robusto il pacchetto `.smzsvc` con deduplica e aggiornamento
6. valutare un modulo separato "fuori sede"
7. aggiungere test automatici sui flussi critici

## Nota Finale

Il progetto non e piu un prototipo semplice "anagrafica + immersioni".

Alla data del 08/04/2026 e gia un gestionale locale strutturato, con persistenza reale dei servizi, contabilita, registro immersioni, chiusura mensile, backup e prima base di scambio dati per scenari futuri fuori sede.

Le mancanze residue non sono piu sul nucleo operativo, ma soprattutto su:

- rifiniture di UI
- output documentali finali
- robustezza del flusso di scambio/import dati
