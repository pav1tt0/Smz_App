# Revisione foglio di servizio giornaliero — 29 ottobre 2026

Il foglio di servizio mantiene i campi e le dichiarazioni del modello esistente, con un'impaginazione piu leggibile su A4 verticale.

## Modifiche

- Corpo del testo da 9 a 12 unita WPF (da 6,75 a 9 punti tipografici); riepilogo da 7 a 10,5 unita WPF.
- Margini laterali piu ampi, interlinea esplicita e intestazione piu leggibile.
- Tabelle con bordi sottili, intestazioni grigio chiaro e righe bianche.
- Riepilogo di straordinario, presenze e immersioni a tutta larghezza. Unita navale e localita fuori sede sono riportate in una tabella distinta con la stessa dichiarazione.
- Dichiarazione economica, riepilogo, fuori sede e firma raggruppati per evitare la firma isolata su una pagina.
- Numero di pagina e riferimento all'ordine di servizio su ogni pagina.

I calcoli di straordinario, presenze, ore e profondita non sono stati modificati. Registro immersioni e contabilita mensile non sono stati modificati.

## File

- `SMZ.Conta.App/Printing/ServizioGiornalieroPrintService.cs`: impaginazione e numerazione.
- `scripts/SMZ.PrintPreview/`: generatore riproducibile di anteprime con dati di esempio e database dedicato.
- `artifacts/stampe/2026-10-29/confronto.html`: confronto visivo locale prima/dopo.
- `artifacts/stampe/2026-10-29/prima/`: immagini e XPS precedenti alla modifica.
- `artifacts/stampe/2026-10-29/dopo/`: immagini, XPS e testo delle pagine aggiornate.

La cartella `artifacts` e esclusa da Git; codice, generatore e questo documento restano tra i file del progetto. La distro gia consegnata non e stata rigenerata.

## Verifiche

- Esaminati visivamente l'esempio ordinario e le due pagine dell'esempio esteso.
- Esempio ordinario: una pagina A4, anche con area stampabile ridotta a 760 × 1080 unita WPF.
- Esempio esteso: due pagine con 12 operatori esterni aggiuntivi e note lunghe.
- Nove scenari di impaginazione, comprese note da 0 a 36 righe: controllati testo delle attivita, note, nominativi, numerazione e presenza della firma sulla stessa pagina della dichiarazione economica.
- Compilazione Debug della soluzione completata con zero errori; tutti i 13 test della suite generale superati.
- Avviso NuGet `NU1900`: il controllo online delle vulnerabilita non e stato disponibile per le restrizioni di rete dell'ambiente.
- Verifica effettuata su documenti impaginati ed esportati in XPS; non e stata effettuata una stampa fisica. La paginazione effettiva dipende dall'area stampabile del dispositivo scelto.

Per rigenerare le anteprime aggiornate dalla cartella del progetto:

```powershell
dotnet run --project scripts/SMZ.PrintPreview/SMZ.PrintPreview.csproj -- artifacts/stampe/2026-10-29/dopo
```

Il generatore usa soltanto il database `db-esempio` nella cartella di output e non accede ai profili dati operativi o della distro.
