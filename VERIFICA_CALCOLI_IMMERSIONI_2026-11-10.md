# Verifica calcoli immersioni - 10 novembre 2026

Ricerca richiesta per Camera Iperbarica e immersioni addestrative, seguita dalla correzione autorizzata dall'utente. Le tabelle della verifica riportano il comportamento precedente alla correzione.

## Regole individuate

- Addestramento del personale brevettato e corsi di perfezionamento/specializzazione: riduzione del 50% dell'indennità, quindi tariffa base × ore contabili × 0,50.
- Camere di decompressione e impianti iperbarici a terra: riduzione del 20%, quindi tariffa base applicabile × ore contabili × 0,80. Il 20% è la riduzione; l'80% è la quota corrisposta.
- Sperimentazione/collaudo di nuove apparecchiature presso reparti autorizzati: maggiorazione del 25%.
- La tariffa base dipende dall'apparecchiatura e dalla fascia di profondità massima. La categoria Camera Iperbarica, da sola, non identifica una tariffa unica: va verificata la classificazione dell'impianto e del gas prevista per il caso effettivo.
- I tempi contabili seguono regole specifiche: per immersioni non in saturazione, minimo di 30 minuti per la prima immersione se più breve e arrotondamento giornaliero dei restanti tempi a quarti d'ora. L'app attualmente usa le ore inserite dall'utente; prima di automatizzare occorre distinguere tempi effettivi e ore già contabilizzate.

Fonti consultate:

