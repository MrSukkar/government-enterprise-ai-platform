# Platformscode Core

**نظام التصميم الموحّد - كود المنصات** (The Unified Design System - Platforms Code): a build-free, open-source design system from Najran University's **Open-Source Platforms Code** initiative («كود المنصات مفتوحة المصدر»).

## Overview

`platformscode-core` is a precompiled, drop-in distribution of the PFC (Platformscode) Bootstrap Foundation design system. There is no bundler, transpiler, or source/dist split: the files in this repository *are* the shipped artifacts, ready to be linked directly into static HTML, .NET, PHP, Liferay, or any other server-rendered project without a frontend build step.

- **Bootstrap 5.3.8 base**, compiled into `css/main.css` (a generated Bootstrap artifact - do not hand-edit the generated rules; PFC overrides are appended at the end of the file).
- **Green/gold palette**: primary green `#1b8354`, secondary gold `#dba102`, exposed as Bootstrap CSS variables.
- **IBM Plex Sans Arabic** typography (font-family `"IBMPlexSansArabic"`), served locally from `fonts/`.
- **First-class RTL and bilingual (Arabic/English)** support throughout, with bilingual documentation driven by `data-ar` / `data-en`.
- **Light/dark theming** via a `data-theme` attribute on `<html>`.
- **Vanilla-JS behavior modules** (no framework, no imports) that opt in through `data-pfc-*` attributes.

## Directory structure

```text
platformscode-core/
├─ css/
│  └─ main.css          # Compiled Bootstrap 5.3.8 + PFC theme (generated artifact)
├─ docs/                # The documentation site (bilingual, one page per component)
├─ fonts/               # IBM Plex Sans Arabic (.woff2 / .woff)
├─ js/                  # Vanilla-JS behavior modules (core.js registry + components)
├─ templates/           # Page-level layout scaffolds
├─ tests/
│  └─ audit.mjs         # Integrity + source-coverage audit
├─ config.js            # Optional runtime configuration (loads before core.js)
├─ index.html           # Redirect to /docs/home (not a landing page)
├─ serve.json           # Static-server config (cleanUrls + redirects for /docs/*)
├─ package.json
└─ README.md
```

## Quick start / Integration

At minimum, link the compiled stylesheet in your `<head>`. This alone renders every style and static component:

```html
<link rel="stylesheet" href="./css/main.css">
```

For interactive behavior, load the scripts in this exact order:

```html
<!-- 1. Bootstrap bundle: external CDN dependency, NOT shipped in this package -->
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>

<!-- 2. config.js MUST load before core.js -->
<script src="./config.js"></script>
<script src="./js/core.js"></script>

<!-- 3. Only the component modules you actually use -->
<script src="./js/dropdown.js"></script>
```

Notes:

- **`config.js` must load before `core.js`.** It exposes `window.PFC_CONFIG` (direction, language, theme, per-component defaults, `autoInit`) and applies direction/lang/theme to `<html>` on load.
- **The Bootstrap bundle is an external CDN dependency and is not shipped here.** It is only required for Bootstrap-native interactive behavior; PFC modules work independently.
- Components auto-initialize on `DOMContentLoaded` (when `autoInit` is `true`, the default). After injecting HTML dynamically, re-wire just that component with `PFC.initModule('<name>')`.

## Components

Every component is opted in through `data-pfc-*` attributes in the markup. Include only the modules you use; `js/core.js` is the shared registry and must load first.

- **Inputs and forms**: `choice`, `dropdown`, `number-input`, `rating`, `search`, `slider`, `upload`
- **Overlays and feedback**: `alert`, `modal`, `notification`, `overlay`, `toast`
- **Navigation and structure**: `accordion`, `header-menu`, `menu`, `tabs`
- **Content**: `code-snippet`
- **Extended** (all in `js/extended.js`): `carousel`, `datepicker`, `filter`, `signature`

After dynamically inserting a component's HTML, call `PFC.initModule('<name>')` (for example `PFC.initModule('dropdown')`) to wire just that component without re-initializing the whole page.

## Documentation

The `docs/` folder **is** the documentation site (a DGA-style bilingual shell, one focused page per component). It is the single entry point: the root `index.html` is a redirect to `/docs/home`, and `serve.json` provides `cleanUrls` plus redirects so extensionless `/docs/*` URLs resolve.

- `docs/home.html` - hero, overview, and installation guide
- `docs/coverage.html` - source coverage map (React source to open-source component)
- `docs/examples.html` - consolidated component review hub
- `docs/extended.html` - advanced interactive components (carousel, date picker, filters, signature, and more)

## Templates

Page-level scaffolds under `templates/`, embedded as live previews by the docs:

- `templates/header.html`
- `templates/footer.html`
- `templates/sidebar.html`
- `templates/layout-basic.html`
- `templates/layout-dashboard.html`
- `templates/page-shell.html`

## Local preview and audit

This package has no dependencies to install. Start a static preview server:

```bash
npm run serve   # npx serve .
```

Then open the local URL that `serve` prints. Run the integrity and source-coverage audit with:

```bash
npm test        # node tests/audit.mjs
```

## Attribution and license

Developed as part of **Najran University's Open-Source Platforms Code initiative** («كود المنصات مفتوحة المصدر»), within the university's open-source software efforts to unify the design language of digital platforms and offer high-quality, ready-made components to the tech community.

All rights reserved to Najran University.
