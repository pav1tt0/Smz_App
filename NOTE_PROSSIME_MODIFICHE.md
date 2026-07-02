# Note prossime modifiche SMZ

## SMZ esterni / occasionali

Decisione da riprendere alla prossima sessione.

- Non inserirli nell'anagrafica principale.
- Gestirli dentro il servizio giornaliero in una sezione dedicata, simile all'assistenza occasionale.
- Nome sezione suggerito: `SMZ esterni / occasionali`.
- Campi previsti:
  - qualifica
  - cognome e nome
  - reparto / sede di appartenenza
  - presente
  - apparato
  - profondita
  - fascia
  - ore immersione
  - categoria contabile
  - note
- Devono comparire dopo il personale interno.
- Non devono entrare nello scadenziario.
- Non devono sporcare l'anagrafica stabile.
- Da decidere prima dell'implementazione: se devono generare importi nella contabilita mensile oppure solo comparire su foglio servizio e registro immersioni.

Ragionamento: sono visite saltuarie di sommozzatori della Polizia in forza ad altri reparti; serve contabilizzare o almeno registrare le immersioni senza trasformarli in personale interno stabile.

## Distribuzione finale del programma

Promemoria per quando prepareremo i file definitivi.

- Opzione leggera: cartella con circa 25-40 file, 35-50 MB, richiede .NET Desktop Runtime 8 gia installato sul PC.
- Opzione completa consigliata: cartella self-contained win-x64, circa 80-150 file, 120-180 MB, non richiede installazioni .NET separate sui PC destinatari.
- Opzione file unico: un eseguibile principale da circa 90-160 MB, piu eventuali cartelle dati/backup/export create dall'app.
- Scelta consigliata per uso reale: pacchetto ZIP con cartella completa self-contained win-x64, piu affidabile su PC diversi e piu semplice da diagnosticare rispetto al file unico.
