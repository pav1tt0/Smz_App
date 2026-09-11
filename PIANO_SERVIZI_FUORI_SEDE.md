# Piano operativo - Servizi svolti fuori dalla sede di La Spezia

Documento di lavoro aggiornato all'11 settembre 2026.

## Obiettivo

Consentire agli operatori SMZ di compilare uno o più ordini di servizio durante una trasferta, anche senza collegamento alla rete e senza portare con sé il database principale.

Al rientro a La Spezia, il programma principale deve importare un file, aggiornare il database centrale e rendere immediatamente disponibili i dati per:

- servizio giornaliero e relativa stampa;
- registro immersioni;
- contabilita delle immersioni e delle ore;
- indennita fuori sede;
- assistenza SMZ e altri prospetti collegati.

## Decisione architetturale consigliata

Il database principale deve restare nella sede di La Spezia.

Per il lavoro in trasferta va realizzata un'applicazione separata, leggera e utilizzabile offline. Prima della partenza, il gestionale principale prepara solo i dati strettamente necessari alla missione, per esempio personale autorizzato e cataloghi operativi. L'app fuori sede compila i servizi e genera il pacchetto da importare al rientro.

Non è consigliato copiare fuori sede l'intero database SQLite, perché contiene dati anagrafici, sanitari, contabili e credenziali.

## Stato attuale

### Funzioni già presenti

- [x] Il servizio giornaliero gestisce data, numero ordine, orario, tipo servizio, localita, unita navale, responsabile, partecipanti, immersioni, supporti e note.
- [x] Il servizio può essere marcato come `FuoriSede`.
- [x] Il gestionale principale esporta un singolo servizio in formato `.smzsvc`.
- [x] Il pacchetto versione 1 contiene testata, partecipanti, ruoli, immersioni, dati contabili e supporti occasionali.
- [x] Il gestionale principale importa `.smzsvc` e `.json` e salva il servizio nel database.
- [x] Dopo l'importazione vengono ricaricati servizi, anni e dati mensili ed è avviato un backup locale.
- [x] La contabilita fuori sede ricava dal database personale presente, date e giornate di impiego.
- [x] È disponibile il prospetto mensile dell'indennita fuori sede esportabile in Word.
- [x] Nella copia di lavoro corrente, il numero dell'ordine viene proposto automaticamente usando il giorno progressivo dell'anno (`001`-`366`). Questa modifica è ancora da inserire in un commit.

### Funzioni mancanti o incomplete

- [ ] Non esiste ancora l'applicazione separata da usare durante la trasferta.
- [ ] Il pacchetto può essere importato più volte e creare servizi duplicati.
- [ ] Il `PackageId` viene generato, ma non viene registrato e controllato dal database centrale.
- [ ] L'identificativo del servizio sorgente non crea un collegamento persistente con il servizio importato.
- [ ] Non esiste una gestione guidata per aggiornare un servizio già importato.
- [ ] Non esiste una schermata di anteprima con conferma, avvisi e conflitti prima del salvataggio.
- [ ] Il formato non comprende ancora gli operatori SMZ esterni/subacquei esterni aggiunti successivamente al servizio giornaliero.
- [ ] Il file è JSON in chiaro, senza cifratura e senza verifica dell'autenticita.
- [ ] Mancano limiti espliciti su dimensione del file, numero di righe e quantità di elementi importati.
- [ ] Le validazioni dell'importazione sono meno complete di quelle applicate dalla scheda del servizio.
- [ ] Non è definito cosa accade se il periodo contabile è già stato chiuso o congelato.
- [ ] Non esistono test automatici del ciclo completo esportazione-importazione.

## Flusso operativo da realizzare

### 1. Preparazione prima della partenza

Il gestionale principale deve permettere di creare un pacchetto di preparazione della missione contenente soltanto:

- identificativi essenziali del personale interessato;
- cognome, nome, qualifica, matricola o altro codice stabile necessario all'abbinamento;
- gruppi e ruoli operativi;
- localita, scopi, unità navali, apparati, fasce di profondita e categorie contabili;
- eventuali informazioni generali sulla missione.

Non devono essere inclusi dati sanitari, credenziali, storico contabile o servizi non necessari.

### 2. Compilazione fuori sede

L'applicazione offline deve consentire di:

