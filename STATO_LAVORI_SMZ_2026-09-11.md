# Stato lavori SMZ - 11 settembre 2026

Questo documento serve per riprendere il progetto senza perdere il quadro del lavoro completato, del collaudo in corso e delle attività ancora aperte.

## Situazione del repository

- Branch di lavoro: `restyling-ui-2026`.
- Ultimo commit pubblicato su GitHub: `f9cebf9`.
- Al termine delle ultime modifiche il branch locale e quello remoto erano sincronizzati.
- Build Release e Debug completate senza errori.
- Test automatici superati, compresi database isolato, versione SQLite, numero servizio, anagrafica, servizio, contabilità, accessi e ripristino backup.
- Il file locale `AUDIT_SICUREZZA_LOGICA_2026-09-10.txt` non è stato inserito nel repository.

## Lavoro completato recentemente

- [x] Numero dell'ordine di servizio proposto automaticamente dalla data, usando il giorno progressivo annuale `001`-`366` (`73a79da`).
- [x] Piano dettagliato per la futura gestione dei servizi compilati fuori sede (`8df8695`).
- [x] Aggiornamento di `Microsoft.Data.Sqlite`, runtime SQLite verificato alla versione 3.53.3 e controllo vulnerabilità NuGet riattivato (`ce7415d`).
- [x] Inserimento del modello `Foglio di servizio SMZ 2026.docx` tra i file del programma (`5ee708d`).
- [x] Ripristino backup protetto con controlli su archivio, dimensioni, manifest, hash, integrità SQLite e schema, scambio atomico, rollback e nuova autenticazione (`272d1e6`).
- [x] Pulizia conservativa dei file generati e dei vecchi pacchetti, mantenendo l'eseguibile Debug e il pacchetto con anagrafica precompilata.
- [x] Interfaccia adattata agli schermi più piccoli, con scorrimento orizzontale disponibile quando necessario (`15a9c9d`).
- [x] Schede del personale disposte definitivamente su tre colonne con righe automatiche (`9c8e765`).
- [x] Login disattivato esclusivamente nella compilazione Debug per velocizzare il collaudo (`d270910`).
- [x] Login ancora obbligatorio nelle compilazioni Release e nei pacchetti destinati agli altri PC.
- [x] Transizione continua fra login e schermata principale: lo sfondo SMZ rimane visibile fino al completamento del caricamento (`5aaa041`, `f9cebf9`).
- [x] Ultime modifiche pubblicate sul branch remoto `restyling-ui-2026`.

## Collaudo operativo in corso sul PC SMZ

Sul PC degli SMZ è stata installata la versione disponibile nella cartella `Dist`. Lo scopo del collaudo è:

- [ ] completare tutta l'anagrafica reale necessaria;
- [ ] inserire tutti i servizi di settembre;
- [ ] verificare i servizi, le immersioni, le presenze e gli orari inseriti;
- [ ] confrontare i risultati della contabilità con i conteggi attesi o eseguiti manualmente;
- [ ] annotare differenze, dati mancanti e problemi di usabilità;
- [ ] provare l'app sullo schermo effettivo, con la risoluzione e il ridimensionamento di Windows usati dagli operatori.

La versione installata da `Dist` è precedente alle ultime correzioni grafiche. Prima della distribuzione finale deve essere preparato e provato un nuovo pacchetto.

## Recupero dell'anagrafica e dei dati del collaudo

I dati compilati sul PC SMZ possono essere recuperati e trasferiti nella versione finale.

Procedura da eseguire alla fine del collaudo:

1. non cancellare o reinstallare l'app sul PC di prova;
2. creare dall'app un backup esterno `.smzbak`;
3. copiare il backup anche su un secondo supporto sicuro;
4. verificare il ripristino su una copia di prova della versione finale;
5. controllare anagrafica, visite, abilitazioni, servizi di settembre e risultati contabili;
6. soltanto dopo la verifica, usare il backup validato come base dati della versione definitiva;
7. riconfigurare la cartella di backup esterno sul PC definitivo.

Il database operativo si trova sotto `%LOCALAPPDATA%\SMZ\Conta`, ma per il trasferimento ordinario va usato il backup `.smzbak` prodotto dall'app.

## Attività immediate ancora da fare

