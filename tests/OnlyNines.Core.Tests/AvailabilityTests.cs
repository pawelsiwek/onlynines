using OnlyNines.Core;
using Xunit;

namespace OnlyNines.Core.Tests;

public class AvailabilityTests
{
    // The canonical example from the conference talk: cheap WordPress on Azure.
    // App Service 99.95% x MySQL 99.9% x Storage LRS 99.9% = 99.75% -> ~21.9 h/yr.
    [Fact]
    public void Serial_CheapWordPress_Is_99_75()
    {
        var sla = Availability.Serial(new[] { 0.9995, 0.999, 0.999 });
        Assert.Equal(0.99750, sla, 4);
        Assert.Equal(21.9, Availability.DowntimeHoursPerYear(sla), 1);
    }

    [Fact]
    public void Serial_Hardened_Is_99_97()
    {
        var sla = Availability.Serial(new[] { 0.9999, 0.9999, 0.9999 });
        Assert.Equal(0.9997, sla, 4);
        Assert.Equal(2.6, Availability.DowntimeHoursPerYear(sla), 1);
    }

    [Fact]
    public void Parallel_TwoNodes_Beats_Either()
    {
        var sla = Availability.Parallel(new[] { 0.999, 0.999 });
        Assert.Equal(0.999999, sla, 6);
    }

    [Fact]
    public void Parallel_EmptySet_IsZero()
    {
        Assert.Equal(0, Availability.Parallel(Array.Empty<double>()), 10);
    }

    // Regression: SQL DB zone-redundant (99.995%) displayed as "100.00%" with F2.
    // Nothing on this site may ever say 100%.
    [Theory]
    [InlineData(0.99995, "99.995")]
    [InlineData(0.9999999, "99.99999")]
    [InlineData(0.9995, "99.95")]
    [InlineData(0.999, "99.9")]
    public void Percent_NeverRoundsUpTo100(double sla, string expected)
    {
        Assert.Equal(expected, Availability.Percent(sla));
    }

    [Theory]
    [InlineData(0.999, 3.0)]
    [InlineData(0.9995, 3.3)]
    [InlineData(0.9999, 4.0)]
    public void Nines_CountsNines(double sla, double expected)
    {
        Assert.Equal(expected, Availability.Nines(sla), 1);
    }
}
