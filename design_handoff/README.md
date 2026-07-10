# Handoff: OnlyNines — Full Design Package

## Overview
OnlyNines (onlynines.app) is a developer tool that computes the **composite SLA** ("how many nines") of a real Azure cloud environment from a single pasted Azure Resource Graph (KQL) query result. This package covers the complete first design pass: **desktop + mobile landing page, the assess (input) page, report page (light + dark mode), and the brand assets** (glyph, app icon, wordmark, palette, type, badge component, OG-image template).

Tagline: **"we only count nines."** Brand is a playful, PG-13 nod to a well-known subscription brand — expressed *only* through color and rounded, friendly geometry. No suggestive imagery, no innuendo beyond the name.

## About the Design Files
The file in this bundle (`OnlyNines Landing.dc.html`) is a **design reference created in HTML** — a prototype showing the intended look and behavior, not production code to copy directly. It is authored as a "Design Component" and uses an inline-styles-only convention with a small runtime for streaming preview; **do not ship it as-is**.

The task is to **recreate this design in the target codebase's existing environment** (React, Vue, Svelte, plain HTML/CSS, etc.) using that project's established patterns, component library, and styling approach. If no environment exists yet, choose the most appropriate stack for a marketing landing page (e.g. Next.js + Tailwind, or Astro) and implement there. Translate the inline styles into the codebase's idiom (CSS modules, Tailwind classes, styled-components — whatever the project uses).

## Fidelity
**High-fidelity (hifi).** Final colors, typography, spacing, and interactions are all specified below and should be recreated pixel-accurately. Exact hex values and font sizes are given.

## Layout Overview
Single scrolling page. Content is centered in a **max-width 1120px** container with **32px horizontal padding** throughout. Sections top to bottom:

1. Nav bar
2. Hero (2-column grid)
3. Three-step strip (3-column grid)
4. Report teaser (2-column grid, tinted band)
5. Privacy strip (2-column grid)
6. Footer (dark)

Root: `font-family: 'Poppins'`, base text color `#0B1E33`, background `#ffffff`, `box-sizing: border-box` on all elements.

## Screens / Views

### 1. Nav bar
- **Layout**: flex, space-between, align-center. Padding `26px 32px`, within the 1120px container.
- **Logo mark** (left): 38×38px, `border-radius: 11px`, background `#00AFF0`, box-shadow `0 6px 16px rgba(0,175,240,0.35)`. Contains a white "9" in JetBrains Mono 700, 22px, letter-spacing -1px, plus a small 4×8px azure keyhole tab at the bottom center (offset +3px right) to read as a padlock/"9".
- **Wordmark**: "Only" in `#0B1E33` + "Nines" in `#00AFF0`, Poppins 800, 21px, letter-spacing -0.5px.
- **Nav links** (right): flex, gap 30px, 15px/500. "Methodology", "GitHub" in `#0B1E33`. CTA "Assess my environment": background `#0B1E33`, white text, `padding: 10px 20px`, `border-radius: 11px`, 600. **Hover**: background → `#00AFF0`.

### 2. Hero
- **Layout**: grid `1.05fr 0.95fr`, gap 56px, align-center. Padding `64px 32px 40px`.
- **Left column:**
  - **Eyebrow pill**: inline-flex, gap 8px, background `#F0FAFE`, border `1px solid #CDEEFB`, text `#0090C8`, `padding: 7px 14px`, `border-radius: 999px`, 13px/600, margin-bottom 26px. Leading 7px green dot (`#1F8A5B`). Text: "SLA calculator for Azure infrastructure".
  - **H1**: Poppins 800, **60px**, line-height 1.02, letter-spacing -2px, margin `0 0 22px`. Text: "One query. / Your **real** Azure SLA." — the word "real" is `#00AFF0`; `<br>` before "Your".
  - **Sub**: 19px, line-height 1.5, color `#47586B`, max-width 460px, margin-bottom 34px. Text: "Compute the composite SLA of your entire Azure cloud environment — every VM, database and storage account — from a single Azure Resource Graph query. No login. No credentials. No agent. We only count nines."
  - **CTA row**: flex, gap 14px.
    - Primary "Show me your nines →": background `#00AFF0`, white, `padding: 15px 26px`, `border-radius: 13px`, 16px/700, box-shadow `0 10px 24px rgba(0,175,240,0.32)`. **Hover**: background `#0090C8`, shadow `0 14px 30px rgba(0,175,240,0.42)`, `transform: translateY(-1px)`.
    - Secondary "Just give me the calculator": text `#0B1E33`, `padding: 15px 22px`, `border-radius: 13px`, 16px/600, border `1.5px solid #E2E8F0`. **Hover**: text + border → `#00AFF0`.
  - **Trust row** (margin-top 34px): flex, gap 22px, 13px/500, color `#8595A6`. Two items, each with a green `✓` (`#1F8A5B` 700): "Runs in your browser", "Zero-trust by design".
