# Checklist Finale `.md` vs Stato Reale

Snapshot operativo del progetto alla data del 31/03/2026, costruito confrontando:

- `SPEC_PROGRAMMA_COMPLETO_SMZ.md`
- `SPEC_ABILITAZIONI.md`
- `RIEPILOGO_PROGETTO_CONTA.md`

con lo stato reale del codice, in particolare:

- `SMZ.Conta.App/MainWindow.xaml`
- `SMZ.Conta.App/ViewModels/MainWindowViewModel.cs`
- `SMZ.Conta.App/ViewModels/ServizioImmersioneDraftViewModel.cs`
- `SMZ.Conta.App/Data/PersonaleRepository.cs`
- `SMZ.Conta.App/Data/DatabaseInitializer.cs`
- `SMZ.Conta.App/Models/ServizioGiornaliero.cs`
- `SMZ.Conta.App/Models/RegistroImmersioniMensile.cs`

## Sintesi

Il progetto reale e ormai nettamente oltre `RIEPILOGO_PROGETTO_CONTA.md` ed e vicino alla struttura prevista da `SPEC_PROGRAMMA_COMPLETO_SMZ.md`.

Il nucleo del gestionale oggi risulta operativo:

- anagrafica personale estesa
- archivio schede eliminate con ripristino
- abilitazioni e visite mediche con scadenze
- dashboard e ricerca con filtri utili
- foglio di servizio giornaliero persistito
- partecipanti interni e supporti occasionali
- dettaglio tecnico-contabile per persona in immersione
- contabilita mensile live su dati salvati
- tariffe contabili modificabili da interfaccia
- registro immersioni mensile interno con riepilogo per categoria

Il delta residuo si concentra in 2 blocchi:

1. chiusura amministrativa finale del mese
2. rifiniture di allineamento tra modello dati e UI operativa

I punti ancora apertamente mancanti sono:

- chiusura mensile persistita (`ElaborazioneMensile` e righe)
- stampe/export reali (PDF, Excel, Word o formato deciso)
- prospetti finali dedicati per assistenza e fuori sede
- backup/ripristino applicativo completo
- editor amministrativo completo dei cataloghi

Ci sono inoltre 2 differenze importanti rispetto alla checklist del 27/03/2026:

- il `Registro immersioni mensile` non e piu da considerare mancante: esiste davvero nel codice e nella UI
- le `immersioni multiple` sono supportate dal modello dati, ma la UI di inserimento crea ancora 2 bozze iniziali e non espone un comando esplicito per aggiungerne altre

## Checklist Operativa Aggiornata

