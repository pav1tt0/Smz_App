# Dashboard e calendario reparto - 16 novembre 2026

La dashboard presenta sei schede: Servizio giornaliero, Personale, Contabilità,
Backup e archivio, Stampe e report, Amministrazione. Con spazio sufficiente sono
disposte su due righe di tre; su finestre strette si ridispongono automaticamente.
Visite e calendario occupano rispettivamente circa il 35% e il 65% della parte
inferiore, passando in verticale sotto 900 unità di larghezza del contenuto.
Lo scorrimento resta quello dell'area centrale esistente.

Il restyling definitivo elimina il titolo "Moduli principali" e il relativo
spazio: le card iniziano a circa 18 unità dal bordo. I titoli sono a 18 px e le
descrizioni a 14 px, con icone nei contenitori azzurri da 60 px, una linea blu
laterale da 3 px e ombre leggere. Le intestazioni inferiori hanno fondo azzurro
tenue e testo scuro a 23 px. I conteggi sanitari passano a 28 px e il pulsante
"Vai all'elenco" usa lo stile blu già presente nell'app.

Il calendario ha numeri a 15 px, nomi dei giorni a 14 px, mese e anno a 22 px,
comandi più leggibili e titoli degli eventi a 15 px allineati a sinistra. Oggi
ha bordo blu, fondo tenue e un piccolo segno sotto il numero; la selezione ha
fondo blu pieno e numero bianco. Se oggi è anche selezionato il segno è bianco.
Gli indicatori degli eventi conservano i colori delle categorie e il contorno
bianco. Il pannello visite mantiene l'altezza necessaria al suo contenuto.

## Ambito del restyling definitivo

L'ultimo intervento modifica esclusivamente questi file dell'app:

- `SMZ.Conta.App/Views/HomeView.xaml`: card, tipografia e proporzioni dei pannelli.
- `SMZ.Conta.App/MainWindow.xaml`: solo lo stile `HomeModuleButtonStyle`.
- `SMZ.Conta.App/Views/CalendarioRepartoView.xaml`: aspetto delle date e degli eventi.

Sono aggiornati anche i controlli di zoom e le anteprime nel test già esistente
`SMZ.Conta.Tests/Program.Calendario.cs` e questo riepilogo. Nessuna modifica a
database, modelli, repository, calcoli, comandi, autorizzazioni o persistenza
durante il restyling definitivo. Le sezioni seguenti descrivono il calendario
realizzato in precedenza, che resta invariato nel funzionamento.

I conteggi sanitari continuano a indicare il personale con visite scadute o in
scadenza, usando gli stessi calcoli e collegamenti già presenti. Con conteggi a
zero i riquadri sono neutri. Le visite non generano eventi nel calendario.

Il calendario mensile è in italiano, inizia il lunedì e permette di selezionare
un giorno, navigare fra mesi e tornare a oggi. Gli indicatori distinguono gli
appuntamenti, le scadenze e il loro stato. Cliccando un evento si apre l'editor:
titolo, categoria, data, ora facoltativa, note e completamento delle scadenze.
L'eliminazione richiede conferma. Gli eventi passati restano disponibili.

## File e dati

- `SMZ.Conta.App/Views/HomeView.xaml`: disposizione della dashboard.
- `SMZ.Conta.App/Views/CalendarioRepartoView.xaml` e relativo codice: calendario.
- `SMZ.Conta.App/Views/EventoCalendarioWindow.xaml` e relativo codice: editor.
- `SMZ.Conta.App/ViewModels/CalendarioRepartoViewModel.cs`: selezione e navigazione.
- `SMZ.Conta.App/Models/EventoCalendario.cs`: dati e rappresentazione degli stati.
- `SMZ.Conta.App/Data/PersonaleRepository.Calendario.cs`: query parametrizzate.
- `SMZ.Conta.App/Data/DatabaseInitializer.cs`: tabella `EventiCalendario` e indice
  sulla data, creati con `IF NOT EXISTS` nella transazione già usata dall'app.
- `SMZ.Conta.Tests/Program.Calendario.cs` e registrazione in `Program.cs`: verifiche.

Gli eventi usano lo stesso database SQLite dell'applicazione. Non vengono
aggiunte dipendenze o connessioni a servizi esterni. Il backup esistente contiene
l'intero database e include quindi anche gli eventi senza cambiare formato o
percorsi. I backup precedenti al calendario restano ripristinabili; al successivo
avvio la tabella viene aggiunta. Le prove usano esclusivamente database temporanei.

La Home continua a essere riservata agli amministratori; il profilo Base resta
limitato alla sezione Personale. La sesta card usa la destinazione 6 già presente.
Menu laterale, barre superiore e inferiore, zoom e moduli operativi non cambiano.

## Verifiche

Comando: `dotnet run --project SMZ.Conta.Tests --no-restore -v quiet`.

La suite comprende 47 controlli, inclusi quattro gruppi dedicati al calendario:
creazione, modifica, cancellazione, validazione e riapertura delle connessioni;
migrazione di un database senza calendario e ripetizione dell'inizializzazione;
backup/ripristino sia con eventi sia precedente alla migrazione; ordinamento,
anni bisestili, cambio anno, stati e conservazione dei dati visualizzati in caso
di lettura fallita. L'app viene aperta su un database temporaneo per controllare
binding, destinazioni delle sei card e calendario caricato. Sono verificati
ridisposizione e limiti dei pulsanti a larghezze da 360 a 1380, con zoom 70%,
80%, 90%, 100% e 150%, oltre al salvataggio dalla finestra evento e ai permessi Base.

Anteprime renderizzate in `artifacts/dashboard-calendario/`:

- `dashboard.png` e `dashboard-100.png`: dashboard al 100%.
- `dashboard-80.png` e `dashboard-90.png`: confronto agli zoom richiesti.
- `dashboard-selezione.png`: oggi e un'altra data selezionata, visibili insieme.
- `dashboard-eventi.png`: aspetto degli eventi, con dati del database temporaneo.

Restano da eseguire il collaudo manuale degli operatori e della conferma grafica
di eliminazione. Non è stato generato un nuovo pacchetto di distribuzione.
L'avviso NU1900 riguarda l'accesso non disponibile al servizio online di verifica
delle vulnerabilità NuGet; non impedisce compilazione e test e non rappresenta
una verifica delle vulnerabilità completata.
