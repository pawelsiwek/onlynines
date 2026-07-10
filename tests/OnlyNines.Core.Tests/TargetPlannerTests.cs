using OnlyNines.Core;
using Xunit;

namespace OnlyNines.Core.Tests;

public class TargetPlannerTests
{
    private static SlaService Db => DatasetLoader.Load("""
        service: DB
        resourceType: t/db
        variants:
          - id: zr-ha
            match: { ha: ZoneRedundant }
            sla: 0.9999
          - id: no-ha
            match: {}
            sla: 0.999
        ladder: [no-ha, zr-ha]
        """);

    private static SlaService App => DatasetLoader.Load("""
        service: App
        resourceType: t/app
        variants:
          - id: zr
            match: { zr: true }
            sla: 0.9999
          - id: single
            match: {}
            sla: 0.9995
        ladder: [single, zr]
        """);

    private static ScoredResource Score(string type, Dictionary<string, string>? attrs = null)
    {
        var scorer = new Scorer(new[] { Db, App });
        return scorer.Score(new AzureResource
        {
            Id = type, Type = type,
            Attributes = attrs ?? new(StringComparer.OrdinalIgnoreCase),
        });
    }

    [Fact]
    public void Under_CutsRecommendationsAtTarget()
    {
        // 0.999 * 0.9995 = 0.9985; production target 0.999.
        // Upgrading only the DB (0.9999) gives 0.99940 -> target met with ONE upgrade.
        var members = new[] { Score("t/db"), Score("t/app") };
        var plan = TargetPlanner.Plan(members, 0.999);

        Assert.Equal(TargetVerdict.Under, plan.Verdict);
        var needed = Assert.Single(plan.NeededUpgrades);
        Assert.Equal("t/db", needed.Resource.Type);
        Assert.Single(plan.BeyondTarget); // the app upgrade is NOT justified by the target
        Assert.True(plan.SlaAfterNeeded >= 0.999);
    }

    [Fact]
    public void Over_FindsDowngradeCandidates()
    {
        // Everything maxed: 0.9999 * 0.9999 = 0.9998; blog target 0.99.
        var members = new[]
        {
            Score("t/db", new(StringComparer.OrdinalIgnoreCase) { ["ha"] = "ZoneRedundant" }),
            Score("t/app", new(StringComparer.OrdinalIgnoreCase) { ["zr"] = "true" }),
        };
        var plan = TargetPlanner.Plan(members, 0.99);

        Assert.Equal(TargetVerdict.Over, plan.Verdict);
        Assert.Equal(2, plan.DowngradeCandidates.Count);
        Assert.Empty(plan.NeededUpgrades);
    }

    [Fact]
    public void Unreachable_WhenLadderTopsOutBelowTarget()
    {
        // Max composite 0.9998 < mission-critical 0.9999 -> redesign, not upgrades.
        var members = new[] { Score("t/db"), Score("t/app") };
        var plan = TargetPlanner.Plan(members, 0.9999);

        Assert.Equal(TargetVerdict.Unreachable, plan.Verdict);
    }

    [Fact]
    public void OnTarget_NoNoise()
    {
        var members = new[] { Score("t/app") }; // 0.9995 vs target 0.9995
        var plan = TargetPlanner.Plan(members, 0.9995);

        Assert.Equal(TargetVerdict.OnTarget, plan.Verdict);
        Assert.Empty(plan.NeededUpgrades);
        Assert.Empty(plan.DowngradeCandidates); // downgrade would drop below target
    }
}
