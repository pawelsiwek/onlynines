# Design prompt (for Claude / any design AI)

> Paste the prompt below to generate the OnlyNines visual identity + landing/report mockups.

---

Design a landing page and a report page for **OnlyNines** (onlynines.app) — a developer tool that computes the composite SLA ("how many nines") of a real Azure environment from a single pasted query result. Tagline: **"we only count nines."**

**The joke, and its limits.** The brand is a playful, PG-13 homage to OnlyFans: loose visual nod through color and rounded, friendly geometry — NOT through imagery. No suggestive photos, no innuendo beyond the name. Think "inside joke that a conference audience laughs at, then trusts the tool anyway."

**Visual direction:**
- Palette: white background, electric azure **#00AFF0** as the primary accent (buttons, links, the brand mark), deep navy **#0B1E33** for text, a warm alert coral for "weakest link" highlights, success green used *sparingly* — nines are earned, not given.
- Logo/wordmark: "OnlyNines" in a rounded geometric sans; consider the "9" glyph doubling as a lock or a gauge needle. A standalone glyph mark: a padlock whose shackle forms a "9".
- Typography: rounded sans for UI (Nunito/Poppins family feel); ALL SLA figures in a monospace with tabular numerals — the numbers are the product, they must look like instruments, not marketing.
- Tone: cheeky copy, serious numbers. Buttons can flirt ("Show me your nines"), figures never do.

**Landing page sections:**
1. Hero: headline "One query. Your real SLA." with sub "No login. No credentials. No agent. We only count nines." CTA button: "Assess my environment". Secondary: "Just give me the calculator".
2. A 3-step strip: paste KQL → export JSON → get your nines (with a tiny code snippet visual).
3. A live-looking report teaser card (see report spec below).
4. Privacy strip: "your resource names never leave your machine" with a small diagram.
5. Footer with GitHub link, methodology link, and an uptime badge of onlynines.app itself ("this site practices what it preaches — chaos-kill history →").

**Report page:**
- Header: big composite SLA figure (e.g., **99.750%**) with a "nines gauge" — a horizontal meter marked 9 / 99 / 99.9 / 99.99 / 99.999, needle at the current value; secondary figure "21.9 h/yr downtime budget" .
- "Weakest links" ranked list: resource, current tier, hours it costs per year (coral), and a "next rung" chip showing the upgrade (+SLA delta).
- An "unknown bucket" section, visually honest (grey, not hidden).
- Share bar: copy permalink, download OG image, and a README badge preview: `OnlyNines · 99.75%` (shields.io style, azure background).
- Sticky footnote: "Designed availability, not a guarantee."

**Deliverables:** landing page mockup (desktop + mobile), report page mockup, logo/wordmark + glyph, badge component, OG-image template (the shareable card with the big SLA number). Dark mode variant of the report page.
