# Stato alla chiusura - 10 novembre 2026

## Aggiornamento serale: servizio, contabilita e distribuzione per il nucleo

Questo e lo stato corrente; le sezioni del mattino piu sotto restano come storico.
L'utente ha ricevuto il nuovo ZIP e ha chiesto di registrare tutto prima di chiudere.
Non e stata verificata direttamente l'installazione sul PC del nucleo.

### Modifiche completate dopo la prima distribuzione

- Elenchi del personale ordinati per qualifica nelle viste, nei servizi, nella
  contabilita e nelle stampe; anzianita e nominativo risolvono le parita disponibili.
  Corretto il calcolo della chiave di ordinamento per le date mancanti.
- Personale su due colonne in ordine verticale: prima tutta la prima colonna,
  poi la seconda (`Infrastructure/ColumnFirstUniformGrid.cs`).
- Pagina nuovo servizio compatta: straordinario, fuori sede e OP sulla stessa riga;
  localita e due selettori natanti uguali sulla stessa riga; orari in deroga accanto
  all'orario di servizio. Pulsante Salva anche in fondo pagina.
- La maschera espone due natanti, come richiesto infine dall'utente. La persistenza,
  le stampe e lo scambio mantengono il supporto a tre per conservare dati precedenti.
- Nel riepilogo contabile del servizio, gli operatori senza incarico possono
  scegliere l'immersione; scelta conservata al salvataggio e alla riapertura.
- Colonna Immersione con numeri automatici per direttore, soccorso e assistenti
  assegnati dai menu superiori; colonna Ruolo con sole descrizioni complete:
  Direttore immersione, Operatore soccorso, Assistenza BLSD, Assistenza sanitaria.
  Numeri e ruoli multipli sono allineati per riga e si aggiornano dai menu.
- Inclusa nel riepilogo l'assistenza sanitaria senza generare contabilita immersioni.
  Gestito il caso misto incarico in una immersione e operatore subacqueo nell'altra.

### Correzione calcoli immersioni

Il riepilogo usava tariffe finali, mentre la contabilita mensile riapplicava
ADD /2, CI *0,8 e SPER *1,25. Rimossi i coefficienti ripetuti sia per interni
sia per esterni: importo = tariffa finale * ore. Non cambiati ore o arrotondamenti.
Esempio ARA 0-12: ORD 5, ADD 2,50, SPER 6,25; CI 4/8/16/22,4 per fascia.

Aggiornati i valori iniziali del catalogo. La migrazione modifica soltanto l'intero
catalogo originale precedente di 34 regole, verificandone tutti i valori e attributi;
qualsiasi personalizzazione conserva l'intero catalogo. Migrazione idempotente.
Il database operativo locale e stato soltanto letto per la diagnosi.

Le chiusure mensili gia salvate restano conservate. Per applicare la correzione
a mesi gia chiusi usare **Rigenera chiusura**. Dettagli:
[VERIFICA_CALCOLI_IMMERSIONI_2026-11-10.md](VERIFICA_CALCOLI_IMMERSIONI_2026-11-10.md).

### Distribuzione corrente per il nucleo

`dist/SMZ-Installazione-Dati-Reali-win-x64-2026-11-10-Aggiornato.zip`
(72.725.194 byte), SHA-256:
`765E8FD22BA8177AFCE3176E7E643F1ABEC1C05704F8924CA51614BC5A5654F5`.

Release self-contained Windows x64, nessun database incluso. Chiudere SMZ,
estrarre tutto e avviare `INSTALLA-E-AVVIA-SMZ.cmd`; scegliere SI per riutilizzare
il database gia configurato. Backup SQLite consistente prima dell'avvio,
account e password conservati, nessun ripiego su database vuoto.
La distribuzione di stamani resta intatta con il nome precedente.

Istruzioni, hash e comandi:
[DISTRIBUZIONE_NUCLEO_AGGIORNATA_2026-11-10.md](DISTRIBUZIONE_NUCLEO_AGGIORNATA_2026-11-10.md).

### Verifiche e precauzioni alla ripresa

- Build Debug e Release riuscite; 34 test applicativi superati in entrambe.
- 14 controlli del nuovo pacchetto operativo superati, con dati simulati isolati.
- Report: `.codex-build/collaudo-operativo-393aef44fa1d4d92b087f0e34794b5dd/esito-collaudo.json`.
- Screenshot della pagina compatta: `artifacts/servizio-compatto-2026-11-10/`.
- NU1900: servizio online NuGet non raggiungibile; non attestato audit online.
- Durante precedenti prove UI alcuni programmi di collaudo con eccezioni non
  gestite hanno prodotto popup Windows. Usare prove con gestione delle eccezioni
  e chiusura dei processi; non lasciare helper in esecuzione.
- Restano separate le decisioni dell'audit rinviate al collega. Le correzioni
  esplicitamente richieste e descritte qui sono completate.

## Storico: chiusura del mattino e prima distribuzione

L'utente ha confermato: «ok funziona, segna tutto che chiudo», dopo la consegna della distribuzione installabile con dati reali. Conferma ricevuta; non equivale a un'ispezione diretta del database sul PC destinatario.

## Lavoro completato

