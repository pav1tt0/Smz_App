# Test operatore SMZ

Output pronto per il test:

- cartella: `dist\SMZ.Conta.App-win-x64-test`
- eseguibile: `dist\SMZ.Conta.App-win-x64-test\SMZ.Conta.App.exe`

Caratteristiche del pacchetto:

- eseguibile Windows `win-x64`
- self-contained, quindi non richiede installazione manuale del runtime .NET
- database locale creato automaticamente al primo avvio

Dove salva i dati:

- `%LOCALAPPDATA%\SMZ\Conta\smz-conta.db`
- backup: `%LOCALAPPDATA%\SMZ\Conta\Backups`
- export: `%LOCALAPPDATA%\SMZ\Conta\Export`

Come consegnarlo all'operatore:

1. Passare l'intera cartella `dist\SMZ.Conta.App-win-x64-test`
2. Oppure creare e passare il file zip generato dallo script di pubblicazione
3. Avvio tramite doppio click su `SMZ.Conta.App.exe`

Per rigenerare il pacchetto:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-Test-Operatore.ps1
```

Opzione utile:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-Test-Operatore.ps1 -SkipZip
```