- **Right column — Gauge card:**
  - Background `#0B1E33`, `border-radius: 24px`, `padding: 34px`, white text, box-shadow `0 30px 60px rgba(11,30,51,0.28)`, `position: relative; overflow: hidden`.
  - Decorative glow: absolute 180×180px circle top-right (-60,-60), `radial-gradient(circle, rgba(0,175,240,0.35), transparent 70%)`.
  - Header row: label "COMPOSITE SLA" (13px/600, `#8DA2B8`, uppercase, letter-spacing 1px) + tag "azure · prod" (JetBrains Mono 11px, `#00AFF0`, background `rgba(0,175,240,0.14)`, `padding: 4px 9px`, `border-radius: 6px`).
  - **Big figure**: JetBrains Mono 700, **66px**, letter-spacing -3px, line-height 1, `font-variant-numeric: tabular-nums`. Text "99.750%" with the "%" in `#00AFF0`.
  - Sub-figure: JetBrains Mono 14px, `#8DA2B8`, margin-top 8px: "≈ 21.9 h/yr downtime budget".
  - **Nines gauge** (margin-top 30px):
    - Track: height 12px, background `rgba(255,255,255,0.08)`, `border-radius: 999px`, position relative.
    - Fill: height 12px, `border-radius: 999px`, `linear-gradient(90deg, #00AFF0, #4FD0FF)`, width = current value (see gauge math), `transition: width 1.5s cubic-bezier(0.22,1,0.36,1)`.
    - Needle knob: 20×20px circle, white, `border: 4px solid #00AFF0`, absolute at `left: <value>`, `transform: translate(-50%,-50%)`, same transition on `left`, box-shadow `0 2px 8px rgba(0,0,0,0.3)`.
    - Scale labels (margin-top 12px): flex space-between, JetBrains Mono 11px, `#5E7590`, tabular-nums: `9  99  99.9  99.99  99.999`.

### 3. Three-step strip
- **Layout**: within 1120px container, `padding: 56px 32px`. Centered heading block (margin-bottom 44px): H2 Poppins 800, 34px, letter-spacing -1px, "Three steps to your Azure nines"; sub 17px `#47586B`, "Straight from Azure Resource Graph. No SDK. No pipeline. Paste, export, count."
- **Cards**: grid `1fr 1fr 1fr`, gap 22px. Each: border `1.5px solid #EAF0F5`, `border-radius: 20px`, `padding: 28px`. **Hover**: border → `#00AFF0`, box-shadow `0 16px 34px rgba(0,175,240,0.12)`.
  - Card header: flex, gap 12px, margin-bottom 18px. Step number chip 34×34px, `border-radius: 10px`, JetBrains Mono 700 15px. Steps 01 & 02: background `#F0FAFE`, text `#00AFF0`. Step 03: background `#00AFF0`, text white. Title Poppins 700, 17px ("Paste KQL", "Export JSON", "Get your nines").
  - **Card 01 & 02 code block**: background `#0B1E33`, `border-radius: 12px`, `padding: 16px`, JetBrains Mono 12.5px, line-height 1.6, base text `#A9C6E0`. Syntax accent colors: keywords/identifiers `#4FD0FF`, pipes `#FF8A6E`, strings `#8FE0B0`, null/values `#FFB27A`.
    - Card 01 content: `resources` / `| where type =~ "microsoft.compute/..."` / `| project name, sku, zones`.
    - Card 02 content: JSON — `[ {` / `"name": "vm-web-01",` / `"zones": null } ]`.
  - **Card 03 result block**: background `#F0FAFE`, border `1px solid #CDEEFB`, `border-radius: 12px`, `padding: 16px`, flex align-baseline gap 10px. Figure "99.750%" JetBrains Mono 700 30px `#0B1E33` tabular-nums + label "composite" JetBrains Mono 12px `#0090C8`.

