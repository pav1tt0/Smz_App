# Installazione SMZ con dati reali - 10 novembre 2026

Pacchetto: `dist/SMZ-Installazione-Dati-Reali-win-x64-2026-11-10.zip`.

Conferma ricevuta dall'utente il 10 novembre 2026 dopo la consegna: «ok funziona». Stato completo alla chiusura: [STATO_LAVORI_SMZ_2026-11-10.md](STATO_LAVORI_SMZ_2026-11-10.md).

Estrarre tutto lo ZIP e avviare `INSTALLA-E-AVVIA-SMZ.cmd`. L'installazione e per l'utente Windows corrente, non richiede privilegi amministrativi e include il runtime Windows x64. Il programma viene installato in `%LOCALAPPDATA%\Programs\SMZ`, con collegamenti sul desktop e nel menu Start.

## Scelta dei dati

- Se esiste un'installazione operativa precedente, viene proposto il database gia configurato; altrimenti si cerca `%LOCALAPPDATA%\SMZ\Conta\smz-conta.db`.
- Scegliendo SI si riutilizza il database al suo posto, creando prima una copia consistente SQLite in `Backups\Prima installazione`.
- Scegliendo NO, o se non esiste un database riconosciuto, si seleziona un `.db`, `.sqlite`, `.sqlite3` o `.smzbak` gia salvato sul PC o su un supporto.
- Un file selezionato altrove viene copiato in un profilo locale. Il sorgente resta intatto. Se la cartella ordinaria contiene gia dati, si usa un nuovo profilo `Operativo-*`, senza sostituire il database precedente.
- Sono conservate tutte le righe del database, compresi account, credenziali e cataloghi. Un `.smzbak` ripristina anche gli Export inclusi; selezionando un singolo database si trasferisce solo il database.

Prima del primo avvio e disponibile una copia di sicurezza dei dati selezionati. Il programma esegue poi le normali inizializzazioni e migrazioni della versione installata. L'installatore non inizializza, ripopola o cancella le tabelle del database.

Il pacchetto non include database, dati iniziali o anagrafiche fittizie. Il file `installazione.json` ricorda la cartella effettiva. Anche l'eseguibile avviato direttamente usa questa configurazione. Un database configurato mancante produce un errore, senza creare un database alternativo; l'eseguibile distribuito richiede i dati selezionati dall'installatore.

## Verifiche

23 test applicativi superati, inclusi copia integrale delle righe, account, riutilizzo senza modifiche, rifiuto di destinazioni occupate o file non validi, importazione `.smzbak` con Export, dati confermati nel WAL e mancato ripiego su un database vuoto.

Il collaudo del pacchetto usa esclusivamente fixture separate nella cartella `.codex-build`; questi dati non vengono inclusi nella distribuzione. Verifica l'eseguibile self-contained tramite la sua modalita di preparazione dati, l'installazione in percorsi con spazi, i collegamenti, la conservazione del sorgente, i dati precedenti, la reinstallazione, gli account, gli Export e il rifiuto di pacchetti alterati. Non e stata eseguita un'installazione sul database reale del destinatario, che si trova sull'altro PC.

Comandi riproducibili:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Publish-Operativo.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-Pacchetto-Operativo.ps1 -ZipPath dist\SMZ-Installazione-Dati-Reali-win-x64-2026-11-10.zip
```

Il file `.zip.sha256.txt` contiene l'hash della distribuzione. Il manifest interno verifica gli hash dei file prima dell'installazione. Sono rimasti gli avvisi NU1900: la verifica online NuGet delle vulnerabilita non era disponibile nella rete di lavoro.
