# Design prompt — criticality verdict section (report page)

> Paste to Claude design. Context: extends the existing OnlyNines report page (see previous handoff: tokens, Poppins + JetBrains Mono, azure #00AFF0, navy #0B1E33, coral #FF6B4A, mist #EEF3F8).

---

Design a new section for the OnlyNines report page: the **criticality verdict**. It sits between the composite-SLA header card and the "Weakest links" table. Its job: answer *"is raising the nines even worth it?"* — the report's emotional peak.

**Input control — criticality selector.** Five workload classes, Azure Well-Architected style: "It's a blog · 99%", "Internal / dev-test · 99.5%", "Production · 99.9%", "Business-critical · 99.95%", "Mission-critical · 99.99%". One is always selected (default: Production). Design as segmented pills or a horizontal slider with labeled stops — it should feel like a *dial that changes the verdict live*, inviting play. Each class has a one-line blurb shown on hover/selection (e.g. "Nobody pages anyone. Downtime is an inconvenience.").

**The verdict banner.** Four states, each with its own color voice and a one-liner:
1. **ON TARGET** (calm green) — "Don't touch it. Every further nine is a vanity metric you'll pay for monthly."
2. **UNDER TARGET** (azure, constructive) — "You're 0.049 points short. These 2 upgrades get you there — and that's where you stop." Below: an ordered mini-list (resource → next tier chip → hours saved/yr), then a muted line: "3 more upgrades possible — your target doesn't justify them."
3. **OVER-ENGINEERED** (warm amber, cheeky) — "You're above target with room to spare. 2 resources can step down a tier — same verdict, smaller bill." Below: downgrade list. This state is the brand moment: the tool that tells you to spend LESS. Give it a subtle flourish (e.g. a small 💸 or a "buy yourself a beer" microcopy — PG, tasteful).
4. **UNREACHABLE** (coral, serious) — "Even maxing every tier gets you to 99.98%. This target needs architecture, not tiers." Link chip to WAF redundancy docs.

**Requirements:** the verdict must read as *the tool's opinion*, typographically distinct from the data tables (data = mono instruments; verdict = confident sentence in Poppins). Changing criticality re-animates the verdict (fade/slide, ~200ms). Include a small "WAF: define reliability targets ↗" reference link in the section header. Mobile: selector wraps to two rows, verdict full-width.

**Deliverables:** desktop + mobile mock of the section in all four states, light + dark mode, using the existing token set.
