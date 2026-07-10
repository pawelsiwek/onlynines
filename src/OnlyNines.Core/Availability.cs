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
}
