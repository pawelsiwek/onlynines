using System.Text.Json;

namespace OnlyNines.Core;

/// <summary>
/// Parses Azure Resource Graph output for kql/inventory.kql. Accepts:
/// - JSON array of rows (portal "Results" copied programmatically),
/// - object with a "data" array (az graph query output),
/// - CSV (portal "Download as CSV").
/// Format is auto-detected.
/// </summary>
public static class ResourceGraphParser
{
    private static readonly string[] CoreFields = { "id", "name", "resourceGroup", "type", "location" };

    public static List<AzureResource> Parse(string input)
    {
        var trimmed = input.TrimStart('﻿', ' ', '\t', '\r', '\n');
        if (trimmed.Length == 0)
            throw new InvalidDataException("Empty input.");
        return trimmed[0] is '[' or '{' ? ParseJson(trimmed) : ParseCsv(trimmed);
    }

    // ---------- JSON ----------

    public static List<AzureResource> ParseJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var rows = doc.RootElement.ValueKind switch
        {
            JsonValueKind.Array => doc.RootElement,
            JsonValueKind.Object when doc.RootElement.TryGetProperty("data", out var data) => data,
            _ => throw new InvalidDataException(
                "Unrecognized input: expected a JSON array of rows or an object with a 'data' array."),
        };

        var result = new List<AzureResource>();
        foreach (var row in rows.EnumerateArray())
        {
            var cells = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in row.EnumerateObject())
                cells[prop.Name] = prop.Value;
            result.Add(BuildResource(cells));
        }
        return result;
    }

    // ---------- CSV ----------

    public static List<AzureResource> ParseCsv(string csv)
    {
        var rows = ReadCsv(csv);
        if (rows.Count < 2)
            throw new InvalidDataException("CSV has no data rows.");

        var headers = rows[0];
        var result = new List<AzureResource>();
        foreach (var row in rows.Skip(1))
        {
            if (row.Length == 1 && string.IsNullOrWhiteSpace(row[0])) continue;
            var cells = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length && i < row.Length; i++)
            {
                var value = row[i];
                if (string.IsNullOrEmpty(value) || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
                    continue;
                cells[headers[i]] = ToJsonElement(value);
            }
            if (cells.Count > 0)
                result.Add(BuildResource(cells));
        }
        return result;
    }

    /// <summary>CSV cells may hold JSON fragments (zones arrays, sku objects) — detect and parse.</summary>
    private static JsonElement ToJsonElement(string value)
    {
        var t = value.TrimStart();
        if (t.StartsWith('[') || t.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(value);
                return doc.RootElement.Clone();
            }
            catch (JsonException) { /* fall through: treat as plain string */ }
        }
        using var str = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return str.RootElement.Clone();
    }

    /// <summary>Minimal RFC-4180 reader: quoted fields, escaped quotes, newlines inside quotes.</summary>
    private static List<string[]> ReadCsv(string text)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var field = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(c);
            }
            else switch (c)
            {
                case '"': inQuotes = true; break;
                case ',': fields.Add(field.ToString()); field.Clear(); break;
                case '\r': break;
                case '\n':
                    fields.Add(field.ToString()); field.Clear();
                    rows.Add(fields.ToArray()); fields.Clear();
                    break;
                default: field.Append(c); break;
            }
        }
        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            rows.Add(fields.ToArray());
        }
        return rows;
    }

    // ---------- shared row builder ----------

    private static AzureResource BuildResource(Dictionary<string, JsonElement> cells)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, value) in cells)
        {
            if (CoreFields.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;

            switch (name.ToLowerInvariant())
            {
                case "zones":
                    var count = value.ValueKind == JsonValueKind.Array ? value.GetArrayLength() : 0;
                    attributes["zoneCount"] = count.ToString();
                    break;
                case "sku":
                    if (value.ValueKind == JsonValueKind.Object)
                        foreach (var s in value.EnumerateObject())
                            AddScalar(attributes, $"sku{Capitalize(s.Name)}", s.Value);
                    else
                        AddScalar(attributes, "sku", value);
                    break;
                default:
                    AddScalar(attributes, name, value);
                    break;
            }
        }

        return new AzureResource
        {
            Id = GetString(cells, "id") ?? Guid.NewGuid().ToString("N"),
            Name = GetString(cells, "name"),
            ResourceGroup = GetString(cells, "resourceGroup"),
            Type = GetString(cells, "type")?.ToLowerInvariant()
                   ?? throw new InvalidDataException("Row is missing 'type'."),
            Location = GetString(cells, "location"),
            Attributes = attributes,
        };
    }

    private static void AddScalar(Dictionary<string, string> attrs, string key, JsonElement value)
    {
        var s = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };
        if (!string.IsNullOrEmpty(s)) attrs[key] = s;
    }

    private static string? GetString(Dictionary<string, JsonElement> cells, string name) =>
        cells.TryGetValue(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string Capitalize(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
