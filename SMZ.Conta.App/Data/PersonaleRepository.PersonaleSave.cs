using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
    public int SavePersonale(Personale personale, bool isNewRecord)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        int perId;
        if (isNewRecord)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO Personale (
                    PerId,
                    Cognome,
                    Nome,
                    Qualifica,
                    DataDecorrenzaQualifica,
                    ProfiloPersonale,
                    RuoloSanitario,
                    CodiceFiscale,
                    MatricolaPersonale,
                    NumeroBrevettoSmz,
                    StatoServizio,
                    DataFineServizio,
                    DataNascita,
                    LuogoNascita,
                    ViaResidenza,
                    CapResidenza,
                    CittaResidenza,
                    Telefono1,
                    Telefono2,
                    Mail1Utente,
                    Mail2Utente)
                VALUES (
                    $perId,
                    $cognome,
                    $nome,
                    $qualifica,
                    $dataDecorrenzaQualifica,
                    $profiloPersonale,
                    $ruoloSanitario,
                    $codiceFiscale,
                    $matricolaPersonale,
                    $numeroBrevettoSmz,
                    $statoServizio,
                    $dataFineServizio,
                    $dataNascita,
                    $luogoNascita,
                    $viaResidenza,
                    $capResidenza,
                    $cittaResidenza,
                    $telefono1,
                    $telefono2,
                    $mail1Utente,
                    $mail2Utente);
                """;
            AddPersonaleParameters(insert, personale);
            insert.Parameters.AddWithValue("$perId", personale.PerId);
            insert.ExecuteNonQuery();
            perId = personale.PerId;
        }
        else
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText =
                """
                UPDATE Personale
                SET Cognome = $cognome,
                    Nome = $nome,
                    Qualifica = $qualifica,
                    DataDecorrenzaQualifica = $dataDecorrenzaQualifica,
                    ProfiloPersonale = $profiloPersonale,
                    RuoloSanitario = $ruoloSanitario,
                    CodiceFiscale = $codiceFiscale,
                    MatricolaPersonale = $matricolaPersonale,
                    NumeroBrevettoSmz = $numeroBrevettoSmz,
                    StatoServizio = $statoServizio,
                    DataFineServizio = $dataFineServizio,
                    DataNascita = $dataNascita,
                    LuogoNascita = $luogoNascita,
                    ViaResidenza = $viaResidenza,
                    CapResidenza = $capResidenza,
                    CittaResidenza = $cittaResidenza,
                    Telefono1 = $telefono1,
                    Telefono2 = $telefono2,
                    Mail1Utente = $mail1Utente,
                    Mail2Utente = $mail2Utente
                WHERE PerId = $perId;
                """;
            AddPersonaleParameters(update, personale);
            update.Parameters.AddWithValue("$perId", personale.PerId);
            update.ExecuteNonQuery();
            perId = personale.PerId;

            DeleteChildRows(connection, transaction, "PersonaleAbilitazioni", perId);
            DeleteChildRows(connection, transaction, "VisiteMediche", perId);
            DeleteChildRows(connection, transaction, "PersonaleAttagliamento", perId);
        }

        InsertAbilitazioni(connection, transaction, perId, personale.Abilitazioni);
        InsertVisite(connection, transaction, perId, personale.VisiteMediche);
        InsertAttagliamento(connection, transaction, perId, personale.Attagliamento);
        transaction.Commit();

        return perId;
    }

    public long DeletePersonale(int perId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        var usage = GetPersonaleServiceUsage(connection, transaction, perId);
        if (usage.HasReferences)
        {
            throw new InvalidOperationException(
                $"La scheda con PerID {perId} non puo essere archiviata perche e collegata a servizi o immersioni gia salvati. " +
                $"Riferimenti trovati: {usage.ServiziComePartecipante} servizi come partecipante, {usage.ImmersioniConRuoli} immersioni con ruoli assegnati. " +
                "Elimina o modifica prima i servizi collegati, poi riprova.");
        }

        var archivedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var archiveId = ArchivePersonale(connection, transaction, perId, archivedAt);
        ArchiveAbilitazioni(connection, transaction, archiveId, perId);
        ArchiveVisite(connection, transaction, archiveId, perId);
        ArchiveAttagliamento(connection, transaction, archiveId, perId);

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM Personale WHERE PerId = $perId;";
        command.Parameters.AddWithValue("$perId", perId);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException($"Scheda con PerID {perId} non trovata.");
        }

        transaction.Commit();
        return archiveId;
    }

    public void DeletePersonaleDefinitivo(int perId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        var usage = GetPersonaleServiceUsage(connection, transaction, perId);
        if (usage.HasReferences)
        {
            throw new InvalidOperationException(
                $"La scheda con PerID {perId} non puo essere eliminata definitivamente perche e collegata a servizi o immersioni gia salvati. " +
                $"Riferimenti trovati: {usage.ServiziComePartecipante} servizi come partecipante, {usage.ImmersioniConRuoli} immersioni con ruoli assegnati.");
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM Personale WHERE PerId = $perId;";
        command.Parameters.AddWithValue("$perId", perId);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException($"Scheda con PerID {perId} non trovata.");
        }

        transaction.Commit();
    }

    public int RestorePersonaleArchivio(long archiveId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        var archived = GetArchivioById(connection, transaction, archiveId);
        if (archived is null)
        {
            throw new InvalidOperationException("Scheda archiviata non trovata.");
        }

        if (ExistsActiveCodiceFiscale(connection, transaction, archived.CodiceFiscale))
        {
            throw new InvalidOperationException(
                $"Esiste gia una scheda attiva con codice fiscale {archived.CodiceFiscale}. Ripristino bloccato.");
        }

        var perIdDaRipristinare = ExistsActivePerId(connection, transaction, archived.PerIdOriginale)
            ? GetNextAvailablePerId(connection, transaction)
            : archived.PerIdOriginale;

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText =
            """
            INSERT INTO Personale (
                PerId,
                Cognome,
                Nome,
                Qualifica,
                DataDecorrenzaQualifica,
                ProfiloPersonale,
                RuoloSanitario,
                CodiceFiscale,
                MatricolaPersonale,
                NumeroBrevettoSmz,
                StatoServizio,
                DataFineServizio,
                DataNascita,
                LuogoNascita,
                ViaResidenza,
                CapResidenza,
                CittaResidenza,
                Telefono1,
                Telefono2,
                Mail1Utente,
                Mail2Utente
            )
            VALUES (
                $perId,
                $cognome,
                $nome,
                $qualifica,
                $dataDecorrenzaQualifica,
                $profiloPersonale,
                $ruoloSanitario,
                $codiceFiscale,
                $matricolaPersonale,
                $numeroBrevettoSmz,
                $statoServizio,
                $dataFineServizio,
                $dataNascita,
                $luogoNascita,
                $viaResidenza,
                $capResidenza,
                $cittaResidenza,
                $telefono1,
                $telefono2,
                $mail1Utente,
                $mail2Utente
            );
            """;
        insert.Parameters.AddWithValue("$perId", perIdDaRipristinare);
        insert.Parameters.AddWithValue("$cognome", archived.Cognome);
        insert.Parameters.AddWithValue("$nome", archived.Nome);
        insert.Parameters.AddWithValue("$qualifica", DbText(archived.Qualifica));
        insert.Parameters.AddWithValue("$dataDecorrenzaQualifica", ToDbValue(archived.DataDecorrenzaQualifica));
        insert.Parameters.AddWithValue("$profiloPersonale", ProfiliPersonaleCatalogo.Normalizza(archived.ProfiloPersonale));
        insert.Parameters.AddWithValue("$ruoloSanitario", DbText(archived.RuoloSanitario));
        insert.Parameters.AddWithValue("$codiceFiscale", archived.CodiceFiscale);
        insert.Parameters.AddWithValue("$matricolaPersonale", DbText(archived.MatricolaPersonale));
        insert.Parameters.AddWithValue("$numeroBrevettoSmz", DbText(archived.NumeroBrevettoSmz));
        insert.Parameters.AddWithValue("$statoServizio", StatoServizioPersonaleCatalogo.Normalizza(archived.StatoServizio));
        insert.Parameters.AddWithValue("$dataFineServizio", ToDbValue(archived.DataFineServizio));
        insert.Parameters.AddWithValue("$dataNascita", ToDbValue(archived.DataNascita));
        insert.Parameters.AddWithValue("$luogoNascita", DbText(archived.LuogoNascita));
        insert.Parameters.AddWithValue("$viaResidenza", DbText(archived.ViaResidenza));
        insert.Parameters.AddWithValue("$capResidenza", DbText(archived.CapResidenza));
        insert.Parameters.AddWithValue("$cittaResidenza", DbText(archived.CittaResidenza));
        insert.Parameters.AddWithValue("$telefono1", DbText(archived.Telefono1));
        insert.Parameters.AddWithValue("$telefono2", DbText(archived.Telefono2));
        insert.Parameters.AddWithValue("$mail1Utente", DbText(archived.Mail1Utente));
        insert.Parameters.AddWithValue("$mail2Utente", DbText(archived.Mail2Utente));
        insert.ExecuteNonQuery();

        RestoreAbilitazioniArchivio(connection, transaction, archiveId, perIdDaRipristinare);
        RestoreVisiteArchivio(connection, transaction, archiveId, perIdDaRipristinare);
        RestoreAttagliamentoArchivio(connection, transaction, archiveId, perIdDaRipristinare);

        DeleteArchivio(connection, transaction, archiveId);
        transaction.Commit();

        return perIdDaRipristinare;
    }

    public void DeletePersonaleArchivio(long archiveId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        DeleteArchivio(connection, transaction, archiveId);
        transaction.Commit();
    }
}
