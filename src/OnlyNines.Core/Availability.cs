namespace OnlyNines.Core;

/// <summary>The math. Small on purpose — this is the part everyone will audit.</summary>
public static class Availability
{
    public const double HoursPerYear = 8760.0;

    /// <summary>Serial chain: all components are hard dependencies. Worst-case model.</summary>
    public static double Serial(IEnumerable<double> slas) =>
        slas.Aggregate(1.0, (acc, s) => acc * s);

    /// <summary>
    /// Redundant set: available if ANY member is available.
    /// Upper bound — shared-fate risks (region, config, deploys) are not modeled.
    /// </summary>
    public static double Parallel(IEnumerable<double> slas) =>
        1 - slas.Aggregate(1.0, (acc, s) => acc * (1 - s));

    public static double DowntimeHoursPerYear(double sla) => (1 - sla) * HoursPerYear;

    /// <summary>"How many nines" — 0.9995 → 3.3. Because we only count nines.</summary>
    public static double Nines(double sla) =>
        sla >= 1 ? double.PositiveInfinity : -Math.Log10(1 - sla);

    /// <summary>
    /// Display formatting that never lies: adds precision instead of rounding
    /// an SLA below 1.0 up to "100". Nothing on this site may ever say 100%.
    /// </summary>
    public static string Percent(double sla)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var text = (sla * 100).ToString("0.###", ci);
        if (sla < 1 && text.StartsWith("100"))
            text = (sla * 100).ToString("0.######", ci);
        return text;
    }
}
