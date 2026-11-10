# Audit tecnico del progetto SMZ — 9 novembre 2026

Riferimento esaminato: checkout `C:\Users\LENOVO\Desktop\SMZ`, commit `4c29582` (`Compatta il personale e migliora stampe e gestione delle scadenze`). Il checkout era pulito all'inizio. L'audit aggiunge questo documento e strumenti locali sotto `artifacts/audit-2026-11-09/`; non modifica il codice applicativo o i dati operativi.

## Valutazione

**Aggiornamento sul contesto d'uso:** l'utente ha chiarito che SMZ è un progetto amatoriale, destinato a contabilizzare servizi e immersioni dei SMZ del Nucleo di SP in locale su notebook. Il piano operativo è stato ridotto: prima correttezza contabile, stampe e recupero dei dati; le protezioni più articolate vanno valutate solo se necessarie. L'utente e il collega utilizzatore decideranno il 10 novembre 2026. Fa riferimento il [promemoria del confronto](PROMEMORIA_CONFRONTO_COLLEGA_2026-11-10.md); non è autorizzata l'implementazione automatica di tutti i rilievi.

La versione Release compila e supera i 14 scenari automatici esistenti. Il controllo NuGet online non segnala vulnerabilità note nelle dipendenze risolte della soluzione. Il ripristino dei backup è molto più protetto rispetto all'audit di settembre.

Restano problemi verificati di riservatezza, controllo degli accessi e integrità dei dati, fra cui account rimasti attivi dopo la cancellazione del personale, sessioni non revocate e copie dei dati senza cifratura applicativa. Il loro ordine di intervento va proporzionato all'uso locale descritto, dando precedenza a contabilità, stampe e recupero. Inoltre .NET 8 termina il supporto il **10 novembre 2026**, il giorno successivo alla data di questo rapporto.

Chiarimento dell'utente dopo l'audit: l'avvio Debug senza login è stato richiesto per velocizzare le modifiche. È una scelta di sviluppo confermata, non un difetto da correggere. F03 è conservato come riferimento descrittivo e rimosso dalle urgenze; resta il controllo della configurazione Release per la distribuzione.

Non sono emerse evidenze di una violazione già avvenuta. Il rapporto identifica difetti e condizioni di rischio; non costituisce una certificazione di sicurezza o una verifica completa di conformità privacy.

## Perimetro, metodo e limiti

- Inventario dei 117 file C#/XAML applicativi, per circa 28.462 righe; lettura approfondita dei percorsi di accesso, persistenza, avvio, migrazione, backup, import/export, contabilità e distribuzione. Esame della struttura UI, dei servizi di stampa e della documentazione operativa.
- Build Release della soluzione e dei due progetti ausiliari `SMZ.TestPackage` e `SMZ.PrintPreview`.
- Esecuzione dei 14 scenari del runner esistente e di 14 verifiche aggiuntive mirate, tutte su dati fittizi isolati.
- Ricerca mirata di segreti evidenti e chiamate di rete nei sorgenti applicativi/script; non sono risultate credenziali di produzione hardcoded evidenti. Le password dei test sono dati fittizi.
- Controllo dei documenti Office Open XML tracciati: nessun candidato codice fiscale/email con le espressioni usate e nessun `vbaProject.bin` nei sei DOCX/XLSX esaminati. Questo controllo non dimostra l'anonimizzazione: nomi, importi, immagini, metadati, file `.doc` e cronologia Git richiedono verifica separata.
- Lettura delle ACL del database ordinario, senza leggerne i record o modificarlo. Sul PC corrente risultano autorizzazioni ereditate per utente, amministratori, SYSTEM e gruppo del sandbox. Non è una verifica delle autorizzazioni sui PC destinatari o sulle cartelle esterne. Cifratura del volume e configurazione BitLocker non verificate.
- Non sono stati eseguiti collaudo manuale completo delle finestre, confronto visivo di tutte le stampe, prova su un secondo PC, stress test, pentest esterno, scansione antivirus o verifica della visibilità del repository remoto. Nessun upload di database o documenti.

## Priorità e cose da fare

