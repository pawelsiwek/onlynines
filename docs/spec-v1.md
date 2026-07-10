# OnlyNines — v1 Specification
### Composite SLA calculator + Azure environment assessment from a single KQL query

**Brand:** OnlyNines · **Domain:** onlynines.app (secured) · **Repo:** `pawelsiwek/onlynines` · **Tagline:** "we only count nines"
**License split:** open core (MIT) + hosted proprietary layer
**Primary goals:** organic dev traffic (programmatic SEO), lead magnet for conference sessions, production victim for the agentic-resiliency demo. Monetization: explicitly out of scope for v1.

---

## 1. Product summary

Paste one KQL query into Azure Resource Graph Explorer. Upload the JSON output to nines. Get back: the composite SLA of your actual environment, hours-of-downtime-per-year, your weakest links ranked by impact, and an upgrade ladder ("what to change to gain the next nine"). Share it as a permalink or a README badge.

**One-line pitch:** "One query. Your real SLA. No login, no credentials, no agent."

## 2. Open core vs hosted split

| Layer | What | Where |
|---|---|---|
| **OSS (MIT), repo `pawelsiwek/onlynines`** | SLA dataset (YAML), scoring library (C#, `OnlyNines.Core`), KQL queries, CLI (`onlynines report.json`, dotnet tool), Blazor web app, Bicep/azd infra | GitHub |
| **Hosted (closed)** | Upload endpoint + persistence, saved stacks/permalinks, badge serving + CDN cache, SEO page corpus, analytics, rate limiting | nines.* deployment |

Rule of thumb: anything a dev wants to run locally or fork → OSS. Anything with network effects → hosted.

## 3. Architecture (deliberately demo-compatible)

- **Web:** Blazor Web App (.NET 8+): static SSR for the SEO corpus (per-service SLA pages render server-side with schema.org), interactive WASM island for the calculator (works even when the DB is down) — hosted on **App Service B1**, which is on the Resiliency Agent supported-scripts list
- **DB:** **PostgreSQL Flexible Server B1ms** — also on the supported list; stores stacks, badge configs, dataset snapshot
- **Storage account (LRS)** — uploaded report blobs (transient, TTL 24 h)
- **CI/CD:** GitHub Actions → azd. Remediation-as-code PRs land here during the talk.
- Baseline cost target: **~$40/mo**. Every architecture upgrade during the conference demo is done ON THIS PRODUCTION APP.

Degradation modes (designed, not accidental — they ARE the demo): DB down → calculator still works client-side, permalinks/badges 503. Zone down (baseline) → everything 503.

## 4. The KQL (v1 draft)

Two variants shipped in `kql/`:

**`inventory.kql` (default):** plain names, anonymization happens client-side before upload (see §7).

```kql
resources
| where type in~ (
    'microsoft.compute/virtualmachines',
    'microsoft.compute/virtualmachinescalesets',
    'microsoft.web/serverfarms',
    'microsoft.web/sites',
    'microsoft.dbforpostgresql/flexibleservers',
    'microsoft.dbformysql/flexibleservers',
    'microsoft.sql/servers/databases',
    'microsoft.sql/managedinstances',
    'microsoft.cache/redis',
    'microsoft.storage/storageaccounts',
    'microsoft.network/loadbalancers',
    'microsoft.network/applicationgateways',
    'microsoft.network/azurefirewalls',
    'microsoft.network/frontdoors',
    'microsoft.cdn/profiles',
    'microsoft.containerservice/managedclusters',
    'microsoft.documentdb/databaseaccounts',
    'microsoft.servicebus/namespaces',
    'microsoft.eventhub/namespaces',
    'microsoft.keyvault/vaults',
    'microsoft.apimanagement/service'
)
| project id, name, resourceGroup, type = tolower(type), location, zones, sku, kind,
    ha = properties.highAvailability.mode,
    zr = properties.zoneRedundant,
    tier = properties.sku.tier,
    replication = sku.name,
    capacity = sku.capacity
```

**`inventory-paranoid.kql`:** same, but `project id = hash(id), name = hash(name), resourceGroup = hash(resourceGroup), ...` for orgs that won't paste anything identifiable even into a browser.

⚠️ **Verify:** exact hash function availability in ARG's KQL subset (`hash()` vs `hash_sha256()`), and per-type property paths (`highAvailability.mode` exists on PG/MySQL Flexible; SQL DB zone redundancy is `properties.zoneRedundant`; App Service ZR is on the *plan*, `microsoft.web/serverfarms` → `properties.zoneRedundant`). Build a fixture per resource type from a real tenant before coding the parser.

## 5. SLA dataset (`data/sla/*.yaml`)

One file per service. Schema:

```yaml
# data/sla/app-service.yaml
service: Azure App Service
resourceType: microsoft.web/serverfarms
docsUrl: https://www.microsoft.com/licensing/docs/view/Service-Level-Agreements-SLA-for-Online-Services
lastVerified: 2026-07-10        # CI fails releases if > 90 days old
variants:
  - id: single-instance-basic
    match: { tier: [Basic, Standard], zoneRedundant: false }
    sla: 0.9995
  - id: zone-redundant
    match: { tier: [PremiumV3, PremiumV4], zoneRedundant: true, minCapacity: 2 }
    sla: 0.9999
    upgradeNote: "Requires Premium tier + >=2 instances"
ladder: [single-instance-basic, zone-redundant]
```

Matching = ordered rules, first match wins; unmatched resource → `unknown` bucket, reported honestly ("we don't score what we can't verify") and auto-filed as a dataset gap (hosted layer logs the type/SKU only).

**Seed set (~20 services) with draft values — EVERY value must be re-verified against current Microsoft SLA docs before launch:**

| Service | Key variants (draft SLA) |
|---|---|
| Virtual Machines | single w/ Premium SSD 99.9 · AZ-spread pair 99.99 · availability set 99.95 |
| VMSS | zonal 99.99 (flex, multi-zone) |
| App Service | 99.95 · ZR 99.99 |
| PostgreSQL Flexible | no HA 99.9 · same-zone HA 99.95 · ZR HA 99.99 |
| MySQL Flexible | same ladder as PG |
| SQL Database | GP 99.99 · BC/ZR 99.995 |
| SQL Managed Instance | 99.99 |
| Redis | standard 99.9 · ZR premium 99.95+ |
| Storage | LRS read 99.9 · ZRS/GRS variants 99.99+ (per access type) |
| Load Balancer (Std) | 99.99 |
| App Gateway (v2) | 99.95 |
| Azure Firewall | 99.95 / 99.99 in AZ |
| Front Door | 99.99 |
| AKS | free control plane 99.5 · SLA tier 99.9 · +AZ 99.95 |
| Cosmos DB | single region 99.99 · multi-region read 99.999 |
| Service Bus (Premium) | 99.9 |
| Event Hubs | 99.95 · dedicated 99.99 |
| Key Vault | 99.99 |
| API Management | 99.95 · multi-zone 99.99 |
| Public IP / networking base | folded into consuming service, not scored separately (documented decision) |

## 6. Scoring algorithm (`packages/core`)

1. **Grouping:** default = one "app" per resource group. User can regroup in the UI (drag between apps) — grouping is metadata, persisted with the stack.
2. **Serial composite (default, labeled "worst-case"):** `SLA_app = Π SLA_i` over scored members. Downtime h/yr = `(1 − SLA) × 8760`.
3. **Parallel/redundant sets:** v1 auto-*suggests* (never auto-applies) parallel treatment when ≥2 same-type resources coexist with an LB/AppGW/Front Door in the group. User confirms → `SLA_set = 1 − Π(1 − SLA_i)`, with a hard cap note ("shared-fate risks like region, config, deploys are NOT modeled — this is an upper bound").
4. **Impact ranking:** per resource, `impact_i = (1 − SLA_i) × 8760` h/yr; report sorts descending → "weakest links".
5. **Upgrade ladder:** for each resource, next variant in its `ladder` → ΔSLA for the whole app recomputed → "moving PG to ZR HA takes you from 99.75% → 99.84% (−7.9 h/yr)". Cost deltas: v2 (needs pricing data; out of v1 scope, show docs link instead).
6. **Honesty rules (product voice):** always show `unknown` bucket; always label estimates "designed availability, not a guarantee"; financially-backed SLA ≠ observed uptime (footnote on every report).

Reference check (used in tests): `0.9995 × 0.999 × 0.999 = 99.75%` → 21.9 h/yr; hardened `0.9999³ = 99.97%` → 2.6 h/yr.

## 7. Privacy model

- Anonymization happens **in the browser, before upload**: client hashes `id/name/resourceGroup` (SHA-256, per-session salt), keeps the `hash → real name` map in `localStorage` only. Server never sees names; UI de-references locally so the report stays readable for the owner. Shared permalinks show hashed labels unless the owner explicitly renames groups.
- Paranoid KQL variant for zero-trust orgs (server-side view identical either way).
- Uploads: TTL 24 h, then only the derived stack (types/SKUs/SLAs) persists.
- This section is a selling point — dedicate a `/privacy` page and a README section to it.

## 8. Surfaces

- **`/` calculator** — manual stack building (no upload needed), client-side math
- **`/assess` upload flow** — the KQL magnet
- **`/stack/{slug}`** — permalink report (SSR, OG image with the SLA number — this is what gets shared on LinkedIn)
- **`/badge/{slug}.svg`** — README badge "designed availability 99.87%"; cached at edge, ETag; the 24/7 traffic + backlink engine
- **SEO corpus (SSR, generated from dataset):**
  - `/sla/{service}` — one page per service (~20 at launch): SLA table, downtime math, ZR ladder, schema.org
  - `/nines/{n}` — "99.9% = how much downtime" pages for each canonical nine (8 pages)
- **`/status`** — self-referential uptime + chaos-kill history ("this site practices what it preaches")

## 9. Repo layout

```
onlynines/
├── src/OnlyNines.Core/           # scoring engine (C#, YamlDotNet only) 
├── src/OnlyNines.Cli/            # dotnet tool: onlynines report.json
├── src/OnlyNines.Web/            # Blazor Web App (SSR + WASM calculator)
├── tests/OnlyNines.Core.Tests/   # xUnit
├── data/sla/                     # YAML dataset  ← community PRs land here
├── kql/                          # inventory.kql, inventory-paranoid.kql
├── infra/                        # bicep + azure.yaml (azd)
├── .github/workflows/            # ci, deploy, dataset-freshness check
└── docs/                         # methodology, design, this spec
```

## 10. Milestones

- **M1 (weekend 1):** `OnlyNines.Core` + dataset (seed services, draft values) + CLI + xUnit tests. Publishable to GitHub immediately — first social post: "I open-sourced the SLA math". **← DONE 2026-07-10 (scaffolded).**
- **M2 (weekend 2):** Blazor web calculator + `/assess` paste-JSON flow, fully client-side WASM (no DB yet). Deployable demo.
- **M3:** Postgres + permalinks + badges → the $40 production architecture is complete → **freeze: this is the conference demo baseline.**
- **M4:** SEO corpus + OG images + status page. Submit to Google Search Console (indexing lead time: 2–4 months — do this ASAP).
- **M5:** dataset verification pass (every `lastVerified` refreshed), launch blog post, LinkedIn KQL lead-magnet post.

## 11. Risks & open questions

1. **SLA values drift** → `lastVerified` + CI freshness gate + community PRs. Owner burden: ~1 h/quarter.
2. **ARG property paths vary by API version** → fixture-driven parser tests per resource type; `unknown` bucket as the safety net.
3. **Composite-SLA methodology criticism** → pre-empt with `/docs/methodology` (worst-case-serial disclosure, parallel upper-bound cap). Being attackable-but-honest generates engagement; being sloppy generates ridicule.
4. **Badge abuse / hotlink load** → edge cache + rate limit (hosted layer).
5. **Dynatrace employment check** → before any monetization move, review IP/side-project clauses. As MIT educational OSS: low risk. (Your action item.)
6. ~~Domain~~ → **DONE: onlynines.app registered (2026-07-10, standard pricing).** Optional: grab only9.app / onlynines.dev as typo-guards if cheap.
