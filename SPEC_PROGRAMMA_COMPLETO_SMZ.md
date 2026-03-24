# Specifica Programma Completo SMZ

Documento di lavoro per definire la struttura completa del gestionale SMZ prima di estendere il software oltre l'anagrafica.

Obiettivo:

- partire dai modelli reali oggi usati in Word/Excel
- costruire un database unico e coerente
- generare da dati strutturati registro, contabilita, indennita e stampe

## Moduli Principali

### 1. Anagrafica Personale

Contiene i dati stabili della persona:

- PerID
- cognome
- nome
- qualifica
- codice fiscale
- data e luogo di nascita
- indirizzo
- contatti
- abilitazioni
- visite mediche
- stato operativo

Ruolo:

- archivio anagrafico unico
- base per assegnazione ai servizi
- base per verifiche operative e scadenze

### 2. Foglio Di Servizio Giornaliero

E il modulo centrale del programma.

Per ogni giornata devono essere registrati:

- data
- giorno settimana
- localita
- scopo immersione
- servizio svolto a bordo unita navale
- indennita fuori sede si/no
- attivita svolta
- note generali
- eventuale servizio in sede / fuori sede

Da questo modulo devono poi derivare:

- registro immersioni mensile
- contabilità immersioni
- indennita supplementare assistenza
- indennita supplementare fuori sede
- riepiloghi mensili

### 3. Immersioni Del Servizio

Ogni servizio giornaliero puo contenere zero, una o piu immersioni.

Nel modello attuale si vedono due immersioni in testata, ma nel database conviene non imporre il limite a due.

Per ogni immersione:

- numero progressivo nel servizio
- orario
- direttore immersione
- operatore soccorso
- assistenza BLSD
- assistenza sanitaria
- localita specifica se diversa da quella del servizio
- scopo immersione se diverso da quello del servizio

### 4. Partecipazione Personale

Per ogni servizio si registra il personale impiegato.

Dati minimi:

- persona
- gruppo operativo
- presente si/no
- ruolo operativo
- note partecipazione

Scelta progettuale consigliata:

- non salvare `X / - / ASS.` come dato principale
- salvare `Presente = Si/No`
- salvare separatamente il `RuoloOperativo`

In stampa:

- presente -> `X`
- assente -> `-`
- eventuale assistenza -> derivata dal ruolo, non dal campo presenza

### 5. Dati Tecnici Di Impiego In Immersione

Per ogni persona coinvolta in una immersione servono anche i dati tecnici usati nel registro e nella contabilita:

- tipologia apparato
- profondita
- fascia profondita
- ore immersione
- categoria ore

Questi dati non appartengono in modo stabile alla persona, ma alla singola attivita svolta in quel giorno.

### 6. Elaborazione Mensile

Modulo di chiusura automatica del mese.

Output previsti:

- registro immersioni mensile
- contabilità immersioni per persona
- indennita assistenza SMZ
- indennita supplementare fuori sede
- riepilogo servizi fuori sede

### 7. Stampe E Report

Modelli da generare dal sistema:

- foglio di servizio giornaliero
- registro immersioni
- prospetto contabilità immersioni
- prospetto indennita assistenza
- prospetto indennita fuori sede
- riepiloghi per persona / mese / anno

### 8. Backup E Ripristino

Da implementare a valle della struttura completa.

Il backup dovra comprendere:

- database completo
- eventuali allegati futuri
- cataloghi e configurazioni

## Entita Principali Del Database

### Personale

Una riga per ogni dipendente.

Campi principali:

- `PersonaleId`
- `PerId`
- `Cognome`
- `Nome`
- `Qualifica`
- `CodiceFiscale`
- `Matricola`
- `DataNascita`
- `LuogoNascita`
- `ViaResidenza`
- `CapResidenza`
- `CittaResidenza`
- `Telefono1`
- `Telefono2`
- `MailPoliziaUtente`
- `MailPersonale`
- `Attivo`
- `Note`

### ServizioGiornaliero

Testata del servizio.

Campi principali:

- `ServizioGiornalieroId`
- `DataServizio`
- `GiornoSettimana`
- `TipoServizio`
- `LocalitaId`
- `ScopoImmersioneId`
- `UnitaNavaleId`
- `FuoriSede`
- `AttivitaSvolta`
- `Note`
- `CreatoIl`
- `AggiornatoIl`

Note:

- `TipoServizio` puo essere `InSede`, `FuoriSede`, `Misto`
- `FuoriSede` resta utile anche se il tipo servizio e gia valorizzato, per aderire ai modelli attuali

### ServizioImmersione

Una riga per ogni immersione registrata all'interno del servizio.

Campi principali:

- `ServizioImmersioneId`
- `ServizioGiornalieroId`
- `NumeroImmersione`
- `OrarioInizio`
- `OrarioFine`
- `DirettoreImmersionePersonaleId`
- `OperatoreSoccorsoPersonaleId`
- `AssistenteBlsdPersonaleId`
- `AssistenteSanitarioPersonaleId`
- `LocalitaId`
- `ScopoImmersioneId`
- `Note`

