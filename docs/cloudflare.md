# Cloudflare in front of OnlyNines

Putting Cloudflare's edge (DNS proxy / CDN / WAF / DDoS) in front of the Azure App
Service origin — **without hosting changes**. The zone already lives in Cloudflare
(we use CF Web Analytics), today as **grey-cloud / DNS-only**. This runbook flips it
to **orange-cloud (proxied)** and locks the origin down so nobody can bypass the edge.

Config is done **by hand in the Cloudflare dashboard**. The only code artifact is the
Azure-side origin lockdown, which lives in [`infra/main.bicep`](../infra/main.bicep)
behind the `restrictToCloudflare` flag (default `false`).

## Why the order matters

Two failure modes to avoid:

1. **Redirect loop / 525.** The origin is `httpsOnly` (App Service) and does
   `UseHttpsRedirection` + HSTS. If you proxy with SSL mode **Flexible** (CF→origin over
   plain HTTP), the origin 301s back to HTTPS forever. You **must** use **Full (strict)**.
2. **Locking out live users.** While DNS is grey-cloud, visitors hit the origin *directly*
   from their own IPs. If you apply the CF-only IP allow-list (`restrictToCloudflare=true`)
   **before** the record is orange-cloud, every real visitor is denied. Lock down the origin
   **only after** proxying is confirmed working.

So the sequence is: **SSL mode first → orange-cloud → verify → then lock the origin.**

---

## Step 1 — SSL/TLS mode: Full (strict)  *(do this before proxying)*

Dashboard → **SSL/TLS → Overview** → set encryption mode to **Full (strict)**.

- Edge (browser→CF) uses CF **Universal SSL** (free, auto): browsers see a valid public cert.
- Origin (CF→App Service): CF connects with SNI/Host `onlynines.app`, App Service serves its
  **App Service Managed Certificate** for that host (public CA, already issued) → Full (strict)
  validates it. No origin cert change needed to start.
- **SSL/TLS → Edge Certificates**: enable **Always Use HTTPS**, **Automatic HTTPS Rewrites**,
  set **Minimum TLS Version = 1.2**. Leave HSTS to the origin (the app already emits it) — don't
  double-configure it at CF unless you remove `UseHsts()` from the app.

### Managed-cert renewal caveat (read this)

App Service Managed Certificates auto-renew via an HTTP token at
`/.well-known/acme-challenge/*`. Behind an orange-cloud proxy + WAF that path must stay
reachable and uncached (see Steps 4–5). If a future renewal ever fails, the quick fix is to
**grey-cloud the record for ~15 min**, let App Service renew, then re-orange.

**Recommended hardening (removes the ACME dependency entirely):** replace the origin cert with
a **Cloudflare Origin CA certificate** (free, 15-year, trusted only by CF):
CF → **SSL/TLS → Origin Server → Create Certificate**, then upload the resulting cert as a
private certificate on App Service and bind it to both hostnames. With Full (strict) + Origin
CA, CF→origin is validated by CF's own CA and there is no public-ACME renewal to babysit. This
is the more robust long-term setup; the managed-cert path above is fine to launch with.

---

## Step 2 — Proxy the records (orange-cloud)

Dashboard → **DNS → Records**. For each record, toggle the cloud icon to **Proxied (orange)**:

| Name  | Type  | Target                              | Proxy    |
|-------|-------|-------------------------------------|----------|
| `@` (onlynines.app) | CNAME/A | current App Service target (`<app>.azurewebsites.net` / its IP) | **Proxied** |
| `www` | CNAME | `onlynines.app` or the App Service host | **Proxied** |

- Keep the **App Service host bindings** for `onlynines.app` and `www` (they exist in bicep) —
  multi-tenant App Service routes by `Host` header, and CF forwards `Host: onlynines.app`.
- Leave the App Insights **asuid** TXT verification record in place (DNS-only) — it's not traffic.
- Leave the CF Web Analytics setup untouched; the beacon is independent of proxy state.

**Verify before moving on:**

```bash
# Edge now answers from Cloudflare, cert valid, no redirect loop:
curl -sSI https://onlynines.app/            | grep -Ei 'server|cf-ray|http/'
# Blazor SignalR endpoint reachable through the proxy (101/200, not 4xx/5xx):
curl -sSI https://onlynines.app/_blazor      | grep -i 'http/'
# Badge still served and edge-cacheable:
curl -sSI https://onlynines.app/badge/q0wd42nn3i.svg | grep -Ei 'cache-control|cf-cache-status'
```

Open the site: the calculator/assess pages must stay **interactive** (SignalR connected). If
interactivity breaks, it's almost always Rocket Loader or Bot Fight Mode — see Step 3.

---

## Step 3 — Blazor Server compatibility (important)

This is **Blazor Server**: interactivity rides a WebSocket/SignalR connection at `/_blazor`.
CF must not touch the app's script or long-lived socket.

Dashboard → **Speed / Scrape Shield / Security**:

