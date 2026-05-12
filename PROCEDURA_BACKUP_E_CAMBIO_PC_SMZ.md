# Procedura backup e cambio PC SMZ

Questa procedura serve agli operatori che usano SMZ Conta senza competenze tecniche.

## Regola principale

Non affidarsi solo al PC dove gira il programma.

Il programma salva i dati in automatico sul PC, ma per proteggersi da guasto, furto, formattazione o cambio computer bisogna configurare anche una cartella di backup esterno.

Cartelle consigliate:

- chiavetta USB dedicata
- disco esterno
- cartella OneDrive o altra cartella sincronizzata approvata dall'ufficio
- cartella di rete accessibile dal reparto

## Prima configurazione

1. Aprire SMZ Conta.
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

1. Aprire SMZ Conta.
2. Andare in `Stampe e report`.
3. Premere `Crea backup esterno`.
4. Verificare che il file `.smzbak` sia presente nella cartella esterna.

Sul nuovo PC:

1. Installare o copiare SMZ Conta.
2. Aprire il programma.
3. Andare in `Stampe e report`.
4. Premere `Ripristina da backup`.
5. Selezionare il file `.smzbak` piu recente.
6. Confermare il ripristino.
7. Riconfigurare la cartella backup esterno.

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
