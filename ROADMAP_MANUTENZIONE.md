# Roadmap manutenzione SMZ

Documento di promemoria per riprendere il lavoro nei prossimi giorni.

## Valutazione attuale

Il progetto e' in una buona fase operativa: le funzioni principali sono presenti, l'app compila senza errori e i flussi reali stanno venendo coperti progressivamente.

Il punto principale da controllare adesso non e' aggiungere molte funzioni nuove, ma evitare che il codice diventi difficile da modificare dopo tante aggiunte successive.

## Stato manutenzione

Interventi gia' completati:

- estratta la vista `Stampe e report` in `ReportsView`;
- estratta la vista `Backup e archivio` in `BackupArchiveView`;
- estratta la vista `Impostazioni` in `SettingsView`;
- estratta la vista `Contabilita` in `ContabilitaView`;
- mantenuti invariati i binding al `MainWindowViewModel`;
- verificata la build dopo ogni estrazione.

## Obiettivo principale

Ridurre gradualmente la dimensione e la responsabilita' di `MainWindowViewModel`, senza fare una riscrittura completa e senza rompere i flussi gia' funzionanti.

Approccio consigliato: piccoli interventi separati, con build e commit dopo ogni passaggio stabile.

## Priorita' 1 - Separare il ViewModel principale

`MainWindowViewModel` oggi contiene molte aree diverse dell'applicazione:

- servizio giornaliero;
- contabilita';
- impostazioni;
- gestione cataloghi;
- tariffe;
- stampa registro immersioni;
- filtri e riepiloghi.

Questa concentrazione rende piu' rischiose le modifiche future.

### Prima fase

Separare solo metodi e logica interna, mantenendo invariati il piu' possibile i binding XAML.

Possibili estrazioni:

- logica servizio giornaliero;
- logica impostazioni/cataloghi;
- logica contabilita';
- logica stampa registro;
- logica filtri.

Rischio: basso/medio.

### Seconda fase

Creare ViewModel dedicati:

- `ServizioGiornalieroViewModel`;
- `ContabilitaViewModel`;
- `ImpostazioniViewModel`;
- `RegistroImmersioniViewModel`.

Rischio: medio, perche' bisogna aggiornare i binding.

### Terza fase

Spostare porzioni XAML in controlli separati, per rendere `MainWindow.xaml` piu' leggibile.

Possibili controlli:

- vista servizio giornaliero;
- vista contabilita';
- vista impostazioni;
- vista registro immersioni/stampa.

Rischio: medio.

## Priorita' 2 - Stabilizzare la stampa del registro immersioni

La stampa e' una parte delicata perche' dipende da:

- larghezza colonne;
- numero righe;
- cambio pagina;
- piu' servizi nello stesso giorno;
- riepilogo finale;
- contenuti presi dal servizio giornaliero.

Azioni consigliate:

- mantenere un modello dati intermedio prima di generare il documento;
- preparare casi reali di prova, ad esempio giorni con uno o piu' servizi;
- verificare sempre copertina, pagine giornaliere e riepilogo;
- evitare modifiche grafiche troppo grandi in un solo commit.

## Priorita' 3 - Test e controlli automatici

Al momento il controllo principale e' la build. Sarebbe utile aggiungere alcuni test mirati sulle parti piu' importanti.

Test utili:

- calcoli contabilita';
- filtri tabella mensile;
- raggruppamento registro immersioni per giorno e numero ordine servizio;
- cataloghi attivi/non attivi;
- salvataggio e ricarica tariffe.

Obiettivo: non coprire tutto subito, ma proteggere le funzioni piu' facili da rompere.

## Priorita' 4 - Pulizia XAML

`MainWindow.xaml` e' cresciuto molto. Quando possibile, conviene estrarre blocchi ripetuti o intere sezioni.

Azioni consigliate:

- individuare sezioni lunghe e autonome;
- creare controlli dedicati;
- mantenere lo stile visuale esistente;
- evitare rifacimenti grafici non necessari.

## Priorita' 5 - Migliorie future

Possibili miglioramenti non urgenti:

- gestione piu' guidata delle impostazioni;
- conferme prima di modificare cataloghi importanti;
- esportazione o anteprima piu' controllata delle stampe;
- backup dati piu' visibile;
- controlli di validazione piu' chiari nei form.

## Metodo di lavoro consigliato

Per ogni sessione:

1. scegliere una sola area;
2. fare modifiche piccole;
3. eseguire `dotnet build`;
4. provare manualmente il flusso interessato;
5. fare commit con messaggio chiaro;
6. fare push solo quando la modifica e' stabile.

## Prossima attivita' consigliata

Proseguire con una nuova estrazione XAML oppure iniziare una separazione prudente della logica del `MainWindowViewModel`.

Possibili prossimi passi:

- estrarre la vista `Servizio giornaliero`, con maggiore cautela perche' e' piu' grande;
- estrarre eventualmente la vista `Scheda personale`, con cautela perche' contiene sotto-tab e molti form;
- individuare proprieta' e comandi legati alle impostazioni;
- spostarli in una classe dedicata o in un servizio;
- mantenere invariato il comportamento visibile;
- verificare build e salvataggio cataloghi/tariffe.
