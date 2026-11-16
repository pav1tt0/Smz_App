# Modifiche servizi, stampe e libretto - 16 novembre 2026

## Comportamento aggiornato

- Eliminati il suono delle bolle, il relativo comando e il lettore audio. Il WAV non viene incluso nelle risorse compilate.
- Servizi: all'apertura compare il mese corrente. Un unico filtro Periodo propone Mese, Giorno e Intervallo: mostra rispettivamente mese/anno, un solo calendario oppure Dal/Al. I campi delle altre modalità sono nascosti e non contribuiscono alla ricerca. Cambiando modalità si conservano le scelte precedenti; il primo intervallo viene proposto con gli estremi del mese scelto. Gli intervalli possono attraversare mesi e anni; entrambi gli estremi sono inclusi.
- L'elenco del periodo comprende tutti i risultati, senza i precedenti limiti di 10/100 servizi. Reimposta cancella la ricerca e ripristina la modalità Mese con il mese corrente. Date incomplete o invertite mostrano un messaggio e non caricano risultati fuori dal filtro.
- Stampe e report / Servizi: lo stesso componente grafico e la stessa selezione sono condivisi con la pagina Servizi, anche al cambio di modalità.
- Stampe e report / Registro immersioni: aggiunti Dal e Al. L'intervallo ha precedenza sui selettori mensili; Usa mese cancella l'intervallo. Il filtro vale sia per il report a video sia per la stampa.
- Registro: A4 verticale, tabelle con larghezze proporzionate e margini simmetrici, una scheda per ogni immersione, copertina e riepilogo separati. Ogni scheda usa località, scopo, orari, partecipazioni e note della propria immersione. Gli operatori sono ordinati per qualifica dalla più alta alla più bassa e, a parità di qualifica, per decorrenza più anziana, sia a video sia in stampa. Il nominativo è usato solo se coincidono anche le decorrenze; le date mancanti seguono quelle note nella stessa qualifica. Il dettaglio a video espone anche qualifica e decorrenza. Il collaudo usa nominativi in ordine alfabetico opposto alla gerarchia, per verificare la precedenza della qualifica. Le schede particolarmente lunghe vengono ridotte proporzionalmente per conservare tutti i contenuti su un solo A4.
- Dashboard: rimossa la scheda Criticità operative; la scheda delle scadenze occupa lo spazio disponibile.
- Fumetto del servizio: aggiunto il responsabile del servizio salvato.

## Libretto del sommozzatore

Nuova scheda in Stampe e report, con selezione dell'operatore, anagrafica essenziale, matricola, brevetto SMZ, storico cronologico delle immersioni registrate e totali di immersioni e ore. Lo storico comprende tutti gli anni e resta indipendente dai filtri mensili. I dati degli operatori esterni con lo stesso PerID non entrano nel libretto personale.

Il pulsante Stampa bozza A4 produce una prima anagrafica e lo storico. L'impaginazione del piccolo libro, eventuali ulteriori campi, firme, timbri e numerazione del modello cartaceo saranno definiti sulle scansioni dell'utente.

## Verifiche

Suite automatica di 43 verifiche su database temporanei isolati. I nuovi casi coprono il mese corrente con oltre 100 servizi, gli intervalli inclusivi fra mesi e anni diversi, le date non valide, il responsabile nel riepilogo, la separazione delle schede immersione, l'ordine per qualifica, note di 100 righe conservate su una pagina e lo storico personale senza contaminazioni degli esterni. Sono inoltre generate schermate dei filtri e del libretto a larghezze di 900 e 1200 pixel.

Anteprime PNG/XPS e log del collaudo: [artifacts/verifica-2026-11-16](artifacts/verifica-2026-11-16).

L'avviso NU1900 riguarda l'indisponibilità del servizio online NuGet per la verifica delle vulnerabilità; non impedisce la compilazione e non costituisce una verifica delle vulnerabilità completata.

Autori aggiornati nella landing: Paolo Vittori, Cesare Scapazzoni e Codex. Il testo va a capo nelle finestre più strette.

Creato e verificato il pacchetto operativo per riutilizzare il DB già presente sull'altro PC: [DISTRIBUZIONE_DATI_REALI_2026-11-16.md](DISTRIBUZIONE_DATI_REALI_2026-11-16.md).
