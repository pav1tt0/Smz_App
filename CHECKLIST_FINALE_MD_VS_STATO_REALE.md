# Checklist Finale `.md` vs Stato Reale

Snapshot operativo del progetto alla data del 27/03/2026, costruito confrontando:

- `SPEC_PROGRAMMA_COMPLETO_SMZ.md`
- `SPEC_ABILITAZIONI.md`
- `RIEPILOGO_PROGETTO_CONTA.md`

con lo stato reale del codice, in particolare:

- `SMZ.Conta.App/MainWindow.xaml`
- `SMZ.Conta.App/ViewModels/MainWindowViewModel.cs`
- `SMZ.Conta.App/Data/PersonaleRepository.cs`
- `SMZ.Conta.App/Data/DatabaseInitializer.cs`

## Sintesi

Il progetto reale e gia oltre `RIEPILOGO_PROGETTO_CONTA.md` e molto piu vicino alla specifica completa.

Il cuore del gestionale risulta gia costruito in modo utilizzabile:

- anagrafica estesa personale
- archivio schede eliminate con ripristino
- abilitazioni e visite mediche con scadenze
- foglio di servizio giornaliero strutturato
- immersioni multiple nello stesso servizio
- partecipazione personale e supporto occasionale
- dettaglio tecnico-contabile per persona x immersione
- contabilita mensile base calcolata live
- tariffe contabili modificabili da interfaccia

Il delta residuo e concentrato soprattutto nel blocco finale di chiusura progetto:

- stampe/export reali
- registro immersioni mensile
- chiusura mensile persistita
- backup/ripristino applicativo
- eventuale editor cataloghi completo lato UI

## Checklist Operativa

