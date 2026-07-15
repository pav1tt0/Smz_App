using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed class AccessService
{
    public const int MinimumPasswordLength = 10;
    private const int PasswordIterations = 310_000;
    private const int SaltSize = 32;
    private const int HashSize = 32;

    public bool HasUsers()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM UtentiAccesso LIMIT 1);";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
    }

    public bool HasUser(int perId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM UtentiAccesso WHERE PerId = $perId);";
        command.Parameters.AddWithValue("$perId", perId);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
    }

    public AccessSession CreateFirstAdministrator(int perId, string password)
    {
        if (HasUsers())
        {
            throw new InvalidOperationException("Il primo amministratore e gia stato configurato.");
        }

        ValidatePerId(perId);
        ValidatePassword(password, perId);
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        InsertUser(connection, transaction, perId, AccessRole.Administrator, password, false);
        transaction.Commit();
        return new AccessSession(perId, GetDisplayName(connection, perId), AccessRole.Administrator, false);
    }

    public AccessSession Authenticate(int perId, string password)
    {
        ValidatePerId(perId);
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT PasswordHash, PasswordSalt, Iterazioni, Ruolo, Attivo, CambioPasswordRichiesto
            FROM UtentiAccesso WHERE PerId = $perId;
            """;
        command.Parameters.AddWithValue("$perId", perId);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException("PerID o password non corretti.");
        }

        if (reader.GetInt32(4) == 0)
        {
            throw new InvalidOperationException("Questo account e sospeso. Contattare un amministratore.");
        }

        var expectedHash = Convert.FromBase64String(reader.GetString(0));
        var salt = Convert.FromBase64String(reader.GetString(1));
        var actualHash = HashPassword(password, salt, reader.GetInt32(2));
        if (!CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
        {
            throw new InvalidOperationException("PerID o password non corretti.");
        }

        var role = AccessRoleExtensions.ParseDatabaseValue(reader.GetString(3));
        var mustChangePassword = reader.GetInt32(5) == 1;
        reader.Close();

        using var update = connection.CreateCommand();
        update.CommandText = "UPDATE UtentiAccesso SET UltimoAccesso = $ora WHERE PerId = $perId;";
        update.Parameters.AddWithValue("$ora", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        update.Parameters.AddWithValue("$perId", perId);
        update.ExecuteNonQuery();
        return new AccessSession(perId, GetDisplayName(connection, perId), role, mustChangePassword);
    }

    public IReadOnlyList<AccessUserSummary> GetUsers()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT u.PerId,
                   COALESCE(NULLIF(TRIM(p.Cognome || ' ' || p.Nome), ''), 'PerID ' || u.PerId),
                   u.Ruolo, u.Attivo, u.CambioPasswordRichiesto, u.UltimoAccesso
            FROM UtentiAccesso u
            LEFT JOIN Personale p ON p.PerId = u.PerId
            ORDER BY p.Cognome, p.Nome, u.PerId;
            """;
        var result = new List<AccessUserSummary>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new AccessUserSummary
            {
                PerId = reader.GetInt32(0),
                DisplayName = reader.GetString(1),
                Role = AccessRoleExtensions.ParseDatabaseValue(reader.GetString(2)),
                IsActive = reader.GetInt32(3) == 1,
                MustChangePassword = reader.GetInt32(4) == 1,
                LastAccess = reader.IsDBNull(5) ? null : DateTime.Parse(
                    reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToLocalTime(),
            });
        }

        return result;
    }

    public void CreateUser(int administratorPerId, int perId, AccessRole role, string temporaryPassword)
    {
        EnsureAdministrator(administratorPerId);
        ValidatePerId(perId);
        ValidatePassword(temporaryPassword, perId);
        using var connection = OpenConnection();
        if (!PersonaleExists(connection, perId))
        {
            throw new InvalidOperationException("Il PerID indicato non e presente nell'anagrafica del personale.");
        }

        using var transaction = connection.BeginTransaction();
        InsertUser(connection, transaction, perId, role, temporaryPassword, true);
        transaction.Commit();
    }

    public void ChangePassword(int perId, string currentPassword, string newPassword)
    {
        _ = Authenticate(perId, currentPassword);
        SetPassword(perId, newPassword, false);
    }

    public void ResetPassword(int administratorPerId, int targetPerId, string temporaryPassword)
    {
        EnsureAdministrator(administratorPerId);
        SetPassword(targetPerId, temporaryPassword, true);
    }

    public void SetUserActive(int administratorPerId, int targetPerId, bool isActive)
    {
        EnsureAdministrator(administratorPerId);
        if (administratorPerId == targetPerId && !isActive)
        {
            throw new InvalidOperationException("Non puoi sospendere l'account con cui hai effettuato l'accesso.");
        }

        using var connection = OpenConnection();
        var target = GetUserState(connection, targetPerId);
        if (!isActive && target.Role == AccessRole.Administrator && target.IsActive && CountActiveAdministrators(connection) <= 1)
        {
            throw new InvalidOperationException("Non e possibile sospendere l'ultimo amministratore attivo.");
        }

        ExecuteUserUpdate(connection, targetPerId, "Attivo", isActive ? 1 : 0);
    }

    public void SetUserRole(int administratorPerId, int targetPerId, AccessRole role)
    {
        EnsureAdministrator(administratorPerId);
        if (administratorPerId == targetPerId && role != AccessRole.Administrator)
        {
            throw new InvalidOperationException("Non puoi modificare il ruolo dell'account con cui hai effettuato l'accesso.");
        }

        using var connection = OpenConnection();
        var target = GetUserState(connection, targetPerId);
        if (role == AccessRole.Base && target.Role == AccessRole.Administrator && target.IsActive
            && CountActiveAdministrators(connection) <= 1)
        {
            throw new InvalidOperationException("Non e possibile declassare l'ultimo amministratore attivo.");
        }

        ExecuteUserUpdate(connection, targetPerId, "Ruolo", role.ToDatabaseValue());
    }

    private void SetPassword(int perId, string password, bool mustChangePassword)
    {
        ValidatePassword(password, perId);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = HashPassword(password, salt, PasswordIterations);
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE UtentiAccesso
            SET PasswordHash = $hash, PasswordSalt = $salt, Iterazioni = $iterazioni,
                CambioPasswordRichiesto = $cambio, AggiornatoIl = $ora
            WHERE PerId = $perId;
            """;
        command.Parameters.AddWithValue("$hash", Convert.ToBase64String(hash));
        command.Parameters.AddWithValue("$salt", Convert.ToBase64String(salt));
        command.Parameters.AddWithValue("$iterazioni", PasswordIterations);
        command.Parameters.AddWithValue("$cambio", mustChangePassword ? 1 : 0);
        command.Parameters.AddWithValue("$ora", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$perId", perId);
        if (command.ExecuteNonQuery() != 1)
        {
            throw new InvalidOperationException("Account non trovato.");
        }
    }

    private static void InsertUser(SqliteConnection connection, SqliteTransaction transaction, int perId,
        AccessRole role, string password, bool mustChangePassword)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = HashPassword(password, salt, PasswordIterations);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO UtentiAccesso (
                PerId, PasswordHash, PasswordSalt, Iterazioni, Ruolo, Attivo,
                CambioPasswordRichiesto, CreatoIl, AggiornatoIl)
            VALUES ($perId, $hash, $salt, $iterazioni, $ruolo, 1, $cambio, $ora, $ora);
            """;
        command.Parameters.AddWithValue("$perId", perId);
        command.Parameters.AddWithValue("$hash", Convert.ToBase64String(hash));
        command.Parameters.AddWithValue("$salt", Convert.ToBase64String(salt));
        command.Parameters.AddWithValue("$iterazioni", PasswordIterations);
        command.Parameters.AddWithValue("$ruolo", role.ToDatabaseValue());
        command.Parameters.AddWithValue("$cambio", mustChangePassword ? 1 : 0);
        command.Parameters.AddWithValue("$ora", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        try
        {
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException("Esiste gia un account associato a questo PerID.", ex);
        }
    }

    private static void EnsureAdministrator(int perId)
    {
        using var connection = OpenConnection();
        var state = GetUserState(connection, perId);
        if (!state.IsActive || state.Role != AccessRole.Administrator)
        {
            throw new InvalidOperationException("Operazione riservata agli amministratori.");
        }
    }

    private static (AccessRole Role, bool IsActive) GetUserState(SqliteConnection connection, int perId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Ruolo, Attivo FROM UtentiAccesso WHERE PerId = $perId;";
        command.Parameters.AddWithValue("$perId", perId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("Account non trovato.");
        return (AccessRoleExtensions.ParseDatabaseValue(reader.GetString(0)), reader.GetInt32(1) == 1);
    }

    private static void ExecuteUserUpdate(SqliteConnection connection, int perId, string columnName, object value)
    {
        if (columnName is not ("Attivo" or "Ruolo")) throw new ArgumentOutOfRangeException(nameof(columnName));
        using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE UtentiAccesso SET {columnName} = $value, AggiornatoIl = $ora WHERE PerId = $perId;";
        command.Parameters.AddWithValue("$value", value);
        command.Parameters.AddWithValue("$ora", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$perId", perId);
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Account non trovato.");
    }

    private static int CountActiveAdministrators(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM UtentiAccesso WHERE Ruolo = 'Amministratore' AND Attivo = 1;";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static bool PersonaleExists(SqliteConnection connection, int perId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM Personale WHERE PerId = $perId);";
        command.Parameters.AddWithValue("$perId", perId);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
    }

    private static string GetDisplayName(SqliteConnection connection, int perId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT TRIM(Cognome || ' ' || Nome) FROM Personale WHERE PerId = $perId;";
        command.Parameters.AddWithValue("$perId", perId);
        return command.ExecuteScalar() as string is { Length: > 0 } name ? name : $"PerID {perId}";
    }

    private static byte[] HashPassword(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);

    private static void ValidatePerId(int perId)
    {
        if (perId <= 0) throw new InvalidOperationException("Inserire un PerID numerico valido.");
    }

    private static void ValidatePassword(string password, int perId)
    {
        if (password.Length < MinimumPasswordLength)
            throw new InvalidOperationException($"La password deve contenere almeno {MinimumPasswordLength} caratteri.");
        if (password == perId.ToString(CultureInfo.InvariantCulture))
            throw new InvalidOperationException("La password non puo coincidere con il PerID.");
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(DatabasePaths.ConnectionString);
        connection.Open();
        return connection;
    }
}