- creare, modificare e annullare bozze di servizio;
- selezionare data e calcolare automaticamente il numero annuale del giorno;
- indicare localita, orari, responsabile, personale presente e ruoli;
- compilare una o più immersioni;
- registrare profondita, apparato, fascia, ore e categoria contabile;
- gestire assistenze occasionali e operatori SMZ esterni;
- indicare lavoro straordinario e indennita fuori sede;
- eseguire gli stessi controlli applicati dal gestionale principale;
- salvare localmente e riprendere il lavoro dopo la chiusura del programma;
- produrre, se necessario, la stampa del servizio durante la trasferta;
- esportare il pacchetto definitivo per il rientro.

L'app deve funzionare senza connessione Internet e non deve consentire l'accesso alle altre aree del gestionale principale.

### 3. Importazione al rientro

Prima di modificare il database, il gestionale principale deve:

1. controllare tipo e versione del pacchetto;
2. verificare autenticita e integrita del file;
3. applicare limiti di sicurezza e validare tutti i campi;
4. verificare personale e cataloghi;
5. cercare pacchetti o servizi già importati;
6. mostrare un'anteprima completa;
7. segnalare conflitti e richiedere conferma all'utente autorizzato;
8. creare un backup di sicurezza prima dell'importazione;
9. salvare tutto in un'unica transazione;
10. registrare l'operazione in un audit log.

Dopo il salvataggio devono essere aggiornati automaticamente tutti i dati mensili e deve essere mostrato un riepilogo dell'esito.

## Evoluzione del formato di scambio

Preparare una versione 2 del formato mantenendo, se possibile, la lettura controllata dei vecchi pacchetti versione 1.

Il nuovo formato dovrebbe contenere almeno:

- identificativo globale e non riutilizzabile del pacchetto;
- identificativo stabile del dispositivo o della postazione che lo ha prodotto;
- data di creazione e data dell'ultima modifica;
- versione dell'app e dello schema;
- uno o più servizi, secondo la decisione operativa finale;
- impronta del contenuto per rilevare alterazioni;
- firma o altro meccanismo di autenticita;
- cifratura dei dati durante il trasporto;
- stato del pacchetto: bozza, definitivo, annullato o sostitutivo;
- riferimento al pacchetto precedente in caso di correzione.

Nel database centrale serve una tabella dei pacchetti importati con vincolo univoco sull'identificativo globale.

## Regole contro duplicati e conflitti

- Lo stesso pacchetto non deve poter essere importato due volte.
- Una nuova esportazione dello stesso servizio deve essere riconosciuta come aggiornamento, non come nuovo servizio.
- Data e numero ordine devono essere confrontati con i servizi già presenti.
- In caso di conflitto l'importazione deve fermarsi e mostrare le differenze.
- L'utente deve poter scegliere soltanto tra operazioni esplicite e autorizzate: annulla, importa come nuovo oppure sostituisci/aggiorna.
- Una sostituzione deve conservare traccia della versione precedente, dell'autore, della data e della motivazione.
- Un servizio appartenente a un periodo contabile chiuso non deve essere modificato silenziosamente.

## Sicurezza e protezione dei dati

- Usare solo dati personali indispensabili alla missione.
- Cifrare sia l'archivio locale dell'app fuori sede sia il pacchetto trasferito.
- Verificare che il file provenga da una postazione autorizzata.
- Non memorizzare password nel pacchetto.
- Definire ruoli separati per compilazione, approvazione e importazione.
- Prevedere scadenza o revoca dei pacchetti di preparazione.
- Cancellare in modo controllato i dati locali della missione dopo l'importazione e la conferma del responsabile.
- Non inserire mai nei test o nel repository pacchetti contenenti dati reali.

## Piano di implementazione

### Fase 1 - Stabilizzare il gestionale principale

- [ ] Definire il contratto del pacchetto versione 2.
- [ ] Aggiungere la tabella dei pacchetti importati.
- [ ] Implementare deduplica e collegamento sorgente-destinazione.
- [ ] Aggiungere validazione completa, limiti e controllo dei conflitti.
- [ ] Aggiungere anteprima e conferma prima dell'importazione.
- [ ] Spostare il backup prima della modifica del database.
- [ ] Gestire periodi contabili chiusi.
- [ ] Integrare operatori SMZ esterni e tutti i dati oggi mancanti.
- [ ] Aggiungere test automatici dell'importatore.

