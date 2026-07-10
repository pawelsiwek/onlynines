# Methodology (a.k.a. "come at me")

OnlyNines computes **designed availability** — what your architecture is *capable of* on paper. It is deliberately simple, and we document every simplification so you can attack the model instead of the numbers.

## The model

1. **Grouping.** By default, one resource group = one application. This is wrong often enough that the UI lets you regroup. Grouping is your statement about what "the app" is; we just provide a default.

2. **Serial composition (default).** Every scored resource in a group is treated as a hard dependency: `SLA = Π SLAᵢ`. This is the worst case. Real architectures have soft dependencies (a cache that degrades instead of failing) — modeling that requires knowledge we don't have. Worst case is honest; optimistic guesses are not.

3. **Parallel sets (opt-in).** Resources you mark as redundant compose as `1 − Π(1 − SLAᵢ)`. This is an **upper bound**: it assumes independent failures. Zone-redundant pairs share a region, a config, a deployment pipeline, and a 2 a.m. hotfix. We print this caveat on every report because it's the most common way availability math lies.

4. **Unknown bucket.** Anything we can't map to a dataset variant is listed, not skipped. A report that silently ignores half your environment is marketing.

## What the numbers are NOT

- **Not a financially-backed SLA.** Microsoft's SLA is a refund policy, not a physics model.
- **Not observed uptime.** Your monitoring knows what actually happened; we compute what the architecture permits.
- **Not a prediction.** Most downtime is caused by deployments and configuration, which no architecture diagram captures.

## Dataset

`data/sla/*.yaml` maps resource type + configuration to an SLA value with a source link and a `lastVerified` date. CI fails when a file goes stale (>90 days). Values are community-maintained; PRs with a source link are the canonical contribution.

## Known limitations (v1)

- Per-resource scoring can't see set-level SLAs (e.g., two VMs across zones = 99.99% *as a set*). The redundant-set flow handles this manually; auto-suggestion is planned.
- Regional pairing / multi-region failover is not modeled yet.
- Storage SLA varies by access tier and read/write path; we use simplified read-path values.