1. [D.P.R. 146/1975, tabella C e note, Gazzetta Ufficiale](https://www.gazzettaufficiale.it/atto/serie_generale/caricaArticolo?art.codiceRedazionale=075U0146&art.dataPubblicazioneGazzetta=1975-05-16&art.flagTipoArticolo=1&art.idArticolo=11&art.idGruppo=0&art.idSottoArticolo=1&art.idSottoArticolo1=10&art.progressivo=0&art.versione=1).
2. [Legge 28/1982, tabella sostitutiva e note, riproduzione del testo](https://www.edizionieuropee.it/law/html/23/zn46_08_416.html): conferma riduzioni e regole dei tempi, distinguendo immersioni in saturazione e non in saturazione.
3. [D.P.R. 53/2025, articolo 7, Gazzetta Ufficiale](https://www.gazzettaufficiale.it/atto/serie_generale/caricaArticolo?art.codiceRedazionale=25G00046&art.dataPubblicazioneGazzetta=2025-04-18&art.flagTipoArticolo=0&art.idArticolo=7&art.idGruppo=1&art.idSottoArticolo=1&art.idSottoArticolo1=10&art.progressivo=0&art.versione=1): importi per le Forze di polizia ad ordinamento civile con decorrenza 1 gennaio 2024. [PDF ufficiale, pagina stampata 45](https://www.gazzettaufficiale.it/eli/gu/2025/04/18/91/so/12/sg/pdf#page=49).

## Risultati riprodotti sul programma

Prova eseguita su un database temporaneo con dati fittizi e tariffe iniziali del catalogo, senza aprire o modificare database operativi.

| Apparato | Categoria ore | Profondità | Ore | Tariffa catalogo | Riepilogo servizio | Contabilità mensile |
| --- | --- | --- | --- | --- | --- | --- |
| A.R.A./ASAS | ADD | 10 m | 1 | 5,00 | 5,00 | 2,50 |
| C.I. | CI | 10 m | 1 | 2,48 | 2,48 | 1,984, visualizzato 1,98 |
| C.I. | CI | 20 m | 1 | 2,48 | 2,48 | 1,984, visualizzato 1,98 |
| C.I. | ADD | 10 m | 1 | assente | vuoto | 0,00 |
| A.R.A./ASAS | ORD | 10 m | 1 | 5,00 | 5,00 | 5,00 |

### Verifica del database locale effettivo

Lettura eseguita in modalità SQLite `mode=ro` e `PRAGMA query_only=ON` sul profilo locale `Conta`. Sono state consultate soltanto le tariffe e i cataloghi collegati. Il database non è stato modificato.

Le tariffe locali sono già calcolate per categoria:

- A.R.A./ASAS 00/12: ORD 5,00; ADD 2,50; SPER 6,25.
- A.R.A./ASAS 13/25: ORD 10,00; ADD 5,00; SPER 12,50.
- Le altre fasce A.R.A. e le voci A.R.O./A.R.M. seguono lo stesso criterio: ADD già al 50% e SPER già al 125% della tariffa ordinaria.
- C.I.: 00/12 = 4,00; 13/25 = 8,00; 26/40 = 16,00; 41/55 = 22,40. Questi valori corrispondono già all'80% delle rispettive tariffe aria 5,00/10,00/20,00/28,00.

Con queste tariffe, per un'ora:

| Caso locale | Tariffa già corretta | Riepilogo servizio | Mensile attuale | Errore nel mensile |
| --- | --- | --- | --- | --- |
| A.R.A./ASAS ADD 00/12 | 2,50 | 2,50 | 1,25 | dimezza nuovamente |
| C.I. 00/12 | 4,00 | 4,00 | 3,20 | applica nuovamente l'80% |
| C.I. 13/25 | 8,00 | 8,00 | 6,40 | applica nuovamente l'80% |
| A.R.A./ASAS SPER 00/12 | 6,25 | 6,25 | 7,8125 prima della visualizzazione | maggiora nuovamente del 25% |

### Problemi confermati

1. **Riduzione doppia confermata per i valori del database locale**: il riepilogo usa la tariffa già ridotta; il mensile riapplica il coefficiente. L'importo del riepilogo non viene trasferito al mensile: quest'ultimo ricalcola autonomamente dai dati salvati. Lo stesso difetto riguarda anche la maggiorazione SPER.
2. Il codice non usa un criterio unico: il riepilogo interpreta la tariffa come importo orario finale della categoria; il mensile come tariffa base prima dei coefficienti.
3. Il catalogo iniziale nel sorgente è diverso dal catalogo locale: contiene tariffe ADD/SPER non adeguate alla categoria e C.I. fissa a 2,48. Un'eventuale correzione deve considerare sia i cataloghi già personalizzati sia le nuove installazioni, evitando di applicare riduzioni alle tariffe locali una seconda volta.
4. La combinazione apparato C.I. + categoria ADD è accettata al salvataggio ma non trova una regola tariffaria e produce zero nella contabilità. Non va risolta applicando automaticamente riduzioni cumulative senza aver stabilito la regola del caso concreto.

### Punti del codice

- `SMZ.Conta.App/ViewModels/MainWindowViewModel.ServiceDrafts.cs`, metodo `AggiornaCalcoliPartecipazioneImmersione`.
- `SMZ.Conta.App/ViewModels/MainWindowViewModel.Mapping.cs`, metodo `CalcolaImportoRiepilogoImmersione`.
- `SMZ.Conta.App/Data/PersonaleRepository.AccountingHelpers.cs`, metodo `GetContabilitaSmzImmersioni`: formule separate per ADD, SPER e CI, sia per interni sia per esterni.
- `SMZ.Conta.App/Models/CataloghiServizio.cs`, regole iniziali C.I. 21, 32, 33, 34.

È stato verificato il database locale `Conta`. Un database su un altro PC o un diverso profilo deve essere controllato separatamente prima di estendere la conclusione ai suoi importi.

## Correzione individuata

Conservare le tariffe locali già corrette e usare `tariffa della categoria × ore contabili` in riepilogo e mensile, senza ulteriori fattori ADD/CI/SPER. Adeguare coerentemente anche i valori iniziali dei nuovi cataloghi. Verificare separatamente come trattare cataloghi precedenti con tariffe base non ancora adeguate: non sovrascrivere automaticamente tariffe personalizzate.

Controllare inoltre eventuali chiusure mensili già salvate: contengono importi memorizzati e non si correggono semplicemente cambiando la formula delle nuove elaborazioni.

## Ulteriori dati per casi specifici

Raccogliere un esempio dell'utente: tipo di impianto/gas, profondità equivalente, durata effettiva oppure ore già contabilizzate, categoria scelta, importo mostrato e importo atteso. Usarlo per confermare la tariffa base e verificare la correzione senza alterare altre regole contabili.

Riproduzione tecnica: `dotnet run --project .codex-build/CalcoliImmersioneCheck/CalcoliImmersioneCheck.csproj -c Release --no-restore`.

## Correzione applicata

- Il mensile usa tariffa finale della categoria × ore, sia per il personale interno sia per gli operatori di altri reparti. Non riapplica i coefficienti ADD, C.I. o SPER.
- Il catalogo iniziale ora contiene gia le tariffe finali: ADD al 50%, SPER al 125%, C.I. 4,00/8,00/16,00/22,40 nelle quattro fasce.
- All'avvio viene aggiornato automaticamente soltanto un catalogo che coincide completamente con tutte le 34 regole originali precedenti, comprese attivazione e date. Una qualsiasi personalizzazione impedisce la riscrittura del catalogo. Un catalogo misto va verificato nella pagina Tariffe contabili: i valori devono rappresentare importi orari finali della categoria.
- La pagina Tariffe contabili spiega che i valori comprendono gia le percentuali. Dopo il salvataggio di una tariffa si aggiorna anche il riepilogo del servizio aperto.
- Le chiusure mensili salvate restano conservate. Per correggere un mese gia chiuso usare **Rigenera chiusura** e confermare la sostituzione.
- Le ore e gli arrotondamenti esistenti non sono stati modificati.

Verifica aggiunta al test runner: tutte le 34 regole iniziali per personale interno ed esterno, ore intere e frazionarie, tariffe personalizzate, aggiornamento del catalogo originale, conservazione di tariffe/attivazioni/date personalizzate e rigenerazione esplicita delle chiusure mensili. Le prove usano database temporanei isolati.
