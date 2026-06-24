---
name: kairufocus-design
description: Use this skill to generate well-branded interfaces and assets for KairuFocus (a developer focus workspace — tasks, Pomodoro, journal), either for production or throwaway prototypes/mocks. Contains essential design guidelines, colors, type, fonts, assets, and UI kit components for prototyping. Built around the Laws of UX.
user-invocable: true
---

# KairuFocus Design System

Read `readme.md` in this skill first — it is the full design guide (sources, content fundamentals, visual foundations, iconography). Then explore the other files.

## What's here
- `styles.css` — the single entry point; `@import`s all tokens + fonts. Link this one file.
- `tokens/` — CSS custom properties: `colors.css` (brand + semantic, light/dark), `typography.css`, `spacing.css`, `radius-elevation-motion.css`, `base.css`.
- `components/` — React primitives (`buttons/`, `forms/`, `data-display/`, `feedback/`). Each has a `.jsx`, `.d.ts`, `.prompt.md` and a showcase card.
- `ui_kits/kairufocus-web/` — interactive click-through recreation of the real app (landing, dashboard, tasks, pomodoro, journal, settings).
- `guidelines/` — foundation specimen cards (color, type, spacing, brand).
- `assets/` — the KairuFocus "K" logo (192 & 512px).

## How to work
- If creating **visual artifacts** (slides, mocks, throwaway prototypes): copy assets out and build static HTML files. Link `styles.css`, then either use the token CSS variables directly or mount components from the compiled bundle (`window.<Namespace>`, e.g. `const { Button } = window.KairuFocusDesignSystem_...`). Read a component's `.prompt.md` for usage.
- If working on **production code** (the Blazor app): treat the tokens as the source of truth. Wire `--bs-*` Bootstrap vars to the semantic aliases (see `tokens/base.css`), keep all visible strings in `@Loc`/`.resx` (never hard-code), and verify both `data-bs-theme` light + dark.

## Non-negotiables (from the product brief)
- Every color must work in **light AND dark**.
- **WCAG 2.1 AA**: text contrast ≥ 4.5:1, focus always visible (never `outline:none` without a replacement), tap targets ≥ 44px, real `aria-label`s on icon-only buttons.
- Respect `prefers-reduced-motion`; keep motion short (< 400ms) and never distract from focus.
- Stay on Bootstrap 5 + custom CSS — no new UI framework.
- **Trace every visual/interaction choice to a named Law of UX.**

If invoked without guidance, ask what the user wants to build or design, ask a few focused questions, and act as an expert KairuFocus designer — outputting HTML artifacts or production-ready CSS/markup as the need dictates.
