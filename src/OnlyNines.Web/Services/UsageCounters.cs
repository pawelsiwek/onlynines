using Npgsql;

namespace OnlyNines.Web.Services;

/// <summary>
/// Zero-PII product counters persisted in Postgres and shown publicly on /status.
/// One row per (day, kind). Complements OTEL metrics: this is the public tally,
/// OTEL is the operational signal.
/// </summary>
public sealed class UsageCounters
{
    private readonly string? _connectionString;
    private bool _tableEnsured;

    public bool Enabled => _connectionString is not null;

    public UsageCounters(IConfiguration config) =>
        _connectionString = config.GetConnectionString("Postgres") ?? config["POSTGRES_CONNECTION"];

    /// <summary>Fire-and-forget friendly: never throws.</summary>
    public async Task IncrementAsync(string kind)
    {
        if (!Enabled) return;
        try
        {
            await EnsureTableAsync();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO usage_counters (day, kind, count) VALUES (CURRENT_DATE, @kind, 1)
                ON CONFLICT (day, kind) DO UPDATE SET count = usage_counters.count + 1
                """, conn);
            cmd.Parameters.AddWithValue("kind", kind);
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Counters must never take the product down. The irony would be unbearable.
        }
    }

    public async Task<IReadOnlyDictionary<string, long>> TotalsAsync()
    {
        var result = new Dictionary<string, long>();
        if (!Enabled) return result;
        try
        {
            await EnsureTableAsync();
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT kind, SUM(count) FROM usage_counters GROUP BY kind", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result[reader.GetString(0)] = reader.GetInt64(1);
        }
        catch { /* see above */ }
        return result;
    }

    private async Task EnsureTableAsync()
    {
        if (_tableEnsured) return;
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS usage_counters (
                day   date NOT NULL,
                kind  text NOT NULL,
                count bigint NOT NULL DEFAULT 0,
                PRIMARY KEY (day, kind)
            )
            """, conn);
        await cmd.ExecuteNonQueryAsync();
        _tableEnsured = true;
    }
}