- Riviste tutte le 22 tabelle: larghezze minime esplicite, intestazioni su piu righe e scorrimento orizzontale. Corretto anche il riepilogo abilitazioni. Dettagli in [VERIFICA_TABELLE_E_DASHBOARD_2026-11-10.md](VERIFICA_TABELLE_E_DASHBOARD_2026-11-10.md).
- Tolti gli avvisi delle visite bimestrali dalla dashboard, incluse le varianti storiche del nome; dati storici conservati. Visite sempre informative e non bloccanti; D.Lgs. 81/08 resta a 12 mesi.
- Interfaccia adattabile alle dimensioni disponibili e al DPI del monitor: campi e pulsanti a capo, navigazione compatta, finestre e dialoghi scorrevoli, zoom 70-150% memorizzato per profilo. Comandi A-/A+ e Ctrl +/-/0. Dettagli in [RESPONSIVE_E_ABILITAZIONI_UNICHE_2026-11-10.md](RESPONSIVE_E_ABILITAZIONI_UNICHE_2026-11-10.md).
- Impedita la stessa abilitazione piu volte per la stessa persona, sia nell'editor sia al salvataggio nel repository. Modifica della riga esistente consentita; nessuna cancellazione automatica dei duplicati storici.
- Preparato e collaudato l'installatore operativo senza database fittizi, con riutilizzo o importazione del database reale gia presente sul PC.

## Distribuzione consegnata

File: `dist/SMZ-Installazione-Dati-Reali-win-x64-2026-11-10.zip` (72.708.561 byte).

SHA256 verificato alla chiusura: `4A9451588C96C57A0B0A7D1C2BCAA49FB850A4D99AADECD14C47998D19764893`.

Estrarre tutto e avviare `INSTALLA-E-AVVIA-SMZ.cmd`. Release Windows x64 con runtime incluso, installazione per utente senza amministratore in `%LOCALAPPDATA%\Programs\SMZ`, collegamenti desktop e Start.

L'installatore propone il database operativo gia configurato oppure quello ordinario in `%LOCALAPPDATA%\SMZ\Conta`; in alternativa accetta `.db`, `.sqlite`, `.sqlite3` e `.smzbak`. Crea una copia SQLite consistente prima dell'avvio. Un'importazione conserva il sorgente; se la destinazione ordinaria e occupata, crea un profilo separato. Conserva account, credenziali e cataloghi; da `.smzbak` recupera anche gli Export inclusi. Le normali migrazioni applicative avvengono al successivo avvio.

`installazione.json` registra la cartella selezionata. L'eseguibile operativo richiede quel database e segnala un errore se manca, senza ripiegare su dati vuoti o dimostrativi. Il vecchio pacchetto Test-MarioRossi e il precedente ZIP Responsive non sono la distribuzione da usare per questa installazione reale.

Istruzioni complete e comandi riproducibili: [DISTRIBUZIONE_DATI_REALI_2026-11-10.md](DISTRIBUZIONE_DATI_REALI_2026-11-10.md) e [PROCEDURA_BACKUP_E_CAMBIO_PC_SMZ.md](PROCEDURA_BACKUP_E_CAMBIO_PC_SMZ.md).

## Verifiche registrate

- 23 test applicativi Release superati e build Debug senza errori. Conservato l'avvio Debug senza login; distribuzione operativa Release con autenticazione.
- 14 controlli del pacchetto superati, compresi sorgente invariato, tutte le righe/account/cataloghi, backup, WAL confermato, reinstallazione, percorsi con spazi, collegamenti, `.smzbak` con Export e rifiuto di dati/pacchetti non validi.
- 66 configurazioni di tabelle e 273 configurazioni di finestre/viste simulate verificate, oltre a quattro dialoghi piccoli. Rilevato contesto PerMonitorV2; non sono prove fisiche su tutti i monitor destinatari.
- Evidenze: `artifacts/table-audit/`, `artifacts/responsive-audit/` e `.codex-build/collaudo-operativo-0f5f8ee0851f44d2bc6d86d0be95ccb9/esito-collaudo.json`.
- Avvisi NU1900 dovuti al servizio online NuGet non raggiungibile: nessuna attestazione di audit vulnerabilita online.

## Ripresa e decisioni ancora aperte

Il lavoro richiesto sopra e completato e l'utente ne ha confermato il funzionamento. Le modifiche sono salvate nei file locali, senza commit Git. Non eliminare i pacchetti o le evidenze per pulizia automatica.

Restano separati gli argomenti gia rimandati al confronto con il collega: esempi contabili approvati, aspettative su stampe/recupero dati/import-export e gestione degli SMZ esterni o occasionali. Vedere [NOTE_PROSSIME_MODIFICHE.md](NOTE_PROSSIME_MODIFICHE.md) e [PROMEMORIA_CONFRONTO_COLLEGA_2026-11-10.md](PROMEMORIA_CONFRONTO_COLLEGA_2026-11-10.md). Non cambiare tariffe o arrotondamenti senza i casi concordati. Le richieste esplicite di questa sessione sono gia state eseguite e non sono bloccate dal rinvio dell'audit.

Per ricostruire: usare `scripts/Publish-Operativo.ps1` e `scripts/Test-Pacchetto-Operativo.ps1`. Evitare build contemporanee della stessa applicazione/configurazione WPF: condividono la cartella `obj`.