| Voce `.md` | Stato reale nel codice | Delta per chiusura |
| --- | --- | --- |
| Anagrafica personale base | Allineata e superata. La scheda personale gestisce dati anagrafici estesi, contatti, profilo personale, ruolo sanitario, matricola e numero brevetto. | Nessun delta strutturale. Eventuali rifiniture solo UX o campi secondari. |
| Archivio anagrafico unico | Presente. Esistono tabelle archivio, lettura archivio, ripristino e cancellazione definitiva delle schede eliminate. | Chiuso a livello funzionale. |
| Abilitazioni personale | Presente. Catalogo nel DB, inserimento guidato, filtri, suggerimenti, profondita/livelli/scadenze attivi. | Manca solo una gestione completamente amministrabile da UI del catalogo abilitazioni. |
| Visite mediche | Presente. Scadenze, esiti e logica di allineamento visite sono operative. | Chiuso nella parte anagrafica. |
| Scadenzario operativo | Presente e ben integrato in dashboard, ricerca e scheda. | Chiuso salvo eventuali migliorie di reportistica. |
| Foglio di servizio giornaliero | Presente. Il modulo gestisce data, tipo servizio, localita, scopo, unita navale, fuori sede, attivita, note, numero ordine servizio e orario servizio. | Chiuso come base operativa. |
| Partecipanti al servizio | Presente. Per ogni dipendente si salvano presenza, gruppo operativo, ruolo operativo e note. | Chiuso nella logica principale. |
| Supporto occasionale | Presente. Gestito nel servizio senza obbligo di anagrafica stabile. | Chiuso per l'uso operativo corrente. |
| Immersioni multiple nello stesso servizio | Presente. Nessun limite fisso a due immersioni; la struttura e dinamica. | Chiuso. |
| Dati tecnici di impiego in immersione | Presente. Ogni partecipazione immersione salva apparato, profondita, fascia, ore, categoria contabile e note. | Chiuso. |
| Ruoli immersione specifici | Presente. Direttore immersione, operatore soccorso, assistenza BLSD e assistenza sanitaria sono gestiti per immersione. | Chiuso. |
| Calcolo contabile immersioni SMZ | Presente. Il calcolo e applicato a livello persona x immersione usando regole contabili e tariffe da database. | Chiuso come motore live. |
| Contabilita mensile base | Presente ma live. L'app mostra riepiloghi mensili SMZ, sanitari e supporto basati sui servizi registrati. | Manca la chiusura mensile come sessione persistita e salvata. |
| Tariffe contabili modificabili | Presente. Le tariffe si possono modificare da interfaccia e salvare nel database. | Chiuso. |
| Registro immersioni mensile | Non trovato come output finale vero e proprio. Esiste la base dati necessaria, ma non la generazione finale del registro. | Da implementare. E uno dei blocchi principali mancanti. |
| Indennita assistenza SMZ | Parziale. I dati di servizio e i conteggi base ci sono, soprattutto per sanitari e supporto, ma non emerge ancora un prospetto finale strutturato dedicato. | Da chiudere come output mensile finale. |
| Indennita fuori sede | Parziale. Il servizio salva il flag fuori sede e la UI mostra il relativo stato. | Manca il calcolo finale strutturato e il prospetto dedicato. |
| Elaborazione mensile | Parziale rispetto alla specifica completa. La specifica prevede entita `ElaborazioneMensile` e `ElaborazioneMensileRiga`, ma nel DB attuale non risultano tabelle dedicate. | Da implementare se si vuole la chiusura mese persistita, storicizzata e ristampabile. |
| Stampe e report finali | Solo predisposti. In UI esiste il tab dedicato, ma senza generazione reale dei documenti finali. | Da implementare davvero: Word/PDF/Excel o altro formato deciso. |
| Export finali | Non ancora chiusi. La struttura concettuale e pronta, ma non risultano esportazioni reali operative. | Da implementare insieme al blocco stampe/report. |
| Backup e ripristino applicativo | Non presente come modulo dedicato. Oggi esiste solo il vantaggio naturale del file SQLite e l'archivio anagrafico. | Da implementare con funzione esplicita di backup/ripristino applicativo. |
| Cataloghi servizio nel database | Presenti per localita, scopi, unita navali, tipologie immersione, fasce, categorie, gruppi, ruoli e regole contabili. | Manca, se voluta, una UI amministrativa completa per modificarli senza codice. |
| Catalogo abilitazioni completamente da DB/UI | Solo parzialmente. I dati sono nel DB, ma ordine di visualizzazione e suggerimenti restano agganciati a `CatalogoAbilitazioni.cs`. | Da chiudere se l'obiettivo e rendere il catalogo davvero amministrabile senza toccare codice. |
| Dashboard operativa | Presente e piu evoluta rispetto ai documenti iniziali. | Nessun delta obbligatorio per la chiusura funzionale. |
| Distinzione profili SMZ operativo / Sanitario | Presente nel modello dati e nella UI. | Chiuso. |
| Ricerca e filtri avanzati | Presente con suggerimenti, filtri per abilitazioni e scadenze. | Chiuso per lo scenario attuale. |
| Stato progetto rispetto a `RIEPILOGO_PROGETTO_CONTA.md` | Ampiamente superato. Il programma non e piu solo "operatori + immersioni + contabilita semplice". | Il riferimento corretto per la chiusura e ormai `SPEC_PROGRAMMA_COMPLETO_SMZ.md`, non il vecchio riepilogo. |

## Priorita Di Chiusura

Ordine consigliato per arrivare a "progetto finito" senza disperdere lavoro:

1. Registro immersioni mensile
2. Stampe/export reali
3. Chiusura mensile persistita (`ElaborazioneMensile` + righe)
4. Prospetti finali indennita assistenza e fuori sede
5. Backup/ripristino applicativo
6. Eventuale editor cataloghi completo da UI

## Nota Operativa

Per lavorare bene da ora in avanti conviene considerare:

- `RIEPILOGO_PROGETTO_CONTA.md` come documento storico iniziale
- `SPEC_PROGRAMMA_COMPLETO_SMZ.md` come riferimento funzionale principale
- questo file come checklist di allineamento reale tra documentazione e codice
