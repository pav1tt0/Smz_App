# Note prossime modifiche SMZ

## Chiusura serale del 10 novembre 2026: distribuzione aggiornata per il nucleo

Completate le successive modifiche richieste a ordinamento del personale, pagina
servizio compatta con due natanti, salvataggio in fondo, scelta e numerazione
immersioni nel riepilogo e descrizioni dei ruoli. Corretto il doppio coefficiente
in contabilita per tariffe finali ADD, CI e SPER; cataloghi personalizzati conservati.
Per aggiornare mesi gia chiusi occorre **Rigenera chiusura**.

Distribuzione corrente: `dist/SMZ-Installazione-Dati-Reali-win-x64-2026-11-10-Aggiornato.zip`.
Riutilizza il database del PC del nucleo con backup automatico e conserva account
e password. Superati 34 test applicativi Release e 14 controlli del pacchetto.
L'utente ha ringraziato e chiesto di salvare lo stato; non e una verifica diretta
dell'installazione sul PC destinatario.

Ripartire da [STATO_LAVORI_SMZ_2026-11-10.md](STATO_LAVORI_SMZ_2026-11-10.md)
e [DISTRIBUZIONE_NUCLEO_AGGIORNATA_2026-11-10.md](DISTRIBUZIONE_NUCLEO_AGGIORNATA_2026-11-10.md).
Il pacchetto del mattino resta conservato come storico.

## Storico del mattino: funzionamento confermato

Completati controllo tabelle, interfaccia responsive, rimozione alert bimestrali, blocco abilitazioni duplicate e distribuzione installabile con database reale. L'utente ha confermato che funziona e ha chiesto di registrare tutto prima di chiudere.

Prima distribuzione: `dist/SMZ-Installazione-Dati-Reali-win-x64-2026-11-10.zip`; nessun dato fittizio incluso. Per lo stato e il pacchetto correnti vedere la chiusura serale sopra.

## Ripresa del 10 novembre 2026 con il collega

L'utente e il collega che utilizzerà SMZ decideranno gli interventi nel confronto del 10 novembre. Il programma è amatoriale e girerà in locale su notebook: dare priorità a contabilità, stampe e recupero dati, senza introdurre sistemi di sicurezza complessi non necessari.

Piano ridotto, decisioni aperte e stato verificato sono salvati in [PROMEMORIA_CONFRONTO_COLLEGA_2026-11-10.md](PROMEMORIA_CONFRONTO_COLLEGA_2026-11-10.md). Questo promemoria aggiorna l'ordine operativo proposto nell'audit del 9 novembre. Nessuna correzione da avviare prima delle decisioni con il collega. Mantenere l'avvio di sviluppo senza login, richiesto dall'utente.

## SMZ esterni / occasionali

Decisione da riprendere alla prossima sessione.

- Non inserirli nell'anagrafica principale.
- Gestirli dentro il servizio giornaliero in una sezione dedicata, simile all'assistenza occasionale.
- Nome sezione suggerito: `SMZ esterni / occasionali`.
- Campi previsti:
  - qualifica
  - cognome e nome
  - reparto / sede di appartenenza
  - presente
  - apparato
  - profondita
  - fascia
  - ore immersione
  - categoria contabile
  - note
- Devono comparire dopo il personale interno.
- Non devono entrare nello scadenziario.
- Non devono sporcare l'anagrafica stabile.
- Da decidere prima dell'implementazione: se devono generare importi nella contabilita mensile oppure solo comparire su foglio servizio e registro immersioni.

Ragionamento: sono visite saltuarie di sommozzatori della Polizia in forza ad altri reparti; serve contabilizzare o almeno registrare le immersioni senza trasformarli in personale interno stabile.

## Distribuzione finale del programma

Scelta realizzata il 10 novembre 2026: ZIP operativo con eseguibile self-contained Windows x64, installatore per utente e selezione del database reale. Vedere [DISTRIBUZIONE_DATI_REALI_2026-11-10.md](DISTRIBUZIONE_DATI_REALI_2026-11-10.md).

Le opzioni seguenti sono il promemoria storico precedente alla consegna; non sostituiscono il pacchetto operativo indicato sopra.

- Opzione leggera: cartella con circa 25-40 file, 35-50 MB, richiede .NET Desktop Runtime 8 gia installato sul PC.
- Opzione completa consigliata: cartella self-contained win-x64, circa 80-150 file, 120-180 MB, non richiede installazioni .NET separate sui PC destinatari.
- Opzione file unico: un eseguibile principale da circa 90-160 MB, piu eventuali cartelle dati/backup/export create dall'app.
- Scelta consigliata per uso reale: pacchetto ZIP con cartella completa self-contained win-x64, piu affidabile su PC diversi e piu semplice da diagnosticare rispetto al file unico.
