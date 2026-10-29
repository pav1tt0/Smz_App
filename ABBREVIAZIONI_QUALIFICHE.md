# Abbreviazioni delle qualifiche

Sigle indicate dall'utente il 29 ottobre 2026. Le qualifiche restano memorizzate con il nome esteso; le sigle sono disponibili per le visualizzazioni compatte tramite `QualificaFormatter.Abbrevia`.

| Qualifica | Sigla |
| --- | --- |
| Agente | Agt. |
| Agente Scelto | Agt.S. |
| Assistente | Ass. |
| Assistente Capo | Ass.C. |
| Assistente Capo Coordinatore | Ass.C.C. |
| Vice Sovrintendente | V.Sov. |
| Sovrintendente | Sov. |
| Sovrintendente Capo | Sov.C. |
| Sovrintendente Capo Coordinatore | Sov.C.C. |
| Vice Ispettore | V.Isp. |
| Ispettore | Isp. |
| Ispettore Capo | Isp.C. |
| Ispettore Superiore | Isp.S. |
| Sostituto Commissario | Sost.C. |
| Sostituto Commissario Coordinatore | Sost.C.C. |
| Vice Commissario | V.Comm. |
| Commissario | Comm. |
| Commissario Capo | Comm.C. |
| Vice Questore Aggiunto | V.Q.A. |
| Vice Questore | V.Q. |
| Primo Dirigente | P.D. |

Il catalogo include anche Vice Questore e Primo Dirigente, con il relativo ordinamento gerarchico. Sono riconosciute sia le forme con `Coordinatore` sia quelle gia presenti con `C.`.

Per le qualifiche tecniche gia presenti, la sigla e ottenuta aggiungendo `T.` alla sigla corrispondente: ad esempio `Isp.T.`, `Isp.C.T.`, `Sost.C.C.T.`. Il catalogo include inoltre `MED.C.`, `MED.P.`, `MED.` e `INF.` per le qualifiche sanitarie.

Le sigle sono usate nella selezione del personale della scheda servizio. Passando il mouse sulla sigla compare il nome esteso. Gli altri punti del programma mantengono la precedente visualizzazione, finche non viene richiesto di adottare anche li le nuove sigle.

Riferimenti: `SMZ.Conta.App/Models/QualificaFormatter.cs` e `SMZ.Conta.App/ViewModels/ServizioPartecipanteDraftViewModel.cs`. I test verificano anche che l'ordinamento gerarchico non dipenda dalle nuove sigle.
