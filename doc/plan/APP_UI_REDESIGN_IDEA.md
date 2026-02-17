# CozyLedger App UI Redesign Idea (Screenshot-Aligned)

## Scope

This document defines a full-app visual redesign direction based on screenshots in `doc/_sources/IMG_1867.PNG` to `IMG_1875.PNG`.

It covers:

- `frontend/src/pages/DashboardPage.tsx`
- `frontend/src/pages/LedgerPage.tsx`
- `frontend/src/pages/ReportsPage.tsx`
- `frontend/src/pages/AccountsPage.tsx`
- `frontend/src/pages/MembersPage.tsx`
- Shared shell and navigation in `frontend/src/App.css`

No implementation is included here. This is a pre-coding reference.

## Screenshot Pattern Library

The screenshots are not one-page examples; they define an end-to-end app style:

- `IMG_1867`: home/dashboard with shortcut chips, hero summary, period blocks, chart card.
- `IMG_1868`: ledger list with dark grouped timeline rows and fixed filter bar.
- `IMG_1869`: reports page with tabs + summary hero + ranked distribution lines.
- `IMG_1871`: members page with grouped member cards and strong CTA footer button.
- `IMG_1872`: accounts list page with hero totals and compact account rows.
- `IMG_1873`: chart detail page (pie) with strong center metric and bottom mode switch.
- `IMG_1874`: account type picker list.
- `IMG_1875`: account form page with dark inputs, switches, and large rounded save button.

## Global Visual Direction

- Dark, layered, mobile-native style (not desktop web default panels).
- Continuous depth: page background + elevated cards, all in navy/charcoal.
- Rounded geometry with medium/large radii.
- Accent colors have fixed semantics:
  - Cyan/teal: positive totals, data highlights.
  - Orange: primary CTA, active controls.
  - Red: expense/negative emphasis.
- Sparse borders and subtle separators; no bright box outlines.

## Global Tokens (Proposed)

- `--cl-bg-0: #0e1119`
- `--cl-bg-1: #151a27`
- `--cl-surface-0: #1a2030`
- `--cl-surface-1: #20273a`
- `--cl-border-soft: rgba(255,255,255,0.06)`
- `--cl-text-primary: #f2f5fe`
- `--cl-text-secondary: #98a0b3`
- `--cl-text-muted: #6d7588`
- `--cl-accent-cyan: #2fc4db`
- `--cl-accent-teal: #26d2a5`
- `--cl-accent-orange: #e6a25d`
- `--cl-accent-red: #ea6d6d`
- `--cl-radius-lg: 18px`
- `--cl-radius-md: 14px`
- `--cl-shadow-soft: 0 8px 24px rgba(0,0,0,0.22)`

## Global Layout Rules

1. App Shell
- Replace current light beige gradients with dark layered background.
- Desktop nav and mobile nav should use the same style language as cards.
- Keep navigation compact; prioritize content density.

2. Card System
- All content sits in dark cards with subtle elevation.
- Internal spacing: 12-16px.
- Card-to-card spacing: 12-14px.

3. Controls
- Inputs/selects/switches become pill or rounded-rectangle dark controls.
- Touch-friendly minimum control height (44px).
- Active state uses orange accent.

4. Typography
- Strong white headings; muted gray metadata.
- Large numeric focus for financial values.
- Dense but readable list rows.

## Page-by-Page Mapping

1. Dashboard (`DashboardPage.tsx`)
- Match `IMG_1867` structure:
  - Header strip + quick action chips (future-friendly container even if actions are placeholders initially).
  - Hero monthly summary card with dominant net value.
  - Three period/stat blocks and mini chart/list section below.
- Existing 3 summary tiles should become one visual group with clearer hierarchy.

2. Ledger (`LedgerPage.tsx`)
- Match `IMG_1868` timeline list style:
  - Dark grouped day sections.
  - Compact transaction rows with icon slot, title, metadata, right-aligned amount.
  - Sticky/fixed bottom filter/action rail style for type/category/account filters.
- Keep create/edit form functionality, but style form and list as separate dark modules.

3. Reports (`ReportsPage.tsx`)
- Match `IMG_1869` + `IMG_1873`:
  - Top tabs/filter strip.
  - Hero summary card.
  - Category distribution with ranked bars.
  - Chart visuals tuned for dark surfaces.
- Pie chart center label and emphasis style should follow `IMG_1873`.

4. Members (`MembersPage.tsx`)
- Match `IMG_1871`:
  - Group invited members and active members into separate card blocks.
  - Inline role labels/tags.
  - Strong full-width bottom CTA style for invite actions.

5. Accounts (`AccountsPage.tsx`)
- Match `IMG_1872`, `IMG_1874`, `IMG_1875`:
  - Account overview hero (net/asset summary area).
  - Dense account rows with right-aligned balances.
  - Account type picker as simple dark list card.
  - Account form fields with dark separators and rounded bottom primary action.

## Chart Styling Rules

- Gridlines/axes use muted tones only.
- Tooltips are dark with thin border and compact text.
- Limit per-chart color count; avoid rainbow defaults except where category semantics need distinct slices.
- Keep currency formatting consistent and 2-decimal precision.

## Responsive Rules

- Mobile-first stacking is default behavior.
- Desktop keeps same mood and density; may split modules into two columns only when readable.
- Do not force side-by-side dense charts under narrow widths.

## Implementation Acceptance Criteria

- Entire app shifts from light-beige dashboard style to screenshot-aligned dark design language.
- Visual consistency across all pages, not just reports.
- Existing data and interactions remain fully functional.
- Mobile layout feels native-app-like, with clear tap targets and compact information density.