### 4. Report teaser
- **Layout**: full-bleed band background `#F5F9FC`, `padding: 72px 32px`. Inner grid `0.85fr 1.15fr`, gap 48px, align-center, max-width 1120px.
- **Left column:**
  - Eyebrow: JetBrains Mono 13px `#0090C8` 500: "// the report".
  - H2 Poppins 800, 36px, letter-spacing -1px, line-height 1.08, margin `12px 0 16px`: "See exactly which resource is dragging you down."
  - Body 17px, line-height 1.55, `#47586B`: "Every resource ranked by the downtime it costs you per year. The weakest link gets the coral. The upgrade path gets a chip. Nothing gets hidden."
  - Link "View a sample report →": `#00AFF0` 700 16px, inline-flex gap 7px. **Hover**: `#0090C8`, gap 9px.
- **Right column — report card**: background `#fff`, `border-radius: 22px`, `padding: 28px`, box-shadow `0 24px 50px rgba(11,30,51,0.10)`, border `1px solid #EAF0F5`.
  - Card header (margin-bottom 20px): flex space-between. "WEAKEST LINKS" (700 15px `#8595A6` uppercase) + "by h/yr cost" (JetBrains Mono 12px `#8595A6`).
  - **Weakest-links rows** (repeat): flex align-center gap 14px, `padding: 15px 0`, `border-bottom: 1px solid #F0F4F8`. Columns:
    1. Resource name — JetBrains Mono 500 14px `#0B1E33`, ellipsis truncate; sub-line tier 12px `#8595A6` margin-top 3px.
    2. Cost — right-aligned, JetBrains Mono 700 15px, **coral `#FF6B4A`**, tabular-nums, min-width 78px.
    3. Next-rung chip — JetBrains Mono 11.5px/500 `#0090C8`, background `#F0FAFE`, border `1px solid #CDEEFB`, `padding: 5px 9px`, `border-radius: 8px`, nowrap.
    - Row data (example figures):
      - `vm-web-01 · single instance` / "Standard VM · no availability set" / **7.9 h/yr** / "Avail. Zones +0.09%"
      - `sql-orders-prod` / "SQL DB · Standard tier" / **6.2 h/yr** / "Business Critical +0.04%"
      - `st-assets-lrs` / "Storage · LRS" / **4.4 h/yr** / "ZRS +0.09%"
  - **Unknown bucket** (margin-top 18px): background `#F5F7FA`, border `1px dashed #D3DBE3`, `border-radius: 12px`, `padding: 14px 16px`, flex space-between. Title "Unknown bucket" 600 13.5px `#64748B`; sub "4 resources with no published SLA" 12px `#94A3B8`. Right tag "excluded" JetBrains Mono 13px `#64748B`, background `#E7ECF1`, `padding: 5px 10px`, `border-radius: 7px`.

