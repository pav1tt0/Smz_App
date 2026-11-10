# SMZ — confronto con il collega del 10 novembre 2026

Promemoria concordato il 9 novembre. Domani l'utente e il collega che utilizzerà il programma decideranno quali interventi realizzare. Nessuna correzione tecnica autorizzata o avviata sulla base del solo audit.

## Uso previsto e criterio di lavoro

- Progetto amatoriale per contabilizzare servizi e immersioni dei SMZ del Nucleo di SP.
- Uso locale su notebook; evitare sistemi complessi non necessari all'operatività reale.
- Priorità: correttezza della contabilità, completezza delle stampe e possibilità di recuperare i dati.
- L'avvio di sviluppo che salta il login è stato richiesto dall'utente per velocizzare le modifiche: mantenerlo. Per il pacchetto operativo controllare il percorso Release con autenticazione.
- L'audit è un elenco di evidenze e rischi, non un ordine di implementare tutto. Le priorità tecniche iniziali sono ridimensionate dal contesto d'uso concordato.

## Proposta ridotta da discutere, in ordine

1. **Contabilità:** scegliere casi reali rappresentativi, ricrearli con dati fittizi e confrontare i risultati con conteggi manuali o fogli Excel approvati dal collega. Verificare categorie, tariffe, ore, maggiorazioni, arrotondamenti e riepiloghi mensili. Non è stato dimostrato che i conteggi ordinari attuali siano sbagliati.
2. **Stampe:** controllare foglio di servizio, registro immersioni e riepiloghi contabili. Verificare nominativi, presenze, esterni, ore, totali, molte righe e cambi pagina. Decidere quali documenti sono effettivamente necessari e quali modelli sono corretti.
3. **Cataloghi:** correggere le modifiche amministrative ai cataloghi predefiniti che vengono perse al riavvio.
4. **Backup semplici e affidabili:** copia automatica locale, copia periodica su supporto separato e prova di ripristino. Definire una conservazione che non perda tutto lo storico dopo molti salvataggi nello stesso giorno e avvisi comprensibili se la copia esterna fallisce.
5. **Chiusure mensili:** concordare una conservazione semplice delle versioni precedenti, per esempio una copia datata prima di rigenerare una mensilità. Evitare un sistema complesso se non serve.
6. **Scambio servizi solo se utilizzato:** se i servizi saranno trasferiti tra computer, correggere prima perdita degli operatori esterni, importazioni duplicate e validazione dei dati. Se non si usa il flusso, rinviare questi interventi.

## Decisioni da prendere con il collega

- Quali prospetti contabili e stampe userà davvero? Quali esempi già verificati possono diventare riferimento?
- Quali tariffe e regole di arrotondamento sono confermate? Quali casi particolari devono essere coperti?
- Gli operatori esterni producono importi oppure compaiono soltanto nei documenti e nel registro?
- Il programma sarà usato su un solo notebook? Si potranno aprire più istanze sullo stesso archivio?
- Si useranno import/export dei servizi, CSV o soltanto stampe/PDF e file Excel?
- Dove conservare la copia esterna, con quale frequenza e per quanto tempo? Chi ne verifica il ripristino?
- È sufficiente salvare una copia datata delle chiusure oppure serve una vera cronologia nel programma?

Le risposte sono ancora da raccogliere: non considerare nessuna opzione già approvata.

## Interventi da ridimensionare o rinviare

- Ruoli granulari, revoca delle sessioni, blocco tentativi e audit log articolati: priorità inferiore per il contesto descritto, da valutare solo in base all'uso effettivo.
- Firma digitale e rifacimenti strutturali: non anteporli a contabilità e stampe.
- Cifratura dentro l'applicazione: non avviarla automaticamente; valutare prima le protezioni Windows del notebook e dei supporti di backup, mantenendo semplice il recupero su altro PC.
- Conversione immediata di tutti i calcoli da `REAL` a centesimi/minuti interi: prima verificare risultati e arrotondamenti; non cambiare rappresentazione e regole senza un beneficio dimostrato.
- Runtime: resta da pianificare l'aggiornamento, perché il supporto di .NET 8 termina il 10 novembre 2026 secondo la fonte Microsoft già riportata nell'audit. È manutenzione tecnica, non motivo per introdurre sistemi di sicurezza complessi.

## Stato da cui ripartire

- Audit completo: [AUDIT_PROGETTO_2026-11-09.md](AUDIT_PROGETTO_2026-11-09.md), con evidenze e limiti.
- Build Release riuscita; 14 scenari automatici esistenti superati.
- 14 condizioni problematiche riprodotte con dati fittizi isolati; prove locali in `artifacts/audit-2026-11-09/Probe`.
- Il controllo NuGet online non ha segnalato vulnerabilità note nelle dipendenze esaminate.
- Nessuna correzione al codice e nessuna modifica al database operativo durante l'audit o la preparazione di questo promemoria.
- Conservare le regole già concordate: visite informative, facoltative e non bloccanti; D.Lgs. 81/08 a 12 mesi; mantenimento brevetto M.M. a 24 mesi; bimestrale disattivata con conservazione dei dati pregressi.

**Prossimo passo:** raccogliere le decisioni con il collega, aggiornare la lista con gli interventi scelti e solo dopo iniziare le modifiche.
