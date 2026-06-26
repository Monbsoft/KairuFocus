# KairuFocus Design System

A centralized design system for **KairuFocus** — *“L’espace de travail pour rester focus.”* — a daily-activity workspace for developers. It pulls everything a developer needs to stay focused into one place: a micro-task todo list, an automatically-generated activity journal, and Pomodoro focus sessions, all behind GitHub sign-in.

This project distills the real KairuFocus web app (Blazor WebAssembly + Bootstrap 5, light/dark via `data-bs-theme`) into reusable design tokens, components, foundation specimens, and a click-through UI kit. It exists to make every new KairuFocus surface look and behave consistently — the brief’s north star: **cohérence = usabilité perçue** (the Aesthetic-Usability Effect).

> Built explicitly around the **Laws of UX** (lawsofux.com). Where a decision has a law behind it, the law is named — in token comments, component docs, and the cards.

---

## Sources

The system was reverse-engineered from the product source. If you have access, explore these to go deeper:

- **GitHub — app & UI code:** [`Monbsoft/KairuFocus`](https://github.com/Monbsoft/KairuFocus) (branch `main`). The presentation layer lives in `src/KairuFocus.Web/` — `Pages/*.razor` (Home, Dashboard, Tasks, Pomodoro, Journal, Settings, Login), `Layout/` (NavMenu, MainLayout), `wwwroot/css/app.css`, and `Helpers/` (TagColors, JournalEntryMeta).
- **Specs (private):** [`Monbsoft/kairu-specs`](https://github.com/Monbsoft) — architecture decisions & project state, if shared with you.

Everything visual here was lifted from that code: the navy→purple sidebar gradient, the blue primary, the coral logo accent, the Pomodoro ring state-colors, the hashed tag palette, and the journal timeline motif. Reading the repo will let you extend this system with higher fidelity.

> **Stack guardrails (from the product brief):** Blazor WASM + Bootstrap 5 (CSS only) + custom CSS. No new UI framework (no MudBlazor / Tailwind / Radzen). All visible strings go through i18n (`@Loc` / `.resx`, 10 languages) — never hard-code user-facing copy. Every color must work in light **and** dark. Touch the presentation layer only.

---

## Content fundamentals

**Language.** The product UI is **French** (the codebase localizes to 10 languages via `.resx`, but French is the design reference). Marketing/landing copy in the repo also ships in English. When writing KairuFocus copy, default to French for in-app surfaces.

**Voice & tone.** Calm, direct, developer-to-developer. Short imperative labels for actions: *“Ajouter”*, *“Démarrer”*, *“Interrompre”*, *“Enregistrer”*, *“Lier”*. Encouraging at the peaks: end-of-sprint messages are warm and celebratory — *“Sprint terminé !”*, *“Pause recommandée : longue”* (Peak-End). Empty states are pedagogical, not dead-ends: *“Aucune activité enregistrée ce jour. Démarrez un sprint Pomodoro pour voir apparaître vos entrées ici.”*

**Casing.** Sentence case everywhere — labels, buttons, headings (*“Tâches du sprint”*, not Title Case). Uppercase is reserved for small eyebrow/section labels with wide tracking (*“ACCÈS RAPIDES”*, the journal day name *“LUNDI”*).

**Person.** Addresses the user warmly and directly — *“Bonjour, dev 👋”* — then otherwise speaks in neutral object labels (*“Tâches en cours”*, *“Entrées aujourd’hui”*). Not chatty; informative.

**Emoji.** Yes — emoji are a deliberate, load-bearing part of the iconography (see Iconography). Feature blocks, stat tiles, Pomodoro session types, and journal events all use emoji as their icon. Decorative emoji are marked `aria-hidden`. Keep them purposeful, one per concept — never sprinkled for flavor.

**Numbers & time.** The Pomodoro clock and durations use **monospaced, tabular** figures (`24:59`). Dates are written long and lower-cased in French (*“lundi 3 mars 2025”*).

**Examples to emulate**
- Tagline: *“The workspace to stay focused.”* / *“L’espace de travail pour rester focus.”*
- Feature blurb: *“Daily micro-tasks with statuses, descriptions and Markdown editing.”*
- Success toast: *“✓ Paramètres enregistrés avec succès !”*
- Error: *“Erreur lors de l’ajout de la tâche.”*

---

## Visual foundations

**Overall vibe.** A focused, modern developer tool in the lineage of Linear / GitHub / Todoist (Jakob’s Law — meet existing mental models). Restrained, content-first, with one confident brand gesture: a deep **navy→purple** gradient on the persistent sidebar rail and hero.

**Color.** Built on a blue primary (`#0d6efd`, the “K” mark), a coral accent (`#e94560`, from the logo, used sparingly as an isolation pop), and Bootstrap-aligned semantic hues (success green, danger red, warning amber, info cyan, plus a task purple seen in the journal dots). Neutrals split by theme: a **warm-gray** ramp for light surfaces, a **cool-slate** ramp for dark. Two invariant brand surfaces stay constant across themes: the sidebar gradient (`#052767 → #3a0647`) and the hero gradient (`#1a1a2e → #16213e → #0f3460`). Every semantic color is exposed as a theme-aware alias (`--surface-card`, `--text-primary`, `--primary`, `--success`…) so a single token works in both themes. Status colors come in three flavors — solid, `-subtle` background, and a readable `-text` for text-on-subtle (AA-checked).

**Typography.** Both families are **native system stacks** — fast, zero-load, no third-party CDN. Sans honors the app’s original `Helvetica Neue` heritage. The numeric/code voice is a **developer-monospace stack** that prefers the devtool faces a developer is likely to already have installed locally — `JetBrains Mono` → `Cascadia Code` → the OS UI mono (`ui-monospace`) → classic system monos — used for timers, code, tag-as-data, and tabular figures. No webfont is fetched (the app only specified a generic `monospace` for the clock; this gives that voice real character without an external request). A 1.20 minor-third scale from 12px → 48px. Large display sizes use tight tracking (`-0.02em`); uppercase eyebrows use wide tracking.

**Spacing & layout.** A single 4px-based scale. Fixed structural sizes: a **64px** sidebar rail and a **3.5rem** top row (both lifted from the app). Reading/content widths are capped — 600px for forms/Settings, 700px for the Journal, 960px for the Dashboard. The sidebar is sticky and persistent (Jakob); the active nav item is highlighted (Von Restorff).

**Backgrounds.** Mostly flat, theme-aware surfaces — no textures, no photography. Gradients are intentional and limited to the two brand surfaces (sidebar, hero) plus the brand logo tile. No full-bleed imagery.

**Corners & cards.** Rounded but not soft: 4px chips, 6px controls, 8px menus/nav-icons, **12px cards** (the dominant radius), 18px on the logo tile. A card is a `--surface-card` panel with a 1px `--border-subtle` and a resting `--shadow-sm`; interactive cards **lift** on hover (`translateY(-2px)` + `--shadow-md`). Active/selected cards switch to a green border + green-subtle tint.

**Elevation.** A five-step shadow ramp (xs→xl) that reads softly on light and heavier on dark (re-tuned per theme). Inputs rest at xs, cards at sm, hovers at md, feature-card hovers at lg, dialogs at xl. One special **brand glow** (`--shadow-brand`, a blue cast) for the hero logo.

**Motion.** Short and focus-respecting (Doherty < 400ms): durations 100/150/200/300ms with a standard `cubic-bezier(0.2,0,0.2,1)` ease (and an `ease-out` for entrances). Hover/press feedback only — buttons depress 1px on `:active`. No gratuitous looping animation (it would break focus). `prefers-reduced-motion` zeroes all durations globally.

**Hover / press / focus states.** Hover = subtle surface tint (`--surface-hover`) or a one-step-darker primary; press = 1px translate-down; focus = a **2px `--focus-ring` outline at 2px offset**, always visible (never `outline:none` without a replacement — WCAG). Removable tag `×` and row actions reveal/strengthen on hover.

**Borders & dividers.** Hairline 1px borders in `--border-subtle`/`--border-default`; the journal uses a 2px vertical timeline line with colored dots (the “connexion uniforme” Gestalt motif). Section dividers are quiet.

**Transparency & blur.** Minimal. The sidebar uses translucent white overlays for hover/active states (`rgba(255,255,255,.15/.37)`); modals use a scrim (`--overlay-scrim`). No frosted-glass blur in the base system.

**Imagery vibe.** There is essentially no photographic imagery — the product is UI-chrome and data. The only “image” is the app icon (white **K** on brand blue). Color temperature overall reads cool (blue/navy/slate) with the warm coral as the single accent.

---

## Iconography

See the **ICONOGRAPHY** section below.

---

## Iconography

KairuFocus uses **three** icon registers, in this order of preference:

1. **Emoji as primary iconography.** This is the app’s signature. Modules, stat tiles, Pomodoro session types, and journal events are all keyed by emoji, rendered at the system emoji font:
   - Modules / nav: ☑️ Tasks · 🍅 Pomodoro · 📖 Journal · ⚙️ Settings · 🔐 Auth
   - Pomodoro sessions: 🍅 Sprint · ☕ Short break · 🌙 Long break
   - Journal events: 🍅 Sprint started · ✅ Sprint completed · ⏸️ Sprint interrupted · ☕ Break started · 🌿 Break completed · ⚡ Break interrupted · 🚀 Task started · 🎉 Task completed · 📌 fallback
   - Feedback: ✓ success · ⚠ error · ℹ info · 👋 greeting
   Decorative emoji must carry `aria-hidden="true"`; when an emoji is the *only* content of a control, pair it with an `aria-label`.

2. **Inline stroke SVG (Feather / Lucide style)** for structural UI chrome where emoji would feel heavy: the dashboard **home** glyph and the **logout** glyph in the nav, and the **GitHub** mark on the sign-in button. These are 24×24, `stroke="currentColor"`, `stroke-width: 2.5`, round caps/joins — so they inherit text color and theme automatically. The components in this system (chevrons, check, pencil, cross) follow the same spec. **Match this stroke weight if you add icons.**

3. **Unicode glyphs** for a few micro-controls: `←` `→` journal day nav, `×` tag-remove, `···` overflow menu, `▶` start.

**Guidance for new work.** Prefer the app’s existing register: emoji for concepts, Feather/Lucide stroke SVG (weight 2.5) for chrome. The repo has **no icon font and no SVG sprite** — icons are authored inline. If you need a glyph that isn’t in the app, pull it from **[Lucide](https://lucide.dev)** (closest match to the existing hand-rolled SVGs — same stroke style) rather than inventing one. Don’t mix in a different icon family’s visual language.

**Brand assets** (in `assets/`): `kairufocus-logo-512.png` and `kairufocus-logo-192.png` — the white **K** on brand-blue app mark. The “@”-flame favicon that ships in the repo is the default Blazor template icon, **not** the KairuFocus brand, and was intentionally excluded.

---

## ⚠️ Substitutions & flags — please confirm

- **Monospace = zero-load system stack.** The app only specifies a generic `monospace` for the Pomodoro clock. Rather than pull a webfont from a third-party CDN (Google Fonts sends the visitor’s IP to Google at render time — a GDPR concern if this CSS is ever served to real users), `--font-mono` prefers devtool faces a developer likely already has locally (`JetBrains Mono` → `Cascadia Code` → `ui-monospace`) and degrades to the classic system monos. No external request, offline-friendly, instant. **If you want a guaranteed-identical face on every machine, self-host a woff2 (JetBrains Mono is OFL — drop it in `assets/` and add a local `@font-face`) rather than re-introducing the remote `@import`.**
- **System sans** is used for body/UI rather than a branded webfont, to stay faithful and fast. If you’d like a distinctive heading face (e.g. Geist), say so.
- The token color values systematize the app’s scattered inline colors into a clean light/dark scale — a few in-between steps were interpolated. Spot-check against your brand intent.

---

*(Index of everything in this project is at the bottom — see “Project index”.)*

---

## Project index

**Root**
- `styles.css` — the single entry point (import manifest only). Consumers link this.
- `readme.md` — this guide. `SKILL.md` — Agent-Skill manifest for Claude Code.
- `assets/` — `kairufocus-logo-512.png`, `kairufocus-logo-192.png` (white K on brand blue).

**Tokens** (`tokens/`, all `@import`ed by `styles.css`)
- `colors.css` — base palette + theme-aware semantic aliases (light + dark).
- `typography.css` — families (system sans + zero-load developer-mono stack), scale, weights, leading, tracking.
- `spacing.css` — 4px scale + structural sizes + hit targets.
- `radius-elevation-motion.css` — radii, shadow ramp, durations & easing.
- `base.css` — element defaults + Bootstrap `--bs-*` bridge + focus ring.

**Components** (`components/`) — `window.KairuFocusDesignSystem_*`
- `buttons/` — `Button`, `IconButton`
- `forms/` — `Input`, `Textarea`, `Select`, `Checkbox`
- `data-display/` — `Badge`, `Tag`, `Card`, `StatCard`, `Avatar`
- `feedback/` — `Alert`, `ProgressRing` (Pomodoro), `Skeleton`

**UI kit** (`ui_kits/kairufocus-web/`) — interactive recreation: Landing, Dashboard, Tasks, Pomodoro, Journal, Settings.

**Foundation cards** (`guidelines/`) — 15 specimen cards across Colors, Type, Spacing, Brand (shown in the Design System tab).