| Voce `.md` | Stato reale nel codice | Delta per chiusura |
| --- | --- | --- |
| Anagrafica personale base | Allineata e superata. La scheda personale gestisce dati anagrafici estesi, contatti, profilo personale, ruolo sanitario, matricola e numero brevetto. | Nessun delta strutturale. Restano solo rifiniture UX. |
| Archivio anagrafico unico | Presente. Esistono archivio, lettura dettaglio, ripristino e cancellazione definitiva delle schede eliminate. | Chiuso a livello funzionale. |
| Abilitazioni personale | Presente. Catalogo nel DB, suggerimenti, profondita, livelli e scadenze sono operativi. | Manca ancora una gestione completamente amministrabile da UI del catalogo. |
| Visite mediche | Presente. Scadenze, esiti e allineamento visite sono operativi. | Chiuso nella parte anagrafica. |
| Scadenzario operativo | Presente e ben integrato in dashboard, ricerca e scheda personale. | Chiuso salvo eventuali migliorie di reportistica. |
| Foglio di servizio giornaliero | Presente. Gestisce data, numero ordine servizio, orario, tipo servizio, localita, scopo, unita navale, fuori sede, attivita e note. | Chiuso come base operativa. |
| Partecipanti al servizio | Presente. Per ogni dipendente si salvano presenza, gruppo operativo, ruolo operativo e note. | Chiuso nella logica principale. |
| Supporto occasionale | Presente. Gestito nel servizio senza obbligo di anagrafica stabile. | Chiuso per l'uso operativo corrente. |
| Immersioni multiple nello stesso servizio | Parziale avanzato. Il database supporta piu immersioni e il caricamento legge tutte le immersioni salvate, ma l'editor iniziale crea ancora 2 bozze e non ha un comando esplicito per aggiungerne/rimuoverne altre. | Da chiudere lato UI se l'obiettivo e avere servizio davvero dinamico senza limite operativo percepito. |
| Dati tecnici di impiego in immersione | Presente. Ogni partecipazione immersione salva apparato, profondita, fascia, ore, categoria contabile e note. | Chiuso. |
| Ruoli immersione specifici | Presente. Direttore immersione, operatore soccorso, assistenza BLSD e assistenza sanitaria sono gestiti per immersione. | Chiuso. |
| Localita e scopo specifici della singola immersione | Parziale. Il modello dati e il DB li prevedono, ma l'editor attuale non espone campi dedicati e salva i valori del servizio principale anche nella riga immersione. | Da completare lato UI se la specifica completa deve essere rispettata fino in fondo. |
| Calcolo contabile immersioni SMZ | Presente. Il calcolo e applicato a livello persona x immersione usando regole contabili e tariffe da database. | Chiuso come motore live. |
| Contabilita mensile base | Presente. L'app mostra riepiloghi mensili SMZ, sanitari e supporto basati sui servizi registrati. | Manca la chiusura mensile come sessione persistita e storicizzata. |
| Tariffe contabili modificabili | Presente. Le tariffe si possono modificare da interfaccia e salvare nel database. | Chiuso. |
| Registro immersioni mensile | Presente. Esistono query dedicate, tab report e riepilogo per categoria di registro derivato dai servizi salvati. | Manca la parte finale di stampa/export del registro. |
| Indennita assistenza SMZ | Parziale avanzato. Esistono riepiloghi mensili per sanitari e supporti con conteggio giornate utili, ma non emerge ancora un prospetto finale dedicato e formalizzato. | Da chiudere come output mensile finale. |
| Indennita fuori sede | Parziale. Il servizio salva il flag fuori sede, ma non risulta ancora un prospetto mensile dedicato o un calcolo finale strutturato. | Da implementare come report/prospetto finale. |
| Elaborazione mensile | Assente rispetto alla specifica completa. Nel DB attuale non risultano tabelle dedicate a `ElaborazioneMensile` e `ElaborazioneMensileRiga`. | Da implementare se si vuole la chiusura mese persistita, storicizzata e ristampabile. |
| Stampe e report finali | Parziale. Il tab report e reale e il registro mensile e consultabile, ma non c'e generazione documentale finale. | Da implementare davvero: PDF, Excel, Word o altro formato deciso. |
| Export finali | Non ancora presenti come funzioni operative reali. | Da implementare insieme al blocco stampe/report. |
| Backup e ripristino applicativo | Non presente come modulo dedicato. Esiste il ripristino dell'archivio anagrafico, ma non un backup/ripristino completo del database applicativo. | Da implementare con funzione esplicita di backup/ripristino applicativo. |
| Cataloghi servizio nel database | Presenti per localita, scopi, unita navali, tipologie immersione, fasce, categorie, gruppi, ruoli e regole contabili. | Manca, se voluta, una UI amministrativa completa per modificarli senza codice. |
| Catalogo abilitazioni completamente da DB/UI | Solo parzialmente. I dati sono nel DB, ma ordine di visualizzazione e suggerimenti restano agganciati a `CatalogoAbilitazioni.cs`. | Da chiudere se l'obiettivo e rendere il catalogo davvero amministrabile senza toccare codice. |
| Dashboard operativa | Presente e piu evoluta rispetto ai documenti iniziali. | Nessun delta obbligatorio per la chiusura funzionale. |
| Distinzione profili SMZ operativo / Sanitario | Presente nel modello dati e nella UI. | Chiuso. |
| Ricerca e filtri avanzati | Presente con suggerimenti, filtri per abilitazioni e scadenze. | Chiuso per lo scenario attuale. |
| Stato progetto rispetto a `RIEPILOGO_PROGETTO_CONTA.md` | Ampiamente superato. Il programma non e piu solo "operatori + immersioni + contabilita semplice". | Il riferimento corretto per la chiusura e ormai `SPEC_PROGRAMMA_COMPLETO_SMZ.md`, non il vecchio riepilogo. |

## Priorita Di Chiusura Aggiornate

Ordine consigliato per arrivare a "progetto finito" senza disperdere lavoro:

1. Chiudere la UI servizio/immersioni rispetto alla specifica completa
2. Implementare `ElaborazioneMensile` persistita e storicizzata
3. Chiudere i prospetti finali per assistenza SMZ e fuori sede
4. Implementare stampe/export reali del registro e dei prospetti
5. Aggiungere backup/ripristino applicativo
6. Valutare editor completo dei cataloghi da UI

## Nota Operativa

Per lavorare bene da ora in avanti conviene considerare:

- `RIEPILOGO_PROGETTO_CONTA.md` come documento storico iniziale
- `SPEC_PROGRAMMA_COMPLETO_SMZ.md` come riferimento funzionale principale
- questo file come checklist aggiornata di allineamento reale tra documentazione e codice

La correzione principale rispetto alla versione precedente di questo file e che il `Registro immersioni mensile` va ormai considerato realizzato internamente, mentre i veri blocchi mancanti sono oggi la chiusura mensile persistita, gli output finali e alcuni ultimi gap di UI rispetto al modello dati gia presente.
