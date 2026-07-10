# OnlyNines

[![OnlyNines](https://onlynines.app/badge/q0wd42nn3i.svg)](https://onlynines.app/stack/q0wd42nn3i)
[![ci](https://github.com/pawelsiwek/onlynines/actions/workflows/ci.yml/badge.svg)](https://github.com/pawelsiwek/onlynines/actions/workflows/ci.yml)

> **we only count nines** · [onlynines.app](https://onlynines.app)

Composite SLA calculator + Azure environment assessment from **a single KQL query**. No login, no credentials, no agent.

Paste one query into Azure Resource Graph Explorer → export the JSON → get back the *designed availability* of your actual environment: composite SLA per application, hours-of-downtime-per-year, your weakest links ranked by impact, and what to change to gain the next nine.

## How it works

1. Run [`kql/inventory.kql`](kql/inventory.kql) in [Azure Resource Graph Explorer](https://portal.azure.com/#view/HubsExtension/ArgQueryBlade) (or `az graph query -q @kql/inventory.kql`)
2. Export the result as JSON
3. `dotnet run --project src/OnlyNines.Cli -- report.json`

```
Resource group: rg-myshop-prod          composite SLA: 99.750%   downtime: 21.9 h/yr
  weakest links:
    1. mysql-myshop          (no HA)              -8.8 h/yr
    2. app-myshop            (single instance)    -4.4 h/yr
```

## The honest math

- Default model is **worst-case serial**: every scored resource in a group is assumed to be a hard dependency. `SLA = Π SLAᵢ`.
- Redundant sets use `1 − Π(1 − SLAᵢ)` — an **upper bound**; shared-fate risks (region, config, deployments) are not modeled.
- Resources we can't confidently map are reported in an `unknown` bucket, never silently skipped.
- Designed availability ≠ financially-backed SLA ≠ observed uptime. See [docs/methodology.md](docs/methodology.md).

## Privacy

Anonymization happens **before your data leaves the machine**: use [`kql/inventory-paranoid.kql`](kql/inventory-paranoid.kql) to hash all identifiers at query time, or the default query + client-side hashing in the web app (server never sees resource names).

## Repository layout

```
src/OnlyNines.Core/          # scoring engine (C#, YamlDotNet only)
src/OnlyNines.Cli/           # CLI: onlynines report.json
src/OnlyNines.Web/           # Blazor web app (landing + calculator)
tests/OnlyNines.Core.Tests/  # xUnit
data/sla/                    # SLA dataset (YAML) ← community PRs welcome!
kql/                         # the queries
docs/                        # methodology, spec, design
```

Run the web app locally: `dotnet run --project src/OnlyNines.Web`

## Dataset disclaimer

SLA values in `data/sla/` are drafts pending verification against the [current Microsoft SLA documents](https://www.microsoft.com/licensing/docs/view/Service-Level-Agreements-SLA-for-Online-Services). Every file carries a `lastVerified` date; PRs updating values with a source link are the most welcome contribution.

## What's here

- **Assessment** ([/assess](https://onlynines.app/assess)) — paste one Resource Graph export (JSON or CSV), get the composite SLA of your environment, weakest links ranked by downtime cost, and an honest unknown bucket.
- **Criticality verdict** — declare how critical the workload is (Well-Architected style) and get one of: *under target* (with the shortest upgrade list that gets you there — and where to stop), *on target* (don't touch it), or *over-engineered* (what you can downgrade without breaking the target).
- **Permalinks & live badges** — save an assessment, embed `![OnlyNines](https://onlynines.app/badge/<slug>.svg)` in your README; reports re-score against the current dataset on every visit.
- **Calculator** ([/calculator](https://onlynines.app/calculator)), **SLA dataset browser** ([/sla](https://onlynines.app/sla)), downtime tables per nine ([/nines/99.9](https://onlynines.app/nines/99.9)).
- **Infra as code** — `infra/` provisions the deliberately-cheap production (App Service B1 + PostgreSQL Burstable, no HA — on purpose) via bicep + GitHub Actions OIDC.

## Next

- WebAssembly for assess/calculator, so "runs in your browser" is literally true
- External uptime monitoring + chaos-kill history on [/status](https://onlynines.app/status)
- Dataset verification pass against current Microsoft SLA documents (values are drafts)

## License

MIT © Paweł Siwek
