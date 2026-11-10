# SMZ aggiornato per il PC del nucleo - 10 novembre 2026

Pacchetto operativo Release, Windows x64, con runtime incluso:
`dist/SMZ-Installazione-Dati-Reali-win-x64-2026-11-10-Aggiornato.zip`.
La distribuzione di stamani resta disponibile con il suo nome originale.

## Installazione conservando il database

1. Sul PC del nucleo chiudere tutte le copie di SMZ.
2. Estrarre l'intero ZIP in una cartella locale.
3. Avviare `INSTALLA-E-AVVIA-SMZ.cmd`.
4. Alla proposta del database gia in uso scegliere **SI**.
5. Accedere con gli stessi PerID e password.

L'installatore riutilizza il database configurato o quello standard in
`%LOCALAPPDATA%\SMZ\Conta\smz-conta.db`, creando prima una copia SQLite
consistente in `Backups\Prima installazione`. Conserva account e dati.
Se il database si trova altrove, selezionare il file reale quando richiesto:
viene importata una copia senza sostituire un altro database gia presente.
Il percorso dei dati e del backup compare al termine dell'installazione.
Non sono inclusi database, anagrafiche di prova o dati iniziali.
Se il database configurato manca, il programma si ferma senza crearne uno vuoto.

Il programma esegue al primo avvio le normali migrazioni della nuova versione.
Il catalogo tariffario viene aggiornato solo se corrisponde integralmente
al catalogo originale precedente; i cataloghi personalizzati vengono conservati.

## Modifiche incluse

- Personale ordinato per qualifica e selezione su due colonne in ordine verticale.
- Pagina servizio compatta, due natanti affiancati e salvataggio in fondo pagina.
- Scelta dell'immersione per gli operatori e numeri automatici degli incarichi
  assegnati dai menu superiori, con descrizioni complete nella colonna Ruolo.
- Calcolo contabile senza applicare nuovamente i coefficienti alle tariffe finali
  delle immersioni addestrative, in camera iperbarica e sperimentali.

Le chiusure mensili gia salvate restano conservate. Per applicare la correzione
dei calcoli a un mese gia chiuso usare **Rigenera chiusura** nella contabilita.

## Verifiche e integrita

Pubblicazione completata; 34 test applicativi Release e 14 controlli del pacchetto
superati. I controlli coprono conservazione delle righe, account e cataloghi,
reinstallazione, backup automatico, percorsi con spazi, collegamenti, importazione
di database e `.smzbak` con Export, rifiuto di file errati e pacchetti alterati.
Il collaudo usa soltanto dati simulati separati sotto `.codex-build`.
Il database del PC del nucleo non e stato usato per questi controlli.

Report del pacchetto:
`.codex-build/collaudo-operativo-393aef44fa1d4d92b087f0e34794b5dd/esito-collaudo.json`.

SHA-256 dello ZIP:
`765E8FD22BA8177AFCE3176E7E643F1ABEC1C05704F8924CA51614BC5A5654F5`.
Lo stesso valore si trova nel file `.zip.sha256.txt` accanto allo ZIP.

Avviso NU1900 durante pubblicazione e collaudo: servizio online NuGet per il
controllo delle vulnerabilita non raggiungibile. Pubblicazione e test sono
riusciti; il controllo online delle vulnerabilita non e stato completato.

Comandi riproducibili:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Publish-Operativo.ps1 -PackageName SMZ-Installazione-Dati-Reali-win-x64-2026-11-10-Aggiornato
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\Test-Pacchetto-Operativo.ps1 -ZipPath dist\SMZ-Installazione-Dati-Reali-win-x64-2026-11-10-Aggiornato.zip
```

Il comando di pubblicazione rifiuta di sovrascrivere un pacchetto esistente:
per una nuova compilazione scegliere un nome nuovo.
