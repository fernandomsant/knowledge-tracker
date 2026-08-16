using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace KnowledgeTracker.Migrations;

internal sealed class MigrationRunner(string connectionString)
{
    private const string MigrationTableName = "dbo.SchemaMigrations";
    private const string MigrationLockName = "KnowledgeTracker.Migrations";
    private readonly string _connectionString = connectionString;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var migrations = DiscoverMigrations();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await AcquireMigrationLockAsync(connection, cancellationToken);
        await EnsureHistoryTableAsync(connection, cancellationToken);

        var appliedMigrations = await LoadAppliedMigrationsAsync(connection, cancellationToken);
        foreach (var migration in migrations)
        {
            if (appliedMigrations.TryGetValue(migration.Id, out var appliedChecksum))
            {
                if (!migration.MatchesChecksum(appliedChecksum))
                {
                    throw new InvalidOperationException(
                        $"Applied migration '{migration.Id}' was modified. Create a new migration instead.");
                }

                Console.WriteLine($"Skipped {migration.Id}; it was already applied.");
                continue;
            }

            await ApplyMigrationAsync(connection, migration, cancellationToken);
            Console.WriteLine($"Applied {migration.Id}.");
        }
    }

    private static IReadOnlyList<SqlMigration> DiscoverMigrations()
    {
        var migrationsDirectory = Path.Combine(AppContext.BaseDirectory, "migrations");
        if (!Directory.Exists(migrationsDirectory))
        {
            throw new DirectoryNotFoundException($"The migrations directory was not found: {migrationsDirectory}");
        }

        return Directory.EnumerateFiles(migrationsDirectory, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .Select(path => SqlMigration.FromFile(path))
            .ToList();
    }

    private static async Task AcquireMigrationLockAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @result INT;
            EXEC @result = sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Session',
                @LockTimeout = 60000;
            SELECT @result;
            """;
        command.Parameters.AddWithValue("@resource", MigrationLockName);

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
        {
            throw new InvalidOperationException("Could not acquire the database migration lock within 60 seconds.");
        }
    }

    private static async Task EnsureHistoryTableAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF OBJECT_ID(N'{MigrationTableName}', N'U') IS NULL
            BEGIN
                CREATE TABLE {MigrationTableName}
                (
                    MigrationId NVARCHAR(255) NOT NULL,
                    Checksum CHAR(64) NOT NULL,
                    AppliedAtUtc DATETIMEOFFSET(7) NOT NULL,
                    CONSTRAINT PK_SchemaMigrations PRIMARY KEY (MigrationId)
                );
            END;
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<string, string>> LoadAppliedMigrationsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT MigrationId, Checksum FROM {MigrationTableName};";

        var appliedMigrations = new Dictionary<string, string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            appliedMigrations.Add(reader.GetString(0), reader.GetString(1));
        }

        return appliedMigrations;
    }

    private static async Task ApplyMigrationAsync(
        SqlConnection connection,
        SqlMigration migration,
        CancellationToken cancellationToken)
    {
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var migrationCommand = connection.CreateCommand())
            {
                migrationCommand.Transaction = transaction;
                migrationCommand.CommandText = migration.Sql;
                await migrationCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var historyCommand = connection.CreateCommand())
            {
                historyCommand.Transaction = transaction;
                historyCommand.CommandText = $"""
                    INSERT INTO {MigrationTableName} (MigrationId, Checksum, AppliedAtUtc)
                    VALUES (@migrationId, @checksum, SYSUTCDATETIME());
                    """;
                historyCommand.Parameters.AddWithValue("@migrationId", migration.Id);
                historyCommand.Parameters.AddWithValue("@checksum", migration.Checksum);
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private sealed record SqlMigration(
        string Id,
        string Sql,
        string Checksum,
        IReadOnlySet<string> AcceptedChecksums
    )
    {
        public bool MatchesChecksum(string checksum) => AcceptedChecksums.Contains(checksum);

        public static SqlMigration FromFile(string path)
        {
            var sql = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new InvalidOperationException($"Migration '{Path.GetFileName(path)}' is empty.");
            }

            if (Regex.IsMatch(sql, @"(?im)^\s*GO\s*(?:--.*)?$"))
            {
                throw new InvalidOperationException(
                    $"Migration '{Path.GetFileName(path)}' contains GO. Split it into executable SQL statements instead.");
            }

            var checksums = CreateAcceptedChecksums(sql);
            return new SqlMigration(
                Path.GetFileName(path),
                sql,
                ComputeChecksum(sql),
                checksums
            );
        }

        private static IReadOnlySet<string> CreateAcceptedChecksums(string sql)
        {
            var lineFeedSql = sql.Replace("\r\n", "\n").Replace("\r", "\n");
            var carriageReturnLineFeedSql = lineFeedSql.Replace("\n", "\r\n");

            return new HashSet<string>(StringComparer.Ordinal)
            {
                ComputeChecksum(sql),
                ComputeChecksum(lineFeedSql),
                ComputeChecksum(carriageReturnLineFeedSql)
            };
        }

        private static string ComputeChecksum(string sql) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
    }
}
