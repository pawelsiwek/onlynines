using OnlyNines.Core;
using Xunit;

namespace OnlyNines.Core.Tests;

public class ScorerTests
{
    private static SlaService AppService => DatasetLoader.Load("""
        service: Azure App Service
        resourceType: microsoft.web/serverfarms
        variants:
          - id: zone-redundant
            match: { zr: true }
            sla: 0.9999
          - id: single-instance
            match: {}
            sla: 0.9995
        ladder: [single-instance, zone-redundant]
        """);

    private static AzureResource Plan(bool zoneRedundant) => new()
    {
        Id = "/sub/x/plan1",
        Name = "plan1",
        ResourceGroup = "rg-test",
        Type = "microsoft.web/serverfarms",
        Attributes = { ["zr"] = zoneRedundant ? "true" : "false" },
    };

    [Fact]
    public void FirstMatchWins_SpecificBeforeCatchAll()
    {
        var scorer = new Scorer(new[] { AppService });
        Assert.Equal("zone-redundant", scorer.Score(Plan(true)).Variant!.Id);
        Assert.Equal("single-instance", scorer.Score(Plan(false)).Variant!.Id);
    }

    [Fact]
    public void UnknownType_IsNotScored_ButReported()
    {
        var scorer = new Scorer(new[] { AppService });
        var scored = scorer.Score(new AzureResource { Id = "x", Type = "microsoft.unknown/things" });
        Assert.False(scored.IsScored);
        Assert.Null(scored.Service);
    }

    [Fact]
    public void NextRung_PointsUpTheLadder()
    {
        var scorer = new Scorer(new[] { AppService });
        var scored = scorer.Score(Plan(false));
        Assert.Equal("zone-redundant", scored.NextRung!.Id);
    }

    [Fact]
    public void ListMatch_HasAnyOfSemantics()
    {
        var service = DatasetLoader.Load("""
            service: Test
            resourceType: t/t
            variants:
              - id: premium
                match: { tier: [PremiumV3, PremiumV4] }
                sla: 0.9999
              - id: rest
                match: {}
                sla: 0.999
            """);
        var scorer = new Scorer(new[] { service });
        var res = new AzureResource
        {
            Id = "1", Type = "t/t",
            Attributes = { ["tier"] = "premiumv4" },
        };
        Assert.Equal("premium", scorer.Score(res).Variant!.Id);
    }

    [Fact]
    public void IgnoredType_IsNotApplicable_NotUnknown_NotScored()
    {
        var ignored = DatasetLoader.Load("""
            service: SQL logical server
            resourceType: microsoft.sql/servers
            ignore: true
            ignoreReason: SLA applies to the databases.
            """);
        var scorer = new Scorer(new[] { AppService, ignored });
        var groups = scorer.ScoreEnvironment(new[]
        {
            Plan(false),
            new AzureResource { Id = "x", Name = "sqlsrv", ResourceGroup = "rg-test", Type = "microsoft.sql/servers" },
        });
        var g = Assert.Single(groups);
        Assert.Single(g.NotApplicable);
        Assert.Empty(g.Unknown);
        Assert.Equal(0.9995, g.CompositeSla, 6); // only the plan counts
    }

    [Fact]
    public void GroupScore_IsWorstCaseSerial()
    {
        var scorer = new Scorer(new[] { AppService });
        var groups = scorer.ScoreEnvironment(new[] { Plan(false), Plan(false) });
        var g = Assert.Single(groups);
        Assert.Equal(0.9995 * 0.9995, g.CompositeSla, 8);
    }
}
