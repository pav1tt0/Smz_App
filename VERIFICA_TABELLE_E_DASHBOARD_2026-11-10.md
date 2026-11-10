# Verifica tabelle e avvisi dashboard - 10 novembre 2026

## Correzione delle colonne

La verifica del controllo WPF reale ha riprodotto la compressione: in presenza di una colonna elastica, anche le colonne con larghezza in pixel potevano restringersi. Ora ogni colonna in pixel ha una larghezza minima corrispondente; le colonne elastiche hanno minimi adatti al contenuto. Quando lo spazio non basta, compare lo scorrimento orizzontale.

Nel riepilogo abilitazioni il tipo si adatta al testo con un minimo di 300 pixel, la data di conseguimento dispone di 140 pixel e le note di almeno 240 pixel. Le intestazioni comuni possono andare a capo e aumentare in altezza.

Controllate tutte le 22 DataGrid delle viste:

| Vista | Griglie |
| --- | ---: |
| Archivio | 1 |
| Contabilita | 3 |
| Personale: abilitazioni, visite, attagliamento | 3 |
| Report | 8 |
| Servizio giornaliero | 3 |
| Impostazioni | 4 |

La selezione del personale del servizio usa gia una griglia di almeno 830 pixel con scorrimento orizzontale e minimi nelle colonne dei nominativi, ruoli e gruppi; questi vincoli sono stati conservati.

## Visite bimestrali

Confermata la disattivazione della visita periodica bimestrale. Esteso il riconoscimento anche alle diciture esatte `Bimestrale`, `Visita bimestrale` e `Visite bimestrali`, ignorando maiuscole e spazi esterni. Il filtro usa gli stessi nomi nel catalogo e nelle query: nessun avviso, urgenza o conteggio della dashboard per queste visite. I record pregressi restano conservati al salvataggio della scheda.

Il controllo in sola lettura del database operativo ha rilevato la dicitura `Visita periodica bimestrale`, correttamente riconosciuta come disattivata. Restano valide le regole concordate: visite facoltative e non bloccanti, D.Lgs. 81/08 a 12 mesi, mantenimento brevetto M.M. a 24 mesi.

## Verifiche

- Controlli WPF reali con righe di esempio a larghezze di 400, 860 e 1240 pixel: 66 configurazioni. Verificati minimi di tutte le colonne e accesso tramite scorrimento orizzontale.
- Ispezione dell'anteprima del riepilogo abilitazioni a 860 pixel: intestazioni leggibili, colonne non compresse e barra orizzontale presente.
- Compilazione Debug e Release senza errori negli output ordinari.
- Tutti i 15 test funzionali superati, compreso il nuovo caso con nomi alternativi, scadenze passate e future, contatori dashboard e conservazione dei dati storici.
- La verifica online NuGet delle vulnerabilita ha prodotto NU1900 per la rete non disponibile. Restore e compilazione sono riusciti.

Risultati e anteprime locali: `artifacts/table-audit/layout-results.txt` e immagini nella stessa cartella, esclusa da Git. Il programma aggiornato si avvia con `Avvia-SMZ.cmd`; i pacchetti gia presenti in `dist` non sono stati rigenerati.
