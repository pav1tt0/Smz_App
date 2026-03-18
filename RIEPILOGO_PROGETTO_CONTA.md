# Progetto Conta

## Obiettivo

Realizzare un programma locale per Windows dedicato alla gestione della contabilita' delle immersioni del Nucleo SMZ della Polizia di Stato di La Spezia.

Il programma dovra' permettere di:

- registrare le immersioni
- associare gli operatori partecipanti
- gestire i dati contabili collegati alle immersioni
- produrre riepiloghi
- esportare i dati per comunicazioni con altri uffici

## Piattaforma prevista

Sistema operativo target:

- Windows 10
- Windows 11

Scelta consigliata per lo sviluppo:

- usare preferibilmente un PC con Windows 11

Motivo:

- il progetto previsto e' una applicazione desktop Windows
- l'ambiente Windows e' il piu' adatto per sviluppo, compilazione e test completi

## Tecnologia consigliata

Stack consigliato:

- C#
- .NET 8
- WPF
- SQLite

Motivazione sintetica:

- `WPF` e' adatto a un gestionale desktop Windows
- `SQLite` consente di usare un database locale in un singolo file
- il programma puo' funzionare offline
- l'architettura e' adatta a future estensioni

Alternativa piu' semplice:

- `WinForms`

Uso consigliato solo se si vuole la massima rapidita' di realizzazione a scapito di una struttura grafica piu' moderna.

## Funzioni principali del programma

### 1. Gestione operatori

Dati previsti:

- matricola
- nome
- cognome
- qualifica
- reparto o ufficio
- stato operativo
- note

### 2. Gestione immersioni

Dati previsti:

- data immersione
- localita'
- tipologia immersione
- durata
- profondita' massima
- missione o intervento
- mezzo impiegato
- note operative

### 3. Collegamento immersioni-operatori

Per ogni immersione dovra' essere possibile associare uno o piu' operatori con:

- ruolo
- durata riconosciuta
- eventuali note

### 4. Contabilita'

Per ogni operatore collegato a una immersione potranno essere registrate una o piu' voci contabili:

- indennita'
- rimborso
- altre voci
- mese di competenza
- anno di competenza
- stato della contabilizzazione
- note amministrative

### 5. Report ed esportazioni

Formati previsti:

- Excel
- CSV
- PDF

Tipi di export utili:

- riepilogo per operatore
- riepilogo mensile
- riepilogo per ufficio
- esportazione tecnica per altri sistemi

## Struttura dati iniziale suggerita

### Tabella `Operatori`

- Id
- Matricola
- Nome
- Cognome
- Qualifica
- Reparto
- Attivo
- Note

### Tabella `Immersioni`

- Id
- DataImmersione
- Localita
- Tipologia
- DurataMinuti
- ProfonditaMax
- Missione
- Mezzo
- Note

### Tabella `ImmersioneOperatori`

- Id
- ImmersioneId
- OperatoreId
- Ruolo
- DurataRiconosciutaMinuti
- Note

### Tabella `VociContabili`

- Id
- ImmersioneOperatoreId
- TipoVoce
- Importo
- MeseCompetenza
- AnnoCompetenza
- Stato
- DataRegistrazione
- Note

### Tabella `Esportazioni`

- Id
- DataEsportazione
- TipoExport
- PeriodoDa
- PeriodoA
- Destinazione
- Note

## Prime schermate previste

1. Home dashboard
2. Gestione operatori
3. Gestione immersioni
4. Dettaglio immersione con partecipanti
5. Contabilita'
6. Report ed export

## Flusso operativo previsto

1. Inserimento o aggiornamento operatori
2. Registrazione immersione
3. Associazione operatori partecipanti
4. Inserimento delle voci contabili
5. Filtri per periodo, operatore o stato
6. Produzione ed esportazione dei prospetti

## Ordine di sviluppo consigliato

1. Creazione progetto WPF
2. Definizione database SQLite
3. Schermata operatori
4. Schermata immersioni
5. Collegamento immersioni-operatori
6. Modulo contabilita'
7. Report base
8. Export Excel e CSV
9. Export PDF
10. Backup dati

## Materiale da raccogliere prima dello sviluppo

Per partire bene, raccogliere:

- file Excel usati fino a oggi
- modelli Word o PDF gia' in uso
- esempi reali di contabilizzazione
- elenco dei dati annotati per ogni immersione
- regole di calcolo di importi, indennita' o rimborsi
- formato richiesto dagli altri uffici
- eccezioni o casi particolari

## Software da installare sul PC Windows 11

Minimo consigliato:

- Visual Studio 2022 Community
- workload `.NET desktop development`
- .NET 8 SDK
- Git

Utili ma non obbligatori:

- DB Browser for SQLite
- 7-Zip

## Nota operativa per la prossima chat

Quando il materiale sara' disponibile, il passo successivo sara':

1. analizzare i documenti attuali
2. separare cio' che va mantenuto da cio' che va migliorato
3. definire il modello dati definitivo
4. avviare la scrittura del software

In quella fase il codice verra' sviluppato direttamente nella cartella del progetto.
