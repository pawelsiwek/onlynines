using System.Globalization;
using OnlyNines.Core;

namespace OnlyNines.Web.Services;

/// <summary>Shields-style two-segment badge per the brand spec.</summary>
public static class BadgeSvg
{
    public static string Render(double sla)
    {
        var nines = Availability.Nines(sla);
        var value = (sla * 100).ToString("F2", CultureInfo.InvariantCulture) + "%";
        var color = nines >= 4 ? "#1F8A5B" // strong — rare on purpose
                  : nines < 2.5 ? "#FF6B4A" // weak — the coral of shame
                  : "#00AFF0";

        const string label = "OnlyNines";
        const int charW = 7;
        var labelW = label.Length * charW + 20;
        var valueW = value.Length * charW + 20;
        var total = labelW + valueW;

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="{total}" height="24" role="img" aria-label="{label}: {value}">
              <clipPath id="r"><rect width="{total}" height="24" rx="5" fill="#fff"/></clipPath>
              <g clip-path="url(#r)">
                <rect width="{labelW}" height="24" fill="#253B52"/>
                <rect x="{labelW}" width="{valueW}" height="24" fill="{color}"/>
              </g>
              <g fill="#fff" text-anchor="middle" font-family="'JetBrains Mono','DejaVu Sans Mono',monospace" font-size="11.5">
                <text x="{labelW / 2}" y="16">{label}</text>
                <text x="{labelW + valueW / 2}" y="16" font-weight="700">{value}</text>
              </g>
            </svg>
            """;
    }
}