Questa fase deve essere completata prima di distribuire l'app fuori sede.

### Fase 2 - Estrarre la logica condivisa

- [ ] Creare una libreria comune per modelli, validazioni e formato del pacchetto.
- [ ] Condividere il calcolo del numero annuale del servizio.
- [ ] Condividere le regole su orari, partecipanti, immersioni e contabilita.
- [ ] Evitare di duplicare le regole tra programma principale e app fuori sede.

### Fase 3 - Realizzare l'app fuori sede

- [ ] Creare un nuovo progetto dedicato, con interfaccia ridotta.
- [ ] Consentire l'importazione del pacchetto di preparazione missione.
- [ ] Realizzare bozze persistenti e protette.
- [ ] Implementare compilazione, controlli e stampa del servizio.
- [ ] Implementare esportazione definitiva del pacchetto.
- [ ] Preparare una distribuzione Windows `win-x64` autonoma e offline.

### Fase 4 - Collaudo completo

- [ ] Eseguire prove con dati fittizi ma realistici.
- [ ] Confrontare la contabilita ottenuta con un inserimento manuale equivalente.
- [ ] Provare il flusso su un PC diverso e senza rete.
- [ ] Effettuare una prova operativa guidata con gli utilizzatori.
- [ ] Correggere problemi di usabilità prima della distribuzione reale.

## Test minimi obbligatori

- esportazione e importazione completa senza perdita di dati;
- importazione ripetuta dello stesso pacchetto bloccata;
- pacchetto alterato, corrotto o troppo grande rifiutato;
- persona mancante o identificazione ambigua gestita senza modifiche parziali;
- catalogo mancante o non compatibile segnalato chiaramente;
- servizio con più immersioni e più partecipanti;
- assistenza occasionale e operatori SMZ esterni;
- giorno progressivo corretto negli anni normali e bisestili;
- conflitto con stesso giorno e numero ordine;
- correzione di un servizio già importato;
- importazione in periodo contabile chiuso;
- errore durante il salvataggio con rollback completo;
- aggiornamento corretto di contabilita, registro immersioni e prospetto fuori sede;
- backup precedente all'importazione verificabile e ripristinabile.

## Decisioni operative ancora da prendere

1. Un file deve contenere un solo servizio oppure tutti i servizi della trasferta?
2. Possono esistere più servizi nello stesso giorno con lo stesso numero annuale?
3. Chi può compilare, rendere definitivo e importare un servizio?
4. Quali persone devono essere disponibili nell'app fuori sede: tutto il personale oppure solo gli assegnati alla missione?
5. Serve stampare e firmare il servizio già durante la trasferta?
6. Come deve essere gestita una correzione inviata dopo la prima importazione?
7. Per quanto tempo devono rimanere sul PC fuori sede i dati della missione?
8. Quale supporto verrà usato per il trasferimento: chiavetta autorizzata, cartella protetta o altro canale?

## Criteri di completamento

Il lavoro può essere considerato concluso soltanto quando:

- il servizio viene compilato completamente fuori sede senza Internet e senza il database principale;
- il file trasferisce soltanto i dati necessari ed è protetto contro lettura e alterazione;
- l'importazione mostra un'anteprima e richiede conferma;
- ogni pacchetto può essere acquisito una sola volta;
- gli aggiornamenti non producono duplicati e lasciano una cronologia;
- un errore non lascia dati parziali nel database;
- la contabilita risultante coincide con quella di un inserimento manuale equivalente;
- backup, audit e gestione dei periodi chiusi sono verificati;
- i test automatici e il collaudo operativo sono superati;
- la procedura di preparazione, trasferta, rientro e cancellazione dei dati è documentata per gli utenti.

## Ordine consigliato per la ripresa

1. Rispondere alle decisioni operative ancora aperte.
2. Rendere sicuro e idempotente l'importatore del gestionale principale.
3. Definire e testare il pacchetto versione 2.
4. Estrarre le regole condivise.
5. Costruire l'app fuori sede.
6. Eseguire il collaudo completo con dati non reali.

Fino al completamento almeno delle fasi 1 e 4, il pacchetto attuale `.smzsvc` deve essere considerato una base tecnica e non un flusso definitivo per dati operativi reali.