- **Rocket Loader: OFF.** It defers/rewrites `blazor.web.js` and breaks startup.
- **WebSockets: ON** (Network tab — on by default; confirm it).
- **Super Bot Fight Mode:** don't let it challenge `/_blazor`. On Free, "Bot Fight Mode" can
  interfere with the persistent socket — if interactivity drops, disable it or add a skip rule
  for `/_blazor` (Step 4). Verify SignalR reconnects cleanly.
- **Email Address Obfuscation:** harmless but off is safer for an app shell.
- **Always Online: OFF.** It serves stale cached HTML when the origin is down — for a Blazor
  Server app that shell just fails to open its socket, and (fittingly for *OnlyNines*) it would
  fake uptime we don't actually have. Leave availability honest.

---

## Step 4 — Caching rules

Defaults are mostly right: CF caches static extensions (`.js`, `.css`, `.svg`) and does **not**
cache HTML without cache headers (Blazor pages send none → not cached). Make the two things that
matter explicit under **Caching → Cache Rules**:

1. **Never cache the app / socket.** Rule: URI Path starts with `/_blazor` **OR**
   `/.well-known/acme-challenge/` → **Bypass cache**. (Protects SignalR and cert renewal.)
2. **Cache the badges.** The `/badge/*.svg` endpoint already returns
   `Cache-Control: public, max-age=3600` (see `Program.cs`), so CF honours it automatically.
   Optionally add an explicit **Eligible for cache**, Edge TTL "Respect origin" rule for
   `/badge/*` to be safe. Expect `cf-cache-status: HIT` after warm-up.

---

## Step 5 — WAF

Dashboard → **Security → WAF**.

- Enable the **Cloudflare Managed Ruleset** (Free gets the free managed ruleset; OWASP Core /
  full managed rules need Pro+). Turn it on in **Log** mode for a day, watch for false positives
  on `/assess` and `/_blazor`, then switch to **Block**.
- Add a **Skip** rule (highest priority) for `/.well-known/acme-challenge/*` → skip all managed
  rules, so cert renewal is never blocked. Consider skipping WAF for `/_blazor` too if the
  managed rules flag the WebSocket frames.
- **Rate limiting:** Free includes one rule — optional here since the app is GET-heavy and
  interactions go over SignalR, not classic POST. Add later if the badge or assess endpoints get
  hammered.

---

## Step 6 — Lock the origin to Cloudflare  *(only after Steps 1–2 verify green)*

Now that all real traffic arrives via CF edge IPs, close the origin so nobody can reach it
directly at `<app>.azurewebsites.net` and bypass the WAF.

```bash
# Deploy the bicep with the flag on:
az deployment group create \
  -g <resource-group> \
  -f infra/main.bicep \
  -p restrictToCloudflare=true pgPassword=<secret>
```

This sets `ipSecurityRestrictions` on App Service to **allow only the Cloudflare ranges** in
`cloudflareIpRanges` and denies everything else. SCM/Kudu stays open
(`scmIpSecurityRestrictionsUseMain: false`) so **GitHub Actions can still deploy**.

- The App Insights availability webtest pings `https://onlynines.app/` → resolves to CF → arrives
  from a CF IP → still allowed. It now also exercises the edge path. 
- **Refresh the IP list** from <https://www.cloudflare.com/ips/> when Cloudflare changes it, or the
  allow-list drifts and legit edge traffic gets denied.

**Verify the lockdown:**

```bash
# Through the edge — should be 200:
curl -sSI https://onlynines.app/ | grep -i 'http/'
# Direct to origin (bypass attempt) — should be 403:
curl -sSI https://<app>.azurewebsites.net/ | grep -i 'http/'
```

### Optional deeper hardening

- **Authenticated Origin Pulls (mTLS):** CF presents a client cert to the origin so even a request
  from a *different* CF account (also a CF IP) is rejected. App Service enforcement is app-level
  (validate `X-ARR-ClientCert`), so it's extra code — note it, skip for now.
- **Secret header:** a CF Transform Rule injects a random header; app middleware rejects requests
  without it. Also app-level. The IP allow-list above is the standard, sufficient lockdown for
  this project.

---

## Rollback

Everything is reversible fast:

1. **Origin lockdown:** redeploy with `restrictToCloudflare=false` (or Azure Portal → App Service →
   Networking → Access restrictions → remove rules). Origin reopens immediately.
2. **Proxy:** DNS → set the records back to **DNS-only (grey)**. Traffic goes straight to Azure
   again. (If you locked the origin, reopen it *first* — grey-cloud + locked origin = site dark.)
3. **SSL mode:** back to Full / off as needed.

## What this changes about our availability story

Fitting for *OnlyNines*: the edge adds real nines the single-region B1 origin can't. Cached badges
and static assets keep serving from CF PoPs even when the origin cold-starts or dies, and CF's
anycast absorbs DDoS the B1 plan never could. The origin is still deliberately fragile — that's the
talk — but the *observed* edge availability is now decoupled from it. Worth a line in
[`docs/methodology.md`](methodology.md).
