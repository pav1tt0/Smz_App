# Distribuzione operativa SMZ - 16 novembre 2026

Pacchetto: [SMZ-Installazione-Dati-Reali-win-x64-2026-11-16.zip](dist/SMZ-Installazione-Dati-Reali-win-x64-2026-11-16.zip).

- Modalità: operativa, Release, Windows x64, runtime incluso.
- Dimensione ZIP: 68.698.671 byte.
- SHA-256: `A33F5233236D5F6089A1346A0C959368FF196A9023E385E3F727743E0CFF3992`.
- Nessun database o dato dimostrativo incluso; database esistente o selezionato obbligatorio.
- Autori: Paolo Vittori, Cesare Scapazzoni e Codex. Testo adattato per andare a capo sulla landing.
- Incluse le modifiche a filtri Servizi/Stampe, registro A4 e ordinamento per qualifica/decorrenza, responsabile nei fumetti e predisposizione del libretto.
- Dashboard mantenuta nella disposizione attuale, con Criticità operative già rimosse; le proposte grafiche non sono incluse.

## Aggiornare l'altro PC

1. Chiudere tutte le copie di SMZ sul PC da aggiornare.
2. Copiare lo ZIP ed estrarlo interamente in una cartella locale.
3. Eseguire `INSTALLA-E-AVVIA-SMZ.cmd`.
4. Alla proposta del database già in uso, scegliere **SÌ** per mantenerlo.
5. Usare lo stesso PerID e la stessa password già presenti nel database.

L'installatore conserva il database configurato, crea una copia di sicurezza prima dell'avvio e aggiorna il programma. Se il database configurato non è disponibile, l'avvio si ferma e non passa a dati vuoti o dimostrativi. La posizione dei dati e del backup è riportata alla fine dell'installazione e nel file `installazione.json` della cartella installata.

## Verifiche effettuate

- Compilazione Debug riuscita e 43 test applicativi superati su dati isolati.
- Pubblicazione Release autonoma win-x64 riuscita.
- Presenza di Cesare Scapazzoni verificata anche nel programma pubblicato.
- 14 verifiche della distribuzione operativa superate: assenza di DB, riutilizzo del DB preesistente, percorsi con spazi, importazione DB, conservazione account/cataloghi, sorgente intatta, backup prima dell'avvio, collegamenti desktop/Start, reinstallazione senza modificare il DB, selezione di altri dati senza sovrascrivere quelli precedenti, ripristino smzbak con Export, rifiuto DB non validi, rifiuto pacchetti alterati e obbligo del database selezionato.

Esito del pacchetto: [.codex-build/collaudo-operativo-4b831714d9fb4f7ba1abcfd93e36b41d/esito-collaudo.json](.codex-build/collaudo-operativo-4b831714d9fb4f7ba1abcfd93e36b41d/esito-collaudo.json).

Log dei test: [artifacts/verifica-2026-11-16/collaudo-finale-debug.txt](artifacts/verifica-2026-11-16/collaudo-finale-debug.txt).

L'avviso NU1900 indica che il servizio online NuGet per il controllo delle vulnerabilità non era raggiungibile. Non è un errore di compilazione e non costituisce un controllo delle vulnerabilità completato.

Le verifiche di installazione sono state eseguite su cartelle e database temporanei nel workspace; l'altro PC non è stato ispezionato direttamente.