### ServizioPartecipante

Una riga per ogni persona assegnata al servizio.

Campi principali:

- `ServizioPartecipanteId`
- `ServizioGiornalieroId`
- `PersonaleId`
- `GruppoOperativoId`
- `Presente`
- `RuoloOperativoId`
- `Note`

Esempi:

- OSSP
- OSSALC
- Assistenza sanitaria
- Supporto

### ServizioPartecipanteImmersione

Dettaglio tecnico di una persona in una immersione specifica.

Campi principali:

- `ServizioPartecipanteImmersioneId`
- `ServizioImmersioneId`
- `ServizioPartecipanteId`
- `TipologiaImmersioneId`
- `ProfonditaMetri`
- `FasciaProfonditaId`
- `OreImmersione`
- `CategoriaContabileOreId`
- `Note`

Motivo:

- una persona puo essere presente nel servizio ma non aver effettuato immersione
- una persona puo essere coinvolta in immersione 1 ma non in immersione 2
- le regole contabili vanno applicate a questo livello

### RegolaContabileImmersione

Catalogo delle tariffe.

Campi principali:

- `RegolaContabileImmersioneId`
- `TipologiaImmersioneId`
- `FasciaProfonditaId`
- `CategoriaContabileOreId`
- `Tariffa`
- `DataInizioValidita`
- `DataFineValidita`
- `Attiva`

### ElaborazioneMensile

Testata di chiusura del mese.

Campi principali:

- `ElaborazioneMensileId`
- `Anno`
- `Mese`
- `Stato`
- `GenerataIl`
- `Note`

### ElaborazioneMensileRiga

Risultato economico per persona e mese.

Campi principali:

- `ElaborazioneMensileRigaId`
- `ElaborazioneMensileId`
- `PersonaleId`
- `TotOreOrd`
- `TotOreAdd`
- `TotOreSper`
- `TotOreCameraIperbarica`
- `TotImportoImmersioni`
- `TotGiorniAssistenza`
- `TotGiorniFuoriSede`
- `TotaleComplessivo`

## Cataloghi Da Gestire Nel Database

I valori delle tendine non devono stare nel codice.

### Catalogo Localita

Esempi rilevati dai modelli:

- BASE NAVALE (SP)
- B.N. - SENO DI PANIGAGLIA (SP)
- LA SPEZIA (SP)
- ISOLA DEL TINO (SP)
- ISOLA DEL TINETTO (SP)
- PORTOVENERE (SP)
- RIOMAGGIORE (SP)
- MANAROLA (SP)
- CORNIGLIA (SP)
- VERNAZZA (SP)
- MONTEROSSO (SP)
- LERICI (SP)
- LERICI (SP) - Diga Foranea
- SARZANA (SP)
- AMEGLIA (SP)
- ALASSIO (SV)
- BARI (BA)
- BOLOGNA (BO)
- COMO (CO)
- FIRENZE (FI)
- GENOVA (GE)
- GROSSETO (GR)
- IMPERIA (IM)
- LIVORNO (LI)
- MASSA-CARRARA (MS)
- MODENA (TN)
- NAPOLI (NA)
- PALERMO (PA)
- PISA (PI)
- ROMA (RM)
- SAVONA (SV)
- SASSARI (SS)

Campi consigliati:

- `LocalitaId`
- `Descrizione`
- `Provincia`
- `Attiva`
- `Ordine`

### Catalogo Scopo Immersione

Esempi rilevati dai modelli:

- ADDESTRATIVA IN B.D.
- ADDESTRATIVA A MARE
- ADDESTRATIVA IN BACINO LACUALE IN ALTITUDINE
- IMMERSIONE IN CAMERA IPERBARICA
- ASSISTENZA SUB. MANIFESTAZIONI SPORTIVE/CULTURALI
- ASSISTENZA SUB. MANIFESTAZIONI RELIGIOSE
- COLLABORAZIONE CON ALTRI ENTI
- DIMOSTRAZIONE/RAPPRESENTANZA SPECIALITA
- MANUTENZIONE/CONTROLLO CATENARIE D'ORMEGGIO
- MANTENIMENTO BREVETTO OPER. SUB. ALTRI UFFICI
- RICERCA/RECUPERO P.G.
- RICERCA ARCHEOLOGIA
- RICERCA/RECUPERO MATERIALI DISPERSI
- RICERCA RESIDUATI BELLICI
- RIPRESE VIDEO/SERVIZIO TELEVISIVO
- SICUREZZA E PREVENZIONE SUBACQUEA
- SPERIMENTAZIONE E COLLAUDO ATTREZZATURA SUBACQUEA
- SVOLGIMENTO STAGE PROPEDEUTICO AL CORSO O.S.S.P.
- TUTELA AMBIENTALE
- PROVE FUNZIONALI ATTREZZATURA SUBACQUEA
- VIGILANZA PARCHI MARINI E PREVENZIONE PESCA DI FRODO
- ALTRO
- ASSISTENZA SUB PROVE SELETTIVE CORSO SMZ
- SVOLGIMENTO ASSISTENZA CORSO RIQUALIFICA SMZ
- PROVE FUNZIONALI PER ESERCITAZIONE SUBACQUEA

