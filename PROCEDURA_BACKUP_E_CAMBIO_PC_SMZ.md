# Procedura backup e cambio PC SMZ

Questa procedura serve agli operatori che usano SMZ senza competenze tecniche.

## Regola principale

Non affidarsi solo al PC dove gira il programma.

Il programma salva i dati in automatico sul PC, ma per proteggersi da guasto, furto, formattazione o cambio computer bisogna configurare anche una cartella di backup esterno.

Cartelle consigliate:

- chiavetta USB dedicata
- disco esterno
- cartella OneDrive o altra cartella sincronizzata approvata dall'ufficio
- cartella di rete accessibile dal reparto

## Prima configurazione

1. Aprire SMZ.
2. Andare in `Stampe e report`.
3. Nel riquadro `Backup dati`, premere `Configura cartella esterna`.
4. Scegliere la chiavetta, disco, cartella di rete o cartella sincronizzata.
5. Il programma crea subito un primo backup esterno.

Dopo questa configurazione, ogni salvataggio importante crea:

- un backup locale sul PC
- un backup esterno nella cartella configurata

## Controllo periodico

Una volta a settimana controllare nel riquadro `Backup dati`:

- `Ultimo backup locale`
- `Ultimo backup esterno`
- `Cartella esterna`

Se il backup esterno non e aggiornato, collegare il supporto esterno o verificare che la cartella sia raggiungibile, poi premere `Crea backup esterno`.

## Cambio PC

Sul vecchio PC:

1. Aprire SMZ.
2. Andare in `Stampe e report`.
3. Premere `Crea backup esterno`.
4. Verificare che il file `.smzbak` sia presente nella cartella esterna.

Sul nuovo PC:

1. Estrarre il pacchetto di installazione per dati reali.
2. Avviare `INSTALLA-E-AVVIA-SMZ.cmd`.
3. Se il database reale e gia presente e viene riconosciuto, scegliere `SI` per usarlo.
4. Altrimenti scegliere il file `.smzbak` salvato sul nuovo PC, oppure il database `.db` reale. Il file sorgente resta conservato; un altro database gia presente non viene sovrascritto.
5. L'installatore crea una copia di sicurezza, configura il database e aggiunge il collegamento `SMZ` al desktop e al menu Start.
6. Accedere con gli account presenti nel database. Se non ce ne sono, creare il primo amministratore usando un PerID dell'anagrafica reale.
7. Verificare personale, servizi e contabilita, quindi riconfigurare la cartella backup esterno.

Il pacchetto operativo non contiene dati di esempio. Non occorre creare prima un'anagrafica fittizia per poter ripristinare il backup.

## Cosa contiene il backup

Il file `.smzbak` contiene:

- database principale
- anagrafica
- abilitazioni e visite mediche
- servizi e immersioni
- contabilita ed elaborazioni mensili
- cataloghi e tariffe
- export gia prodotti, se presenti nella cartella export dell'app

## Regola pratica

Prima di interventi importanti, cambio PC, assistenza tecnica o aggiornamenti: premere sempre `Crea backup esterno`.