La tabella conserva la **classificazione tecnica iniziale**, precedente al chiarimento sull'uso locale. Le etichette U1/U2/U3 non sono scadenze o impegni approvati: l'ordine operativo aggiornato è quello del promemoria per il collega. In particolare account, sessioni e protezioni articolate non precedono automaticamente contabilità e stampe. Restano oggettivi la scadenza di supporto del runtime e i difetti riprodotti; lo scambio servizi richiede correzioni prima dell'uso solo se verrà effettivamente utilizzato.

| ID | Urgenza | Problema | Evidenza |
| --- | --- | --- | --- |
| F01 | U1 | .NET 8 a fine supporto il 10/11/2026; runtime locale non aggiornato all'ultima patch pubblicata | Progetto, ambiente e fonte Microsoft |
| F02 | U1 | Database, backup, export e copie locali senza cifratura applicativa | Codice e prova su DB fittizio |
| F03 | Scelta confermata | Avvio Debug senza login richiesto per velocizzare lo sviluppo; nessuna modifica richiesta | Codice e chiarimento dell'utente |
| F04 | U1 | Account utilizzabile dopo eliminazione o archiviazione del personale | Riprodotto |
| F05 | U1 | Sessione aperta conserva privilegi dopo sospensione/declassamento | Riprodotto nel ViewModel |
| F06 | U2 | Lo scambio servizi omette gli operatori sub esterni | Riprodotto |
| F07 | U2 | Import ripetibile, validazione insufficiente e lettura senza limite esplicito | Riprodotti duplicati e dati incoerenti; limite assente nel codice |
| F08 | U2 | Seeding annulla modifiche ai cataloghi predefiniti | Riprodotto per località; stesso schema per unità navali |
| F09 | U2 | Retention numerica e configurazione corrotta indeboliscono i backup | Riprodotto |
| F10 | U2 | Formule non neutralizzate nei CSV | Riprodotto sull'encoder |
| F11 | U2 | Chiusure mensili sovrascrivibili senza versioni; importi/ore in REAL | Riprodotta sovrascrittura; schema verificato |
| F12 | U2 | Mancano limite tentativi, blocco per inattività e audit delle operazioni | Analisi del codice |
| F13 | U2 | Gli export nei backup non sono coperti da hash; manca autenticità del pacchetto | Riprodotta modifica di un export accettata dal restore |
| F14 | U2 | Salvataggi da copie obsolete perdono modifiche precedenti | Riprodotto |
| F15 | U3 | Pubblicazione meno robusta, versione poco riconoscibile e pacchetto non firmato | Script e binario locale verificati |
| F16 | U3 | Automazione, test e struttura del codice da consolidare | Struttura del repository e copertura osservata |

## Dettagli e criteri di chiusura

### F01 — Aggiornamento del runtime

`SMZ.Conta.App/SMZ.Conta.App.csproj:5` usa `net8.0-windows`; anche gli altri progetti si basano su .NET 8. L'ambiente di audit ha SDK 8.0.419 e runtime 8.0.25. La pagina Microsoft consultata riporta 8.0.33 come patch corrente e fine supporto di .NET 8 al 10 novembre 2026; .NET 10 LTS risulta supportato fino al 14 novembre 2028. Non è stata associata una specifica CVE sfruttabile a SMZ sulla sola base di questa differenza.

**Intervento:** migrare a .NET 10 LTS con SDK aggiornato, ricompilare, provare migrazioni/stampe/backup e rigenerare le distribuzioni. Se occorre una transizione, aggiornare almeno la patch 8 corrente, sapendo che non estende il supporto. Nei pacchetti self-contained il runtime è incorporato: aggiornare quello di Windows non aggiorna il pacchetto già distribuito.

**Chiuso quando:** build e test passano sul target supportato; un pacchetto nuovo su PC pulito riporta versione e runtime corretti e conserva i dati esistenti.