Campi consigliati:

- `ScopoImmersioneId`
- `Descrizione`
- `CategoriaRegistroId`
- `Attiva`
- `Ordine`

### Catalogo Categoria Registro

Serve per il riepilogo finale del registro mensile.

Valori iniziali consigliati:

- Immersioni addestrative a mare e in bacino delimitato
- Immersioni ordinarie
- Immersioni per sperimentazione attrezzature e materiali subacquei
- Immersioni in camera iperbarica
- Altro

### Catalogo Unita Navale

Esempi rilevati dai modelli:

- P.S. 1230 arimar
- P.S. 1190 stilmar
- P.S. 1162 stilmar
- P.S. 1347 orion
- P.S. 1174 MDN
- P.S. 1287 zodiac420
- P.S. 1289 zodiac470
- P.S. 1437 med55
- P.S. 1438 med55
- P.S. 1272 vizianello
- P.S. 1447 Whally

Campi consigliati:

- `UnitaNavaleId`
- `Descrizione`
- `Sigla`
- `Attiva`
- `Ordine`

### Catalogo Tipologia Immersione

Valori rilevati dai modelli:

- A.R.A./ASAS
- A.R.O.
- A.R.M.
- C.I.

Campi consigliati:

- `TipologiaImmersioneId`
- `Codice`
- `Descrizione`
- `Attiva`
- `Ordine`

### Catalogo Fascia Profondita

Valori rilevati dall'Excel contabile:

- 00/12
- 13/25
- 26/40
- 41/55
- 56/80

Campi consigliati:

- `FasciaProfonditaId`
- `Descrizione`
- `MetriDa`
- `MetriA`
- `Attiva`
- `Ordine`

### Catalogo Categoria Contabile Ore

Valori rilevati dall'Excel contabile:

- ORE ORD
- ORE ADD
- ORE SPER
- ORE C.I.

Campi consigliati:

- `CategoriaContabileOreId`
- `Codice`
- `Descrizione`
- `Attiva`
- `Ordine`

### Catalogo Gruppo Operativo

Valori iniziali consigliati:

- OSSP
- OSSALC
- Assistenza sanitaria
- Supporto

### Catalogo Ruolo Operativo

Valori iniziali consigliati:

- Operatore
- Assistenza
- Sanitario
- Direttore immersione
- Operatore soccorso
- BLSD
- Supporto

## Regole Funzionali Di Base

### Regola 1

Il servizio giornaliero e l'unica fonte primaria dei dati operativi.

### Regola 2

Registro immersioni, contabilità e indennita mensili devono essere prodotti automaticamente dai servizi giornalieri.

### Regola 3

Le tendine devono essere gestite come cataloghi modificabili, non come valori fissi nel codice.

### Regola 4

`Presenza` deve essere un dato semplice e affidabile:

- `Si`
- `No`

Eventuali ruoli o funzioni devono stare in campi dedicati.

### Regola 5

Le regole economiche devono essere versionabili nel tempo per gestire eventuali cambi normativi o tariffari.

## Flusso Operativo Consigliato

1. Si inserisce o si apre il servizio giornaliero.
2. Si selezionano localita, scopo, unita navale e fuori sede.
3. Si registra il personale impiegato.
4. Si registrano le immersioni del giorno.
5. Per ogni immersione si valorizzano i dati tecnici per il personale coinvolto.
6. A fine mese si genera l'elaborazione mensile.
7. Dall'elaborazione si producono stampe e report.

## Ordine Di Sviluppo Consigliato

### Fase 1

- cataloghi base
- schema database nuovo
- modulo servizio giornaliero

### Fase 2

- partecipanti al servizio
- immersioni del servizio
- dati tecnici immersione

### Fase 3

- generazione registro mensile
- riepilogo categorie immersione

### Fase 4

- contabilità immersioni
- indennita assistenza
- indennita fuori sede

### Fase 5

- stampe finali aderenti ai modelli in uso
- backup e ripristino completi

## Note Aperte

Da confermare nelle prossime revisioni:

- se il campo `giorno` va calcolato automaticamente dalla data
- se una giornata puo avere piu servizi distinti
- se una persona puo comparire in piu ruoli nella stessa immersione
- se `ASS.` nei modelli attuali aveva un significato contabile specifico
- se il servizio fuori sede puo maturare indennita anche senza immersione
- se il foglio giornaliero SFS va gestito come modulo distinto o come variante del servizio giornaliero generale
