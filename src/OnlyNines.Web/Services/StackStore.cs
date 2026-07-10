using Npgsql;

namespace OnlyNines.Web.Services;

/// <summary>
/// Persists saved assessments (the raw pasted inventory) under a short slug.
/// Reports are re-scored on read, so they stay current with the SLA dataset.
/// Disabled gracefully when no connection string is configured (local dev without DB).
/// </summary>
public sealed class StackStore
{
    private readonly string? _connectionString;
    private bool _tableEnsured;

    public bool Enabled => _connectionString is not null;

    public StackStore(IConfiguration config) =>
        _connectionString = config.GetConnectionString("Postgres") ?? config["POSTGRES_CONNECTION"];

    public async Task<string> SaveAsync(string payload)
    {
        if (!Enabled) throw new InvalidOperationException("StackStore is not configured.");
        await EnsureTableAsync();

        var slug = NewSlug();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO stacks (slug, payload) VALUES (@slug, @payload)", conn);
        cmd.Parameters.AddWithValue("slug", slug);
        cmd.Parameters.AddWithValue("payload", payload);
        await cmd.ExecuteNonQueryAsync();
        return slug;
    }

    public async Task<string?> GetAsync(string slug)
    {
        if (!Enabled) return null;
        await EnsureTableAsync();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT payload FROM stacks WHERE slug = @slug", conn);
        cmd.Parameters.AddWithValue("slug", slug);
        return await cmd.ExecuteScalarAsync() as string;
    }

    private async Task EnsureTableAsync()
    {
        if (_tableEnsured) return;
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS stacks (
                slug    text PRIMARY KEY,
                payload text NOT NULL,
                created timestamptz NOT NULL DEFAULT now()
            )
            """, conn);
        await cmd.ExecuteNonQueryAsync();
        _tableEnsured = true;
    }

    private static string NewSlug()
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
        return string.Concat(Enumerable.Range(0, 10)
            .Select(_ => alphabet[Random.Shared.Next(alphabet.Length)]));
    }
}
