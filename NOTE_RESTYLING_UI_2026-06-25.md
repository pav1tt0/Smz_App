# Note restyling UI - 2026-06-25

Obiettivo discusso: modernizzare l'interfaccia WPF senza cambiare logica, database o flussi applicativi.

## Direzione generale

- Stile istituzionale moderno, non marketing.
- Palette: navy scuro per sidebar/header, bianco/off-white per contenuti, blu operativo per azioni, rosso/ambra solo per scadenze e criticita.
- Card pulite e compatte, con raggio indicativo 8 px.
- Layout piu gestionale: leggibile, ordinato, denso ma non affollato.
- Usare stemmi e loghi reali dagli asset del progetto, non immagini generate.
- Testi istituzionali come TextBlock reali, non parte delle immagini.

## Branding concordato

- Titolo principale applicazione/header: `Nucleo Sommozzatori`.
- Sidebar in alto:
  - `POLIZIA DI STATO`
  - `Centro Nautico e SMZ`
- Evitare `SMZ Conta` come titolo visibile principale.
- Evitare la scritta `Centro Nautico e Sommozzatori SMZ`.
- Dove compare il logo/stemma, non mettere testo aggiuntivo che lo affianchi inutilmente.

## Dashboard/Home

Riferimento visivo preferito: la prima immagine fornita dall'utente, `C:\Users\LENOVO\Downloads\Generated image 1.png`.

Da mantenere:

- Sidebar laterale scura.
- Header superiore scuro.
- Riga `Moduli principali` in alto.
- Card modulo:
  - `Servizio giornaliero`
  - `Personale` / `Anagrafica personale`
  - `Backup e archivio`
  - `Contabilita`
  - `Stampe e report`
  - eventuale `Impostazioni operative`

Da togliere perche inventato/non necessario:

- `Servizio di oggi` con missioni/operatori.
- `Personale in servizio` con reperibilita.
- `Dotazioni principali`.
- `Contabilita - Situazione` con budget.
- Grafici, calendari, meteo, mappe, task feed, KPI inventati.

Struttura dashboard preferita:

- `Stato nucleo`, compatto.
- Una sezione unica `Scadenze personale`, evitando ridondanze.
- Dentro `Scadenze personale`:
  - badge/contatori compatti: `3 Scadute`, `4 In scadenza`
  - lista unica con righe miste scadute/in scadenza.
- `Criticita personale` come sezione separata.

Motivo: evitare la ripetizione `Scadenze visite mediche` + `Visite scadute` + elenco nomi.

## Landing/Welcome

Va rivista nello stesso stile, ma deve restare piu minimale della dashboard.

Direzione:

- Schermata istituzionale pulita.
- Titolo: `Nucleo Sommozzatori`.
- Sottotitolo/branding:
  - `POLIZIA DI STATO`
  - `Centro Nautico e SMZ`
- Logo SMZ reale, grande ma pulito.
- Sfondo subacqueo piu sobrio/scuro.
- Pulsante principale semplice: `Entra` o `Entra nel gestionale`.
- Evitare troppe card informative: la landing non deve diventare una seconda dashboard.

## File interessati quando si procedera

- `SMZ.Conta.App\Views\WelcomeView.xaml`
- `SMZ.Conta.App\Views\HomeView.xaml`
- stili condivisi in `SMZ.Conta.App\MainWindow.xaml` o, meglio, in futuro in ResourceDictionary dedicato.

Intervento previsto: XAML/stili soltanto, salvo piccole correzioni di binding se necessarie.

## Asset utili gia presenti

- `SMZ.Conta.App\Assets\logo_smz.png`
- `SMZ.Conta.App\Assets\smz.jpg`
- `file per programma\Stemmi Vettoriali\scudo.jpg`
- `file per programma\Stemmi Vettoriali\logo sommozzatori POLIZIA DI STATO.jpg`
- `file per programma\Stemmi Vettoriali\vettoriale SMZ 1.png`
- `file per programma\Stemmi Vettoriali\vettoriale SMZ 2.png`
- `file per programma\Stemmi Vettoriali\vettoriale SMZ 3.png`

## Immagini/mockup

- Riferimento indicato dall'utente:
  - `C:\Users\LENOVO\Downloads\Generated image 1.png`
- Immagini generate nella sessione:
  - `C:\Users\LENOVO\.codex\generated_images\019efea8-00b1-7060-af0a-54bbfd0fdff5`

Nota: le immagini in `.codex\generated_images` sono utili come riferimento, ma per implementare la UI reale contano soprattutto questa nota e l'immagine preferita salvata in Download.

## Stima tempi discussa

- Prima bozza funzionante: 2-3 ore.
- Rifinitura testi, spaziature, stemmi, hover/selected: 1-2 ore.
- Controllo finale nell'app reale: circa 1 ora.
- Totale realistico: mezza giornata.