### 5. Privacy strip
- **Layout**: within 1120px container, `padding: 72px 32px`. Grid `1fr 1fr`, gap 48px, align-center.
- **Left — diagram card**: background `#0B1E33`, `border-radius: 22px`, `padding: 34px`, white. Inner flex space-between, gap 16px:
  - "Your machine": 62×62px tile, `border-radius: 16px`, background `rgba(255,255,255,0.06)`, border `1px solid rgba(255,255,255,0.12)`, 26px 🖥️ emoji; label 12px `#8DA2B8` 600 margin-top 10px. (In production, replace emoji with the codebase's icon set — a laptop/monitor icon.)
  - Middle: flex-1, center. Dashed line `repeating-linear-gradient(90deg, #2A4056 0 8px, transparent 8px 16px)`, height 2px; caption JetBrains Mono 10.5px **`#FF6B4A`** 500: "✗ resource names stay here".
  - "Our servers": same tile at `opacity: 0.4`, background `rgba(255,255,255,0.04)`, dashed border, ☁️ emoji; label "Our servers".
- **Right column:**
  - Eyebrow JetBrains Mono 13px `#0090C8`: "// privacy".
  - H2 Poppins 800, 34px, letter-spacing -1px, line-height 1.1, margin `12px 0 16px`: "Your resource names never leave your machine."
  - Body 17px, line-height 1.55, `#47586B`: "The math runs entirely in your browser. We never see your subscription, your topology, or what you named that one VM at 2am. There's nothing to breach because there's nothing sent."

### 6. Footer
- **Layout**: background `#0B1E33`, white text, `padding: 56px 32px 40px`. Inner max-width 1120px.
- **Top row**: flex, align-flex-start, space-between, wrap, gap 32px, `padding-bottom: 34px`, `border-bottom: 1px solid rgba(255,255,255,0.1)`.
  - **Brand block** (max-width 320px): logo mark (34×34px azure, `border-radius: 10px`, white "9" JetBrains Mono 700 19px) + wordmark ("Only" white / "Nines" `#00AFF0`, 800 19px). Below: 14px `#8DA2B8`, line-height 1.55: "We only count nines. Designed availability, computed honestly, from a query you already have."
  - **Link columns** (flex, gap 56px):
    - "RESOURCES" (12px uppercase `#5E7590` 700, letter-spacing 1px, margin-bottom 14px) → column of links (flex-col gap 11px, 14.5px, `#C4D3E2`, **hover** `#00AFF0`): "GitHub", "Methodology", "SLA sources".
    - "OUR OWN UPTIME" → **uptime badge**: inline-flex, `border-radius: 8px`, overflow hidden, JetBrains Mono 12.5px/500, box-shadow `0 4px 12px rgba(0,0,0,0.25)`. Left half: background `#253B52`, white, `padding: 7px 11px`, "onlynines.app". Right half: background `#00AFF0`, white, `padding: 7px 11px`, "99.982%". Below (margin-top 12px): link 13px `#8DA2B8`, **hover** `#00AFF0`: "this site practices what it preaches — chaos-kill history →".
- **Bottom row** (padding-top 24px): flex space-between, wrap, gap 10px, 13px `#5E7590`. Left "© 2026 OnlyNines"; right JetBrains Mono: "Designed availability, not a guarantee."

## Interactions & Behavior
- **Gauge animation**: on mount, both the fill `width` and needle `left` animate from `0%` to the current value over **1.5s**, easing `cubic-bezier(0.22,1,0.36,1)`, after a ~260ms delay. Implement with a mount effect that flips a state flag, or an IntersectionObserver so it triggers when the card scrolls into view.
- **Hover states**: nav CTA, hero CTAs, step cards, and all footer links — see per-component specs above. All transitions ~150–200ms ease is fine.
- **Navigation**: all CTAs currently `href="#"`. Wire primary CTAs to the calculator/assessment route; "GitHub", "Methodology", "SLA sources", "chaos-kill history" to their real URLs.
- **Responsive** (not in this desktop mock, but expected): the three 2-column grids and the 3-column strip should stack to single column below ~900px; hero figure sizes should scale down (H1 ~40px on mobile). A separate mobile mock was not produced this round.

## State Management
- Marketing page — effectively stateless except the gauge's mount/in-view animation flag.
- The **gauge value** is the one real data input: given a composite SLA percentage `p` (e.g. `0.9975`), compute nines and position (see below). On this landing page it is a fixed showcase value (99.750%); on the actual report page it would be driven by the computed result.

## Gauge Math (important)
The gauge scale marks are the "nines" milestones evenly spaced: `9` → 1 nine, `99` → 2, `99.9` → 3, `99.99` → 4, `99.999` → 5.

```
nines    = -log10(1 - p)          // p = 0.9975 → 2.60
position = (nines - 1) / (5 - 1)  // → 0.40  → "40.0%"
```

For 99.750%: nines ≈ 2.60, fill/needle position ≈ **40.0%**. Downtime budget = `(1 - p) * 8760 h` = 21.9 h/yr.

## Design Tokens

**Colors**
- Azure (primary accent): `#00AFF0`; hover/deep: `#0090C8`; gauge gradient end: `#4FD0FF`
- Navy (text / dark surfaces): `#0B1E33`; footer badge left: `#253B52`; dashed diagram line: `#2A4056`
- Coral (weakest-link / alert): `#FF6B4A`; dark-surface variant: `#FF8A6E`
- Success green (used sparingly): `#1F8A5B`
- Text greys: body `#47586B`, muted `#8595A6`, on-dark muted `#8DA2B8`, on-dark faint `#5E7590`, `#64748B`, `#94A3B8`
- Tints & borders: azure tint bg `#F0FAFE`, azure tint border `#CDEEFB`, band bg `#F5F9FC`, card border `#EAF0F5`, hairline `#F0F4F8`, neutral tint `#F5F7FA`, dashed border `#D3DBE3`, tag bg `#E7ECF1`, default border `#E2E8F0`
- On-dark code text: `#A9C6E0`; code accents `#4FD0FF` / `#8FE0B0` / `#FFB27A`
- White `#ffffff`

**Typography**
- UI / headings: **Poppins** (400/500/600/700/800). Google Fonts.
- All figures / code / labels-as-instruments: **JetBrains Mono** (400/500/700), always `font-variant-numeric: tabular-nums` on numeric figures.
- Scale: H1 60px/800/-2px · H2 34–36px/800/-1px · big figure 66px/700/-3px · body 17–19px · small 12–14px · micro labels 11–13px.

**Radius**: 6, 7, 8, 10, 11, 12, 13, 16, 20, 22, 24, 999 (pills) px.

**Shadows**
- CTA: `0 10px 24px rgba(0,175,240,0.32)` → hover `0 14px 30px rgba(0,175,240,0.42)`
- Card (light): `0 24px 50px rgba(11,30,51,0.10)`
- Card (dark hero): `0 30px 60px rgba(11,30,51,0.28)`
- Step hover: `0 16px 34px rgba(0,175,240,0.12)`
- Badge: `0 4px 12px rgba(0,0,0,0.25)`

**Layout**: container max-width 1120px, horizontal padding 32px. Section vertical padding 56–72px.

## Assets
- **Fonts**: Poppins + JetBrains Mono via Google Fonts (`https://fonts.googleapis.com/css2?family=Poppins:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500;700&display=swap`). Self-host in production if preferred.
- **Logo/glyph**: no image file — the mark is built inline (azure rounded tile + JetBrains Mono "9" + keyhole tab). A production SVG glyph should be commissioned per the brief ("a padlock whose shackle forms a 9"). No SVG asset is shipped in this bundle.
- **Icons**: 🖥️ and ☁️ emoji are placeholders in the privacy diagram — replace with the codebase's icon library (monitor + cloud).
- **No raster images** are used.

---

# Report Page (`OnlyNines Report.dc.html` + `OnlyNines Report Dark.dc.html`)

The page a user lands on after an assessment. Single centered column, **max-width 940px**, `padding: 0 28px`. Page background `#EEF3F8` (light) / `#060F1B` (dark). Bottom padding 88px to clear the sticky footnote. Same fonts and gauge math as the landing page.

## Report — Views

### Nav
Flex space-between, `padding: 22px 28px`. Left: 34px logo tile + wordmark. Right: an env chip (JetBrains Mono 12.5px, "azure · prod · 61 resources"; light: white bg / `#E2E8F0` border / `#64748B`; dark: `#0F2438` bg / `#1E3A4D` border / `#8DA2B8`) + "Re-run assessment" button (light: navy → hover azure; dark: azure → hover `#4FD0FF` on `#06121F`).

### Header card (the hero of the report)
- Surface: light `#0B1E33`; dark `linear-gradient(160deg,#0F2438,#0A1B2C)` + `1px solid #1B3A57`. `border-radius: 24px`, `padding: 38px 40px`, white text, `overflow: hidden`. Top-right radial glow `rgba(0,175,240,0.28–0.35)`, 260px.
- Inner grid `1fr auto`, gap 40px, align-items flex-start.
- **Left:** label "COMPOSITE SLA" (13px/600 uppercase, `#8DA2B8`/`#7C93AB`, letter-spacing 1.5px). Big figure JetBrains Mono 700, **82px**, letter-spacing -4px, line-height 0.9, tabular-nums, "%" in azure. Beside it, secondary "≈ 21.9 h/yr downtime budget" (JetBrains Mono 14px muted, two lines).
- **Gauge** (margin-top 34px, max-width 520px): 14px track (`rgba(255,255,255,0.06–0.08)`), fill `linear-gradient(90deg,#00AFF0,#4FD0FF)` (dark adds `box-shadow: 0 0 18px rgba(0,175,240,0.6)`), 22px white knob w/ 5px azure border. Two label rows below: values `9 / 99 / 99.9 / 99.99 / 99.999` (12px) and nines `1 nine / 2 / 3 / 4 / 5 nines` (10px uppercase). Animates in on mount, 1.5s cubic-bezier(0.22,1,0.36,1), ~260ms delay.
- **Right — share bar** (min-width 210px, flex-col gap 10px): two full-width buttons "🔗 Copy permalink" and "🖼️ Download OG image" (translucent white surface, hover lightens / dark hover tints azure). Then label "README badge" (11px uppercase `#5E7590`) and the badge chip: JetBrains Mono 12px, `border-radius: 7px`, left `#253B52` "OnlyNines", right `#00AFF0` "99.75%". (Replace emoji with real link/image icons in production.)

### Weakest links table
- Card: light `#fff` / `1px solid #E7EDF3`; dark `#0C1D30` / `1px solid #17324B`. `border-radius: 20px`, `padding: 30px 34px`, margin-top 20px.
- Heading row: H2 "Weakest links" (Poppins 800, 20px) + right caption "ranked by downtime cost · h/yr" (13px muted). Sub-line 13.5px muted explaining the chips.
- **Grid columns** (header + each row): `30px 1fr 110px 200px`, gap 16px, align-center. Header row 11px/700 uppercase muted, `border-bottom: 2px solid` (`#EEF3F8` light / `#16304A` dark): `#` · `Resource · current tier` · `Costs` (right) · `Next rung`.
- **Row** (`padding: 16px 0`, hairline bottom border `#F2F5F9` / `#122A41`):
  1. Rank — JetBrains Mono 700 14px, faint (`#C2CDD9` / `#3F5570`).
  2. Name (JetBrains Mono 500 14.5px, ellipsis) + tier sub-line (12.5px muted).
  3. Cost — right-aligned JetBrains Mono 700 16px **coral** (`#FF6B4A` / `#FF7A5C`) tabular-nums, with "h/yr" micro-label under.
  4. Next rung: chip (JetBrains Mono 12px azure on azure-tint; dark uses `rgba(0,175,240,0.1)` bg / `rgba(0,175,240,0.3)` border / `#4FD0FF`) + `+delta%` in **green** (`#1F8A5B` / `#34C77B`).
- **Row data** (rank · name · tier · cost h/yr · rung · delta):
  1. vm-web-01 · single instance / Standard VM · no availability set / 7.9 / Availability Zones / +0.09%
  2. sql-orders-prod / Azure SQL · Standard tier / 6.2 / Business Critical / +0.04%
  3. st-assets-lrs / Storage account · LRS / 4.4 / ZRS / +0.09%
  4. aks-prod-nodepool / AKS · free control plane / 1.8 / Uptime SLA tier / +0.05%
  5. redis-session-c1 / Cache for Redis · Standard / 1.1 / Premium (zone-redundant) / +0.09%
  6. appgw-edge-01 / App Gateway · single zone / 0.5 / Multi-zone deployment / +0.04%

### Unknown bucket
Deliberately honest, not hidden. Surface: light `#F4F6F9` / `1px dashed #CBD5E1`; dark `#0A1725` / `1px dashed #2A415A`. `border-radius: 20px`, `padding: 26px 30px`, margin-top 20px. Grey dot + H2 "Unknown bucket" (18px/800, muted grey — NOT navy). Body: "4 resources have no published Microsoft SLA, so we **excluded** them from the composite rather than guess. They're not hidden — they're just not counted." Right: two JetBrains Mono stats (`4` resources, `—` SLA). Below: chips for the 4 resource names (`func-webhooks-consumption`, `eventgrid-topic-orders`, `logicapp-billing-sync`, `customvision-ocr-endpoint`).

Then a centered "Methodology & SLA sources →" link.

### Sticky footnote
`position: fixed` bottom bar, full width, `rgba(11,30,51,0.94)` (light) / `rgba(6,15,27,0.92)` (dark) + `backdrop-filter: blur(8px)`, top hairline, `z-index: 50`. Centered JetBrains Mono 12.5px muted: azure "ⓘ" + "Designed availability, not a guarantee."

**Dark mode** is a full recolor of the same structure — token pairs are given inline above (light / dark). Implement as a single component with a theme flag, not two code paths.

---

# Mobile Landing (`OnlyNines Landing Mobile.dc.html`)

The desktop landing flow reflowed to a **390px** single column (shown inside a phone frame for the mock — the frame/status bar/home indicator are presentation only, not part of the product UI). Section order identical to desktop: nav (logo + hamburger) → hero (stacked, H1 38px, full-width stacked CTAs) → gauge card (52px figure) → 3 steps (vertical list with number chips) → report teaser card (compact weakest-links) → privacy diagram → footer. All colors/tokens match desktop; sizes step down (see file). Use this as the responsive spec for the landing page below ~640px.

---

# Brand Assets (`OnlyNines Brand.dc.html`)

A reference board, not a shippable page — extract each asset into the codebase's design system / asset pipeline.

### Glyph mark — padlock "9"
Built as inline SVG (viewBox `0 0 100 120`), single color (azure `#00AFF0`, or navy on light):
- Bowl of the 9 = ring: `<circle cx=52 cy=40 r=24 stroke width=12 fill=none>`
- Stem of the 9: `<line x1=76 y1=40 x2=76 y2=74 stroke width=12 stroke-linecap=round>`
- Lock body: `<rect x=30 y=68 width=52 height=44 rx=13 fill>`
- Keyhole (white): `<circle cx=56 cy=86 r=6>` + `<rect x=53 y=89 width=6 height=13 rx=3>`

A production designer should refine this into a final single-path SVG; the construction above is the intended read (bowl+stem = 9, body+keyhole = lock).

### App icon
Rounded tile (`border-radius: 26px` at 104px; scale radius ~25% of size), azure fill, white JetBrains Mono 700 "9" (~60% of tile height, letter-spacing negative), plus a small azure keyhole tab at the base to echo the glyph. Shadow `0 14px 30px rgba(0,175,240,0.45)`.

### Wordmark
"Only" in navy (`#0B1E33`, or white on dark) + "Nines" in azure `#00AFF0`, Poppins 800, letter-spacing -0.8px, preceded by the logo tile. Provide light-bg and dark-bg lockups.

### Badge component (shields.io style)
Two-segment pill, JetBrains Mono 13px/500, `border-radius: 6px`, `overflow: hidden`, each segment `padding: 7px 11px` white text. Left label always navy `#253B52`. Right value colored **by tier**:
- Default / brand: azure `#00AFF0`
- Strong SLA (≥ ~4 nines): green `#1F8A5B` (rare on purpose)
- Weak SLA: coral `#FF6B4A`
- Neutral metric (e.g. nines count): navy `#0B1E33`

Markdown/embed form: `![OnlyNines](https://onlynines.app/badge/<env-id>.svg)`. Implement server-side as a real SVG endpoint.

### OG-image template — 1200 × 630
Navy `#0B1E33` background, top-right azure radial glow (480px). `padding: 64px 72px`, three stacked zones (space-between):
- **Top:** 52px logo tile + wordmark (30px) on the left; "azure · prod" tag (JetBrains Mono 17px, azure-tint pill) on the right.
- **Middle:** "COMPOSITE SLA" eyebrow (22px uppercase, letter-spacing 2px, `#8DA2B8`) then the huge figure — JetBrains Mono 700, **168px**, letter-spacing -8px, line-height 0.85, tabular-nums, "%" azure. Below: "≈ 21.9 h/yr downtime budget" (24px `#A9C6E0`) + "weakest: vm-web-01" (20px coral).
- **Bottom:** a full-width mini gauge (12px track, 40% azure fill, 22px knob) with "onlynines.app" (JetBrains Mono 20px `#5E7590`) at the right.

Render this server-side (e.g. Satori / headless screenshot) with the env's real numbers to produce the shareable PNG.

---

# Assess Page (`OnlyNines Assess.dc.html`)

The input page (`/assess`) where a user gets their Azure inventory and pastes it to be counted. **This is a corrective spec** — an earlier build drifted visually; the points below are what to fix/keep. Same fonts, tokens, and nav as the rest of the site.

## Layout
Single **centered card**, `max-width: 860px`, `padding: 0 32px`, on the mist background `#EEF3F8`. Centered header (eyebrow `// assess`, H1 "Show me your nines." 46px/800, sub 17px). Then one white flow card: `border-radius: 24px`, `border: 1px solid #E3EAF1`, `box-shadow: 0 18px 44px rgba(11,30,51,0.08)`, `overflow: hidden`, containing two numbered steps split by a hairline. Do NOT let the flow sprawl full-width — contain it in this card.

## Step 01 — Get the inventory
- Header row: `01` number chip (32px, `#F0FAFE` bg / `#00AFF0`, JetBrains Mono 700) + title "Get the inventory". **Top-right: a quiet "⧉ Copy" button** (white, `1.5px solid #E2E8F0`, hover azure border/text) — this is the utility action that copies the script. NOT a heavy filled button.
- **Segmented tabs** (3): "Cloud Shell · recommended" / "Portal (KQL + CSV)" / "paranoid". Pill group `#F1F5F9` bg, active tab white + `#00AFF0` text + small shadow, JetBrains Mono 12.5px.
- **Query block** (read-only): navy `#0B1E33`, `border-radius: 14px`. Top chrome bar with three traffic-light dots (coral/amber/green), a per-mode filename (`cloud-shell.sh` / `inventory.kql` / `inventory-paranoid.kql`) and a right-aligned "read-only" tag. Body is the highlighted query.
- **Below the query, NOT top-right: the primary next-step button** — full azure `#00AFF0`, `border-radius: 11px`, `box-shadow: 0 8px 18px rgba(0,175,240,0.3)`, label per mode: **"Open Cloud Shell ↗"** (Cloud Shell) / **"Open Resource Graph Explorer ↗"** (Portal & paranoid). To its right, the hint text (13px `#64748B`). Rationale: reading order = action order — the user reads the command, then the "go run it" button sits exactly where they finish reading. Top-right holds only the utility Copy.

### Query formatting (important — match this)
The query is a **constant string OnlyNines ships**, not user input, so its formatting is fully under your control. Two requirements:
1. **Wrap, never clip.** Render the block with `white-space: pre-wrap; word-break: break-word; overflow-y: auto; max-height: ~240px`. The earlier build let the Cloud Shell one-liner (`az graph query --first 1000 -q "…"`) overflow horizontally and get cut off behind a scrollbar — do not do that. The `az` command is written across multiple lines inside the quoted query so it reads cleanly.
2. **Syntax highlight.** Ship the query pre-tokenized (or run a tiny highlighter once on mount — highlight.js / Prism with a KQL grammar). Token colors on the navy bg: strings `#8FE0B0`, pipes `#FF8A6E`, comments `#5E7590`, keywords (`resources`, `where`, `project`, `type`, `in~`, `summarize`, `extend`) `#4FD0FF`, everything else `#A9C6E0`. The reference implements this as a small span-tokenizer — the visual result is what to match.

Per-mode query content: **Cloud Shell** = an `az graph query` CLI wrapper that auto-downloads `onlynines.json` when run (hint: "Paste, Enter — onlynines.json downloads itself. Drop the file below. Nothing to copy from the terminal."). **Portal** = the raw KQL, run in Resource Graph Explorer then Download as CSV. **paranoid** = same KQL but `project name = hash_sha256(name), …` so names are hashed client-side.

## Step 02 — Paste the output
- Header: `02` chip + "Paste the output — JSON or CSV, we'll figure it out" + a right-aligned **live counter** (JetBrains Mono 12px): "waiting for input" grey `#94A3B8` → "✓ N resources detected" green `#1F8A5B` once a valid JSON array or CSV is present.
- **The paste field must be LIGHT, not dark.** `background: #F7FAFC`, `1.5px solid #DCE6EF`, `border-radius: 14px`, `height: ~180px`, JetBrains Mono 13px. Focus: white bg, `1.5px solid #00AFF0`, `box-shadow: 0 0 0 4px rgba(0,175,240,0.12)`. The earlier build made this a second big dark block — two stacked dark voids ("wall of dark"). Dark = the read-only query you're *given*; light = *your* input. The contrast is the affordance.
- Privacy reassurance line under it (12.5px `#64748B`, green 🔒): "Parsed entirely in your browser — resource names never leave your machine."
- **CTA row, in priority order:** primary "Count my nines →" (azure `#00AFF0`, `border-radius: 13px`; **disabled state** = `#B8D9E8` + `cursor: not-allowed` until input parses) · secondary "…or upload the CSV / JSON file" (white outline) · tertiary "Try with sample data" (quiet text link). Three equal filled buttons is wrong — establish this hierarchy.

## Input parsing
Accept **both JSON and CSV** (the Portal path yields CSV). Validity check drives the counter and the primary button's enabled state: try `JSON.parse` → array length; on failure, if it looks like CSV (has a header line with commas + ≥1 data row) count `lines - 1`. "Try with sample data" fills the field with a 3-row sample JSON array.

## Files in this bundle
- `OnlyNines Landing.dc.html` — desktop landing page
- `OnlyNines Landing Mobile.dc.html` — mobile landing page (390px)
- `OnlyNines Assess.dc.html` — assess / input page (corrective spec above)
- `OnlyNines Report.dc.html` — report page, light
- `OnlyNines Report Dark.dc.html` — report page, dark mode
- `OnlyNines Brand.dc.html` — brand board: glyph, app icon, wordmark, palette, type, badge component, OG template
- `support.js` — runtime for the `.dc.html` preview format (reference only; do not ship)

All `.dc.html` files are **design references** — recreate them in the target stack using its own components and styling, per the guidance at the top of this README.
