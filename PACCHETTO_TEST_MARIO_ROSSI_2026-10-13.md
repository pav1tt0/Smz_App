# Pacchetto SMZ di prova - Mario Rossi 12345

Pacchetto pronto in `dist/SMZ-Test-MarioRossi-12345-2026-10-13.zip` (72.700.790 byte).
SHA-256: `46883F38828DF56C01D2D22143583F759391BCEE457A01032D26CC65B1A91588`.

Il pacchetto contiene la build Release Windows x64 con runtime .NET incluso, le visite facoltative e la visita D.Lgs. 81/08 a 12 mesi. Estrarre tutto lo ZIP e avviare `INSTALLA-E-AVVIA-SMZ-TEST.cmd`; istruzioni complete in `LEGGIMI-PRIMA.txt`.

Il database nasce da zero tramite `scripts/SMZ.TestPackage`, senza leggere database operativi o backup precedenti. Contiene un solo dipendente: Mario Rossi, PerID 12345, profilo Operatore Subacqueo, stato Attivo. `TEST12345` e un segnaposto fittizio nel campo codice fiscale obbligatorio. Gli altri campi anagrafici sono vuoti.

Tutte le altre tabelle operative sono vuote, inclusi visite, abilitazioni personali, attagliamento, archivi, servizi, immersioni, elaborazioni contabili e utenti di accesso. Restano gli 11 cataloghi standard generati dall'app, necessari a menu e calcoli. Il dettaglio dei conteggi e incluso nello ZIP in `VERIFICA-DATABASE.txt`.

Al primo accesso si inserisce PerID 12345 e si sceglie una password di almeno 10 caratteri: l'app crea l'amministratore. Nessuna password o account e preimpostato nel pacchetto.

Programma installato in `%LOCALAPPDATA%\Programs\SMZ-Test-MarioRossi-12345`.
Dati in `%LOCALAPPDATA%\SMZ\Test-MarioRossi-12345`.
L'installatore crea collegamenti dedicati su desktop e menu Start. La reinstallazione conserva il database di questa prova. Il database ordinario `%LOCALAPPDATA%\SMZ\Conta` resta separato.

La proprieta MSBuild `SmzDataProfile` inserisce `SMZ.DataProfile` nella configurazione runtime del pacchetto. Anche un avvio diretto dell'eseguibile pubblicato usa il profilo dati di test. Le normali compilazioni senza questa proprieta mantengono il percorso `Conta`. L'override `SMZ_CONTA_APPDATA` rimane disponibile per test isolati; il launcher installato lo imposta sulla cartella della prova.

Verifiche completate:

- Build Release senza errori o avvisi e 11 test automatici superati.
- Integrita SQLite e riferimenti verificati, controllo di tutte le tabelle del database iniziale.
- Estrazione dello ZIP e installazione in un percorso contenente spazi.
- Creazione e verifica dei collegamenti desktop e menu Start in cartelle di collaudo isolate.
- Reinstallazione: database modificato di prova conservato byte per byte.
- Database ordinario simulato conservato byte per byte.
- Profilo dati di test verificato nell'eseguibile pubblicato.
- Eseguibile dello ZIP avviato: schermata iniziale, prima configurazione dell'amministratore 12345 e finestra principale con `Rossi Mario / PerID 12345` verificati tramite UI Automation.
- Il primo accesso e stato provato su una copia del database; il database distribuito resta privo di account.

Risultati locali del collaudo: `.codex-build/collaudo-pacchetto-0533d0e8a5ee4f9182c43bb910aba597/esito-collaudo.json` ed `esito-avvio.json`.

Per rigenerare, usare un nuovo nome per conservare il pacchetto gia consegnato:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Publish-Test-Pulito.ps1 -PackageName SMZ-Test-MarioRossi-12345-NUOVA-VERSIONE
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-Pacchetto-Pulito.ps1 -ZipPath dist\SMZ-Test-MarioRossi-12345-NUOVA-VERSIONE.zip
```

La generazione richiede l'SDK .NET sul PC di sviluppo; il collaudo del pacchetto usa anche Python. Il PC destinatario non richiede questi strumenti.