Fonti: [politica di supporto Microsoft](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [distribuzione self-contained](https://learn.microsoft.com/en-us/dotnet/core/deploying/).

### F02 — Riservatezza di database e copie

`Data/DatabasePaths.cs:49` costruisce una connessione SQLite ordinaria. `Data/BackupService.cs:589` crea ZIP non cifrati; gli export JSON, CSV, DOCX e XLSX sono prodotti senza cifratura applicativa. Il database contiene anagrafica, informazioni sulle visite, servizi, contabilità e hash/sali delle password. La prova su dati fittizi conferma l'intestazione leggibile `SQLite format 3`.

La password dell'applicazione non protegge i file da chi ha già diritto di leggerli. Lo stesso problema riguarda copie su USB, cartelle condivise/sincronizzate, directory temporanee e vecchi pacchetti. Sono presenti in `dist/Backup-Anagrafica-Collega` due `.smzbak` e un `.db`, oltre al pacchetto nominato `SMZ-Collega-Anagrafica-Precompilata-2026-07-16.zip`: il loro contenuto personale non è stato ispezionato e va classificato prima di qualsiasi condivisione. `dist` è ignorata da Git, ma questo non protegge copie o invii manuali.

**Intervento immediato:** inventariare queste copie, limitarne accesso e condivisione e usare supporti/volumi cifrati approvati. Non cancellarle prima di decidere conservazione e necessità di recupero.

**Intervento applicativo:** scegliere la protezione del database e delle chiavi, cifrare e autenticare i backup trasferibili, gestire recupero su nuovo PC e minimizzare gli export. Non basta aggiungere `Password=` al provider SQLite ordinario. La cifratura del volume protegge soprattutto il supporto a riposo; non risolve letture da processi già autorizzati o da un account Windows condiviso.

**Chiuso quando:** una copia sottratta non è leggibile senza la chiave; perdita password/PC e cambio utente sono coperti da una procedura di recupero provata; ACL di database, backup e destinazioni esterne sono verificate sui PC effettivi.

Fonte: [cifratura in Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/encryption).

### F03 — Avvio di sviluppo senza login: scelta confermata

`Avvia-SMZ.cmd:3` richiama lo script senza opzioni. `Avvia-SMZ.ps1:3` usa `Debug` come valore predefinito. `App.xaml.cs:55` in Debug crea direttamente una sessione amministrativa con login disattivato. Non viene selezionato automaticamente un profilo dati di sviluppo: il percorso predefinito resta `%LOCALAPPDATA%\SMZ\Conta`.

L'utente ha confermato di aver richiesto questo comportamento per velocizzare le modifiche. L'avvio di sviluppo va quindi mantenuto: non costituisce un difetto rispetto al comportamento concordato. La classificazione iniziale come U1 è ritirata.

Il pacchetto Mario Rossi generato dallo script pulito è pubblicato esplicitamente in Release con profilo separato. Come controllo della distribuzione, verificare che i pacchetti destinati all'uso operativo siano Release e richiedano autenticazione; questo controllo rientra in F15. `Publish-Test-Operatore.ps1` accetta attualmente una configurazione libera.

**Stato:** nessuna correzione richiesta all'avvio di sviluppo. Non cambiare il lanciatore o riattivare il login durante le modifiche sulla base di questo audit.

### F04 — Ciclo di vita degli account

`PersonaleRepository.PersonaleSave.cs:117` e `:151` eliminano/archiviano il personale senza aggiornare `UtentiAccesso`. La tabella utenti (`DatabaseInitializer.cs:523`) non ha un vincolo verso l'anagrafica. `AccessService.Authenticate`, a riga 48, controlla l'account ma non la presenza del dipendente. Le prove confermano l'accesso dopo entrambe le operazioni, anche come amministratore.

La cessazione dall'interfaccia propone già di sospendere l'account (`MainWindowViewModel.Personale.cs`, metodo `DisattivaPersonaleDaOggi`): è una mitigazione parziale e non copre la cancellazione definitiva. Il fatto che Base possa creare e modificare le anagrafiche è invece una funzione documentata, non una violazione dedotta dall'audit.

**Intervento:** rendere atomici modifica del personale e sospensione dell'account, decidere esplicitamente il comportamento dello storico e del ripristino e proteggere l'ultimo amministratore. Controllare anche eventuali account già orfani.

**Chiuso quando:** account orfani/archiviati non accedono; cancellazione e rollback non lasciano stati intermedi; una nuova scheda con lo stesso PerID non eredita accessi in modo implicito.

### F05 — Revoca delle sessioni

`MainWindowViewModel.Access.cs:23` calcola i privilegi dalla sessione in memoria. `EnsureAdministratorAccess` a riga 139 verifica lo stesso valore, senza rileggere lo stato corrente. Una prova crea un ViewModel amministrativo, poi declassa e sospende l'utente: `IsAdministrator` rimane `true`.

I metodi di gestione degli account in `AccessService` rivalidano già l'amministratore, quindi non tutte le operazioni rimangono possibili. Le altre aree che si affidano alla sessione/UI non ricevono però una revoca generale.

**Intervento:** introdurre rivalidazione e invalidazione delle sessioni dopo sospensione, cambio ruolo o reset, con controllo nei servizi delle operazioni sensibili e blocco per inattività. Definire l'effetto su eventuali altre istanze.

**Chiuso quando:** una sessione revocata non può più scrivere o esportare attraverso le aree privilegiate e viene riportata all'accesso senza perdere silenziosamente modifiche.

### F06 — Perdita degli operatori esterni nello scambio

`ServizioScambioPackageService` non rappresenta `OperatoriSubEsterni`; il formato delle immersioni non rappresenta `PartecipazioniEsterne`. `ServizioScambioService.BuildDocument` e `BuildServizio` omettono questi campi. La prova salva un servizio con un esterno, lo esporta e importa: l'esterno non è più presente. Il servizio originale non viene cancellato, ma la copia ricevuta è incompleta e può dare report/contabilità incompleti.

**Intervento:** estendere il formato con versione nuova e round-trip completo. Fino alla correzione, avvisare o rifiutare l'esportazione di un servizio che perderebbe dati.

**Chiuso quando:** esportazione e importazione preservano interni, esterni, partecipazioni e supporti; i pacchetti precedenti hanno una gestione esplicita.

### F07 — Importazione non idempotente e poco validata

`ServizioScambioService.cs:43-67` legge l'intero file e salva un servizio nuovo. `PackageId` e `SourceServizioGiornalieroId` sono presenti nel formato ma non impediscono la reimportazione. La prova importa lo stesso file due volte e ottiene due ID diversi. Un altro contenuto importato conserva **-5 ore**, **-10 metri** e un partecipante indicato come assente ma associato all'immersione.

La scrittura usa già una transazione: non manca l'atomicità del salvataggio; manca una validazione di dominio comune e un'identificazione persistente degli import. Non ci sono limiti espliciti del file, delle collezioni e dei campi testuali prima del caricamento. Il rischio di consumo eccessivo di memoria è ricavato dal codice, non provato con un file enorme.

**Intervento:** registro degli import con identità origine/pacchetto, gestione esplicita di duplicati e aggiornamenti, limiti e validazione condivisa tra editor e import. Cifrare/autenticare lo scambio quando usato fuori dal perimetro fidato. `STATO_LAVORI_SMZ_2026-09-11.md` segnala già che il flusso attuale non è quello definitivo per dati reali.

**Chiuso quando:** il secondo import non duplica il primo; un pacchetto incoerente viene rifiutato senza scritture parziali; un file oltre soglia viene respinto prima di essere caricato integralmente. Le visite mancanti/scadute restano informative e non bloccanti come richiesto.

### F08 — Modifiche ai cataloghi annullate

`DatabaseInitializer.cs:830` e `:880` usano `ON CONFLICT ... DO UPDATE` sui cataloghi predefiniti a ogni `EnsureDatabase()`. La prova modifica descrizione e stato attivo di una località, poi simula l'inizializzazione al riavvio: entrambi tornano ai valori di origine. Il pattern delle unità navali è analogo. Il ripristino richiama anch'esso `EnsureDatabase`, quindi il problema può alterare anche valori appena recuperati da backup.

**Intervento:** separare inserimento dei valori iniziali da migrazioni esplicite; non sovrascrivere le personalizzazioni amministrative a ogni avvio.

**Chiuso quando:** località/unità predefinite modificate restano tali dopo riavvio e ripristino; un catalogo nuovo viene comunque inizializzato correttamente.

### F09 — Recuperabilità e monitoraggio dei backup

`BackupService.cs:16-17` conserva 20 backup locali e 30 esterni; `PruneOldBackups` a riga 743 considera soltanto il numero. La prova crea più di 20 backup nella stessa giornata e vede scomparire il primo: il numero di copie non garantisce giorni/settimane di storico. Un JSON di impostazioni invalido viene assorbito da `LoadSettings` a riga 46 e restituisce una configurazione senza destinazione esterna, senza segnalare la corruzione.

Gli errori esterni vengono principalmente affidati a `Stato`; nel backup automatico di avvio il messaggio viene poi sostituito con il successo del backup locale (`MainWindowViewModel.Backup.cs:172-175`). Non è un'indicazione affidabile dello stato della copia esterna.

Ulteriori limiti statici: creazione e restore non condividono limiti di dimensione/numero file, perciò la creazione non impedisce un archivio che il restore potrebbe rifiutare. Inoltre `CreateBackup` pubblica direttamente il nome finale e fa pruning prima della chiusura dell'archivio: un'interruzione può lasciare un file incompleto. Queste due condizioni non sono state simulate con crash o archivi di grandi dimensioni.

**Intervento:** conservazione giornaliera/settimanale/mensile, almeno una copia indipendente dal PC, avviso persistente per backup fallito/vecchio o impostazioni corrotte; creazione in file temporaneo, chiusura/verifica e successiva pubblicazione del file finale, poi pruning. Definire e provare tempi massimi di perdita dati e di recupero.

**Chiuso quando:** numerosi salvataggi nello stesso giorno non cancellano lo storico previsto; un errore esterno rimane visibile; ogni backup dichiarato riuscito si ripristina su profilo isolato. Questo problema non annulla le protezioni di restore già implementate.

### F10 — Formula injection nei CSV

`MainWindowViewModel.Snapshots.cs:582` esegue escaping di separatori, virgolette e righe, ma lascia `=1+1` invariato: prova effettuata con la funzione realmente usata dall'export contabile. Aprendo il CSV con un foglio elettronico un campo testuale può essere interpretato come formula. Non è stata eseguita una formula malevola in Excel.

**Intervento:** trattare come testo i campi non numerici, definire i programmi destinatari e verificare formule, prefissi e caratteri di controllo. Preferire l'XLSX con celle testuali per la lettura umana; l'escaping CSV da solo non garantisce un comportamento uniforme dopo salvataggi successivi.

**Chiuso quando:** casi `=`, `+`, `-`, `@`, separatori e caratteri di controllo non diventano formule nei programmi supportati senza alterare gli importi numerici legittimi.

Fonte: [OWASP CSV Injection](https://community.owasp.org/attacks/CSV_Injection).

### F11 — Chiusure contabili e precisione

`PersonaleRepository.Accounting.cs:216-276` aggiorna la stessa mensilità cancellando le righe precedenti. La UI chiede già conferma della rigenerazione, quindi la sostituzione non è completamente inattesa; manca però una versione precedente consultabile con autore e motivo. La prova salva una mensilità con righe e la risalva vuota: le righe precedenti spariscono.

`DatabaseInitializer.cs:317,509-514` usa `REAL` per tariffe/importi/ore; parte degli importi è calcolata direttamente in SQL. È un rischio di precisione e riconciliazione, non la prova di un pagamento già errato.

**Intervento:** versioni delle chiusure con stato e motivazione di riapertura; rappresentazione economica e temporale con precisione/arrotondamento definiti. Valutare centesimi e minuti interi, verificando prima che le regole ammettano tutte le frazioni richieste. Non modificare tariffe o regole operative confermate durante il solo refactoring.

**Chiuso quando:** una rigenerazione conserva la versione precedente e il confronto degli importi; casi limite di arrotondamento sono approvati e riproducibili.

### F12 — Rafforzamento dell'accesso e tracciabilità

`AccessService` usa PBKDF2-SHA256 con sale casuale, 310.000 iterazioni e confronto constant-time: le password non sono in chiaro. Mancano però limitazione progressiva dei tentativi, scadenza per inattività e registro applicativo delle modifiche/esportazioni/ripristini. `UltimoAccesso` non è un audit delle operazioni. Il fattore PBKDF2 è inferiore alle 600.000 iterazioni indicate da OWASP nella fonte consultata.

I ruoli sono Base e Amministratore. Come documentato, Base può modificare le anagrafiche: bisogna verificare con i responsabili se l'accesso ai campi sulle visite e alle altre informazioni personali corrisponde alle necessità effettive. Non si propone di restringere arbitrariamente un comportamento concordato.

**Intervento:** ritardi/blocchi temporanei progettati per evitare abusi, blocco schermata, audit con autore/azione/esito, niente password o contenuti sanitari nei log, conservazione dei log definita. Aumentare il costo delle nuove password con benchmark e aggiornamento degli hash al login; definire una matrice di autorizzazioni applicata nei servizi.

**Chiuso quando:** tentativi e sessioni inattive sono gestiti; le operazioni critiche hanno una traccia consultabile e protetta; i vecchi hash sono aggiornabili senza invalidare gli account.

Fonte: [OWASP Password Storage](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html).

### F13 — Integrità completa e autenticità dei backup

Il restore verifica già hash del database, struttura, integrità SQLite, relazioni, limiti dell'archivio e percorsi. Il manifest non include però hash degli export. La prova modifica `Export/audit.txt` dentro un backup senza toccarne il manifest e il ripristino accetta il documento alterato.

L'hash del DB contenuto nello stesso archivio rileva danni rispetto a quel manifest, ma non dimostra chi ha prodotto il pacchetto: chi può riscrivere entrambi può aggiornarlo. Non è stato dimostrato un accesso remoto tramite backup; il restore rimane un'operazione amministrativa e richiede nuova autenticazione al termine.

**Intervento:** coprire tutti gli elementi con manifest e protezione autenticata, stabilire chiavi e provenienza fidata e una politica per i formati legacy. Allineare questa scelta alla cifratura di F02.

**Chiuso quando:** modificare database, manifest o un singolo export produce rifiuto prima di cambiare dati; rimane possibile recuperare legittimamente un backup su nuovo PC.

### F14 — Modifiche perse con due copie della stessa scheda

`PersonaleRepository.PersonaleSave.cs:76-101` aggiorna la scheda per PerID senza versione attesa; salva l'intero modello e sostituisce i record figli. La prova carica due copie, modifica il telefono nella prima e il nome nella seconda: salvando la seconda sparisce il telefono appena cambiato dalla prima. Due istanze sullo stesso profilo possono riprodurre questa situazione. L'audit non ha aperto due UI reali contemporaneamente.

**Intervento:** controllo di concorrenza tramite revisione/valore atteso e ricaricamento guidato; se l'uso previsto è una sola istanza, impedirne altre sullo stesso profilo. Valutare la stessa protezione per servizi e cataloghi.

**Chiuso quando:** il secondo salvataggio rileva il conflitto e non sovrascrive in silenzio.

### F15 — Affidabilità della distribuzione

`scripts/Publish-Test-Operatore.ps1:20` non verifica `$LASTEXITCODE` subito dopo `dotnet publish`: in Windows PowerShell un exit code nativo non viene fermato dal solo `$ErrorActionPreference = 'Stop'`. Con una cartella di pubblicazione preesistente potrebbe quindi produrre un archivio di file vecchi/misti e annunciare successo. È un difetto statico; non è stata provocata una pubblicazione fallita. Lo script `Publish-Test-Pulito.ps1` controlla invece già gli exit code e conserva i pacchetti precedenti.

L'EXE del pacchetto test del 13 ottobre presente in `dist` risulta `NotSigned` e versione file `1.0.0.0`. Il manifest SHA-256 aiuta a verificare coerenza, ma senza una provenienza fidata non autentica da solo il produttore. Non è stato ricertificato il contenuto completo di tutti gli ZIP precedenti.

**Intervento:** usare un'unica procedura con staging nuovo, stop su errori, Release obbligatorio, test e verifica seed/profilo, versione leggibile collegata al commit, runtime e hash nel manifesto di release; valutare firma Authenticode per gli artefatti distribuiti.

**Chiuso quando:** un errore di pubblicazione non crea ZIP né annuncia successo; è possibile risalire dal pacchetto al commit e verificare origine, dati iniziali e isolamento.

### F16 — Automazione, test e manutenzione

La suite è un eseguibile console con 14 scenari: il comando corretto è `dotnet run --project SMZ.Conta.Tests/SMZ.Conta.Tests.csproj -c Release --no-build`. Un semplice `dotnet test` non documenta l'esecuzione di questi scenari. Non sono emersi workflow CI nel checkout né pin dell'SDK tramite `global.json` o lock file NuGet.

Le 14 prove di audit riproducono problemi che la suite attuale non rileva. Il MainWindowViewModel è suddiviso in file `partial`, ma rimane la stessa classe con responsabilità estese: il file principale ha 2.332 righe, Accounting 1.551, ServiceDrafts 1.329. La separazione dei file migliora l'orientamento ma non isola validazione, persistenza, UI e regole.

**Intervento:** trasformare i difetti corretti in regressioni mirate, automatizzare build/audit/runner e smoke del pacchetto, includere i progetti ausiliari e controllare esplicitamente l'esecuzione dei test. Estrarre gradualmente validazioni, regole e servizi, preservando i binding e le funzioni esistenti. Aggiungere casi di migrazione da vecchi DB fittizi e verifiche delle stampe con pagine lunghe e operatori esterni.

**Chiuso quando:** una modifica che reintroduce ciascun difetto corretto fallisce nei controlli automatici; una release è ricostruibile con ambiente dichiarato.

## Misure già presenti da conservare

- SQL generalmente parametrizzato; i nomi dinamici individuati provengono da codice/cataloghi o da allowlist. Non è emersa SQL injection evidente nei percorsi esaminati.
- Transazioni per salvataggi principali e migrazioni; vincoli relazionali SQLite attivati nelle connessioni applicative.
- PBKDF2, sali casuali e confronto constant-time; cambio obbligatorio delle password temporanee nel percorso Release.
- Il restore usa staging, limiti di file/byte, controllo percorsi e duplicati, manifest, SHA-256 del DB, `integrity_check`, `foreign_key_check`, rifiuto di trigger/view inattesi, sostituzione con rollback e richiesta di nuovo accesso.
- Audit NuGet abilitato per tutte le dipendenze e warning alti/critici trattati come errori in `Directory.Build.props`. Va reso evidente anche il fallimento del recupero degli advisory (`NU1900`), senza presentarlo come audit riuscito.
- Il profilo Mario Rossi è separato dal profilo ordinario e i relativi test passano. L'avvio Debug senza login è una scelta di sviluppo richiesta dall'utente; la distribuzione Release segue il percorso con autenticazione.
- Visite facoltative e non bloccanti confermate dai test; D.Lgs. 81/08 a 12 mesi e mantenimento brevetto M.M. a 24 mesi. La visita bimestrale è disattivata, con conservazione dei dati pregressi. Non sono problemi da "correggere" cambiando regole già concordate.

## Piano operativo consigliato

Piano ridotto da discutere con il collega il 10 novembre, senza avviare correzioni prima delle decisioni:

1. Confrontare la contabilità con esempi e risultati approvati: categorie, tariffe, ore, maggiorazioni, arrotondamenti e riepiloghi. Non assumere che i conteggi attuali siano errati.
2. Collaudare completezza e impaginazione delle stampe realmente utilizzate, compresi operatori esterni e servizi con molte righe.
3. Correggere la perdita delle modifiche ai cataloghi al riavvio.
4. Concordare backup locale, copia periodica separata, conservazione semplice e prova di ripristino.
5. Conservare una versione datata delle chiusure prima della rigenerazione, scegliendo una soluzione proporzionata.
6. Correggere perdita dati, duplicazioni e validazione dello scambio servizi prima di usarlo, se il flusso è necessario.

Rinviare ruoli più granulari, revoca sessioni, limitazione tentativi, audit log articolati, firma e refactoring ampio salvo esigenze concrete. Valutare prima le protezioni Windows per notebook/supporti; non avviare automaticamente cifratura applicativa o conversione dei calcoli da REAL senza verifica dei risultati. Pianificare separatamente l'aggiornamento del runtime. Mantenere l'avvio di sviluppo senza login.

Per ciascuna attività: usare dati fittizi o copie isolate, verificare migrazione/rollback, documentare il risultato e generare un pacchetto solo dopo i controlli pertinenti. L'audit non ha applicato queste correzioni.

## Sicurezza dei dati: verifiche organizzative ancora necessarie

| Area | Cosa definire o verificare | Risultato richiesto |
| --- | --- | --- |
| Accessi | Utenti individuali Windows/app, gruppi, destinatari di dati sulle visite, contabilità ed export | Matrice autorizzazioni approvata e applicata |
| Cifratura | Volumi dei PC, supporti esterni, chiavi applicative, recupero dopo cambio PC | Prova di lettura negata senza chiave e recupero autorizzato riuscito |
| Conservazione | Durata di anagrafica, visite, archivi, export, backup e log | Regole coerenti; eliminare una scheda non equivale a eliminarla dalle copie |
| Scambi | USB, rete, cartelle sincronizzate, pacchetti e destinatari | Canali approvati, accesso minimo e copie identificabili |
| Documenti | Nominativi/importi/metadati nei modelli Office, file legacy e cronologia Git | Anonimizzazione verificata e visibilità del remoto confermata |
| Recupero | Perdita massima accettabile e tempo massimo di ripristino | Prova periodica su profilo/PC separato, non solo presenza del file |
| Incidenti | Referenti e procedura in caso di perdita dispositivo/copia o accesso anomalo | Responsabilità e azioni documentate |

Queste verifiche vanno svolte con chi gestisce infrastruttura e trattamento dei dati; l'ispezione del codice non le può confermare.

## Registro delle verifiche eseguite

| Verifica | Esito |
| --- | --- |
| `dotnet restore SMZ.Conta.sln --verbosity minimal` | Completato; warning NU1900 per accesso agli advisory limitato dal sandbox |
| `dotnet build SMZ.Conta.sln -c Release --no-restore --verbosity minimal` | Riuscita, 0 errori, 2 warning NU1900 |
| `dotnet run --project SMZ.Conta.Tests/SMZ.Conta.Tests.csproj -c Release --no-build` | 14 scenari passati; SQLite caricato 3.53.3 |
| `dotnet list SMZ.Conta.sln package --vulnerable --include-transitive --format json` | Ripetuto con accesso di rete autorizzato: exit 0, nessun pacchetto vulnerabile segnalato |
| Build Release `scripts/SMZ.TestPackage/SMZ.TestPackage.csproj` | Riuscita, 0 errori; warning NU1900 |
| Build Release `scripts/SMZ.PrintPreview/SMZ.PrintPreview.csproj` | Riuscita, 0 errori; warning NU1900 |
| Probe di audit | 14 condizioni riprodotte su DB fittizi |
| Firma EXE test 13 ottobre | NotSigned; versione 1.0.0.0 |

Il successo NuGet vale per gli advisory disponibili al momento del comando e per le dipendenze risolte della soluzione; non certifica il runtime, gli ZIP storici o l'assenza di vulnerabilità non note. I warning delle build limitate dalla rete non sono stati ignorati come se fossero un controllo riuscito.

Il progetto locale di riproduzione, ignorato da Git perché sotto `artifacts`, si trova in [Probe.csproj](artifacts/audit-2026-11-09/Probe/Probe.csproj) e [Program.cs](artifacts/audit-2026-11-09/Probe/Program.cs). Si esegue con:

```powershell
dotnet run --project artifacts\audit-2026-11-09\Probe\Probe.csproj -c Release
```

Crea un nuovo profilo esclusivamente fittizio nella propria directory `data`; non usa il database operativo. I dati di prova sono conservati localmente per rendere verificabili gli esiti. Non è una suite di regressione definitiva: le condizioni attese dovranno essere invertite/riscritte quando i difetti saranno corretti.

```text
AUTH_AFTER_DELETE: REPRODUCED
AUTH_AFTER_ARCHIVE: REPRODUCED
SESSION_STILL_ADMIN_AFTER_REVOKE: REPRODUCED
CATALOG_CHANGES_LOST: REPRODUCED
IMPORT_DUPLICATES: REPRODUCED
EXTERNAL_OPERATORS_LOST_ON_EXCHANGE: REPRODUCED
NEGATIVE_HOURS_DEPTH_ABSENT_DIVER_ACCEPTED: REPRODUCED
CSV_FORMULA_UNCHANGED: REPRODUCED
MONTHLY_SNAPSHOT_OVERWRITTEN: REPRODUCED
STALE_SAVE_LOSES_OTHER_CHANGES: REPRODUCED
EXPORT_TAMPERING_UNDETECTED: REPRODUCED
DB_PLAINTEXT_SQLITE_HEADER: REPRODUCED
RETENTION_DISCARDS_SAME_DAY_BACKUPS: REPRODUCED
BROKEN_SETTINGS_SILENTLY_DISABLE_EXTERNAL_BACKUP: REPRODUCED
```
