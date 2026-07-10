using System.Text.Json;

namespace OnlyNines.Web.Services;

/// <summary>
/// Envelope for saved stacks: raw inventory + user choices (criticality).
/// Legacy payloads (raw JSON/CSV) unwrap with null criticality.
/// </summary>
public sealed record StackPayload(string Input, string? Criticality)
{
    private sealed record Envelope(int Onlynines, string? Criticality, string Input);

    public string Wrap() =>
        JsonSerializer.Serialize(new Envelope(2, Criticality, Input),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    public static StackPayload Unwrap(string payload)
    {
        var trimmed = payload.TrimStart();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.TryGetProperty("onlynines", out _) &&
                    doc.RootElement.TryGetProperty("input", out var input))
                {
                    string? criticality = doc.RootElement.TryGetProperty("criticality", out var c)
                        && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
                    return new StackPayload(input.GetString() ?? "", criticality);
                }
            }
            catch (JsonException) { /* legacy raw payload */ }
        }
        return new StackPayload(payload, null);
    }
}