- [ ] Concludere il collaudo con tutti i servizi di settembre.
- [ ] Confrontare la contabilità generata con il risultato di riferimento e documentare ogni differenza.
- [ ] Verificare manualmente il nuovo passaggio login-caricamento-app su un PC meno veloce.
- [ ] Verificare la nuova griglia a tre colonne e l'adattamento dell'interfaccia sul PC SMZ.
- [ ] Creare e verificare il backup finale del database di collaudo.
- [ ] Preparare un nuovo pacchetto `Dist` Release con tutte le correzioni pubblicate.
- [ ] Installare il nuovo pacchetto su un PC di prova senza sovrascrivere i dati raccolti.
- [ ] Decidere, a collaudo concluso, se rimuovere anche dal codice il bypass del login Debug, così da evitare distribuzioni accidentali di una build di test.
- [ ] Eseguire il collaudo finale completo prima dell'uso ordinario.

## Audit di sicurezza e logica: stato aggiornato

Interventi ad alta priorità già chiusi:

- [x] dipendenza SQLite vulnerabile e audit NuGet;
- [x] ripristino backup non validato, senza rollback e senza nuova autenticazione.

Problemi ancora aperti, in ordine operativo consigliato:

- [ ] collegare la cancellazione definitiva del personale alla sospensione o eliminazione controllata del relativo account;
- [ ] proteggere database, backup ed esportazioni contenenti dati personali e sanitari, definendo cifratura, chiavi e autorizzazioni Windows;
- [ ] impedire importazioni duplicate dei pacchetti servizio e validare in modo completo i file importati;
- [ ] neutralizzare nei CSV i valori che iniziano con `=`, `+`, `-` o `@`;
- [ ] evitare che il seeding iniziale sovrascriva modifiche amministrative ai cataloghi;
- [ ] rendere le chiusure contabili realmente storicizzate e definire arrotondamenti basati su minuti e centesimi interi;
- [ ] introdurre limite tentativi di accesso, scadenza per inattività, rivalutazione delle sessioni, audit log e permessi più granulari;
- [ ] migliorare la conservazione dei backup con una politica temporale, non soltanto numerica;
- [ ] verificare anonimizzazione e opportunità di conservare documenti Office nel repository remoto;
- [ ] valutare la firma digitale dell'eseguibile finale.

## Servizi svolti fuori sede

Il progetto dedicato non è ancora stato realizzato. Il dettaglio si trova in `PIANO_SERVIZI_FUORI_SEDE.md`.

Passaggi principali ancora necessari:

- [ ] definire le decisioni operative ancora aperte e il formato pacchetto versione 2;
- [ ] rendere l'importazione sicura, transazionale e non duplicabile;
- [ ] estrarre in una libreria comune modelli, validazioni e regole contabili;
- [ ] realizzare l'app offline ridotta per la compilazione in trasferta;
- [ ] cifrare e autenticare i dati trasferiti;
- [ ] eseguire il collaudo completo esportazione-importazione-contabilità.

Fino a quel momento il file `.smzsvc` attuale è una base tecnica e non va considerato il flusso definitivo per dati operativi reali.

## Altre decisioni aperte

- [ ] Stabilire se gli SMZ esterni o occasionali devono produrre importi contabili oppure comparire soltanto nel servizio e nel registro immersioni.
- [ ] Scegliere il formato definitivo di distribuzione. La soluzione consigliata resta un pacchetto ZIP self-contained `win-x64`.
- [ ] Proseguire gradualmente con la separazione di `MainWindowViewModel` e delle viste più grandi, senza interrompere il collaudo operativo.

## Ordine consigliato per la prossima ripresa

1. raccogliere l'esito del collaudo di settembre e correggere eventuali errori contabili;
2. mettere al sicuro e verificare il backup con l'anagrafica completa;
3. chiudere il problema del ciclo di vita degli account;
4. correggere l'esportazione CSV;
5. rendere idempotente e sicura l'importazione dei servizi;
6. preparare e provare il nuovo pacchetto `Dist`;
7. riprendere il progetto dell'app per i servizi fuori sede.

## Documenti di riferimento

- `PIANO_SERVIZI_FUORI_SEDE.md`: progetto dettagliato per la compilazione in trasferta.
- `PROCEDURA_BACKUP_E_CAMBIO_PC_SMZ.md`: procedura operativa per salvare e trasferire i dati.
- `ROADMAP_MANUTENZIONE.md`: interventi di manutenzione e separazione del codice.
- `NOTE_PROSSIME_MODIFICHE.md`: decisione ancora aperta sugli SMZ esterni e opzioni di distribuzione.
- `AUDIT_SICUREZZA_LOGICA_2026-09-10.txt`: fotografia originale dell'audit; alcune criticità indicate lì sono già state risolte, come riportato in questo documento.
- `CHECKLIST_FINALE_MD_VS_STATO_REALE.md`: fotografia storica del 31 marzo 2026, non più affidabile come stato corrente.
