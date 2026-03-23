# Specifica Abilitazioni

Documento di lavoro per mantenere allineati:

- nomi visualizzati
- ordine nel menu
- campi obbligatori
- profondita suggerite
- certificati o livelli
- scadenze

Compilare o correggere questo file e usarlo come riferimento unico per aggiornare il catalogo del programma.

## Legenda

- `Richiede livello`: il campo testuale o guidato e obbligatorio
- `Livelli suggeriti`: valori proposti nel menu
- `Richiede profondita`: la profondita e obbligatoria
- `Profondita suggerite`: valori proposti nel menu
- `Richiede scadenza`: la data scadenza e obbligatoria

## Catalogo Attuale

| Ordine | Codice | Nome visualizzato | Categoria | Richiede livello | Livelli suggeriti | Richiede profondita | Profondita suggerite | Richiede scadenza | Note |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | ARA | Sommozzatore abilitato ARA | Subacquea | No |  | Si | 39, 60 | No |  |
| 2 | ARO | Sommozzatore abilitato ARO | Subacquea | No |  | No |  | No |  |
| 3 | ARM | Sommozzatore abilitato ARM | Subacquea | No |  | Si | 24, 54 | No |  |
| 4 | ASAS | Sommozzatore abilitato ASAS | Subacquea | No |  | Si | 15, 30 | No |  |
| 5 | EOR | EOR | Subacquea | No |  | No |  | No |  |
| 6 | TECNICO_IPERBARICO | Tecnico iperbarico | Tecnica | No |  | No |  | No |  |
| 7 | CINE_FOTO | Cine-foto operatore | Tecnica | No |  | No |  | No |  |
| 8 | BLSD | BLS-D | Sanitaria | No |  | No |  | Si |  |
| 9 | COMANDANTE_COSTIERO | Comandante costiero | Nautica | No |  | No |  | No |  |
| 10 | MOTORISTA_NAVALE | Motorista navale | Nautica | No |  | No |  | No |  |
| 11 | ISTRUTTORE_TIRO | Istruttore di tiro | Istruttore | No |  | No |  | No |  |
| 12 | ISTRUTTORE_TECNICHE_OPERATIVE | Istruttore tecniche operative | Istruttore | No |  | No |  | No |  |
| 13 | ISTRUTTORE_DIFESA_PERSONALE | Istruttore difesa personale | Istruttore | No |  | No |  | No |  |
| 14 | CORDA | Esperto manovratore di corda | Tecnica | No |  | No |  | No |  |
| 15 | MAESTRO_NUOTO | Maestro nuoto | Didattica | No |  | No |  | No |  |
| 16 | ISTRUTTORE_NUOTO | Istruttore nuoto | Didattica | No |  | No |  | No |  |
| 17 | ASSISTENTE_BAGNANTI | Assistente bagnanti | Didattica | No |  | No |  | Si |  |
| 18 | CONDUTTORE_ACQUASCOOTER | Conduttore acquascooter | Nautica | No |  | No |  | No |  |
| 19 | PATENTE_GUIDA | Patente di guida | Guida | Si | 1, 2, 3, 4, 5 | No |  | Si | Il campo livello viene usato come certificato patente |
| 20 | GRU_BANDIERA | Abilitazione utilizzo gru bandiera | Mezzi | No |  | No |  | No |  |
| 21 | GRU_SEMOVENTE | Abilitazione gru semovente | Mezzi | No |  | No |  | No |  |
| 22 | ALPINISTA | Alpinista | Tecnica | No |  | No |  | No | Attualmente in fondo all'elenco |

## Modifiche Da Fare

Usa questa sezione per annotare cosa vuoi cambiare prima di aggiornare il programma.

Esempi:

- rinominare una voce
- cambiare l'ordine
- aggiungere o togliere profondita
- rendere obbligatoria una scadenza
- aggiungere valori guidati per un certificato

## Note Operative

- Il programma oggi usa questo catalogo per il menu di inserimento e per il filtro abilitazioni.
- Le profondita guidate sono gia attive per `ARA`, `ARM` e `ASAS`.
- Per `Patente di guida` e gia attiva la scelta guidata del certificato da `1` a `5`.
