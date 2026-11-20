# Métadonnées de recherche et de partage — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal :** rendre la page d'accueil publique indexable et partageable avec des balises lisibles sans JS.

**Architecture :** balises statiques dans `wwwroot/index.html` (servi par l'API pour toute route),
fichiers statiques `robots.txt`, `sitemap.xml` et `og-image.png` dans `wwwroot`, `noindex` de
`/login` par `<HeadContent>`. Aucun changement Domain / Application / Infrastructure / API.

**Tech Stack :** Blazor WebAssembly .NET 10, xUnit (`KairuFocus.Web.Tests`), PowerShell 5.1 +
System.Drawing pour générer l'image.

**Spec :** `docs/superpowers/specs/2026-11-20-metadonnees-recherche-design.md`

## Global Constraints
- URL canonique : `https://kairufocus.com/` (identique dans `canonical`, `og:url`, `sitemap.xml`).
- Image : `https://kairufocus.com/og-image.png`, PNG 1200×630.
- Titre : `KairuFocus — Your developer workspace`. Langue : `en`. Description ≤ 160 caractères.
- Tests nommés `Should_[résultat]_When_[contexte]`.

## Review Focus
- `og:image` relative → aperçu sans image sur la plupart des plateformes : test de l'URL absolue https.
- `robots.txt` qui bloque `/_framework`, `/css`, `/lib` ou `/` → Google ne peut plus rendre la page : test.
- `/login` bloqué dans `robots.txt` → son `noindex` ne serait jamais lu : test.
- `canonical`, `og:url` et `<loc>` divergents → signaux contradictoires : test d'égalité.
- `<PageTitle>` de la page d'accueil (localisé) remplace le titre statique après chargement : contrôle navigateur.

---

### Task 1 : tests des fichiers statiques (rouge)
**Files :** Create `tests/KairuFocus.Web.Tests/StaticSite/StaticSiteFiles.cs` (localise
`src/KairuFocus.Web/wwwroot` en remontant depuis `AppContext.BaseDirectory` jusqu'à `KairuFocus.slnx`),
`tests/KairuFocus.Web.Tests/StaticSite/IndexHtmlMetadataTests.cs`, `RobotsAndSitemapTests.cs`,
`ShareImageTests.cs`.
- [ ] Écrire les tests : `lang="en"` ; titre exact ; description 50–160 caractères ; `robots` = `index, follow` ;
  `canonical` = `og:url` = URL canonique ; `og:type` `website`, `og:site_name`, `og:title`, `og:description`,
  `og:image` = URL de l'image, `og:image:width` 1200, `og:image:height` 630, `og:image:alt` non vide ;
  `twitter:card` `summary_large_image`, `twitter:title`, `twitter:description`, `twitter:image` = `og:image` ;
  `robots.txt` : `Sitemap: https://kairufocus.com/sitemap.xml`, `Disallow: /api/`, aucun `Disallow` sur `/`,
  `/_framework`, `/css`, `/lib`, `/login`, `/og-image.png` ; `sitemap.xml` : un seul `<loc>` égal à l'URL
  canonique ; PNG 1200×630 (en-tête IHDR).
- [ ] `dotnet test tests/KairuFocus.Web.Tests` → échecs attendus (balises et fichiers absents).

### Task 2 : image de partage
- [ ] Générer `src/KairuFocus.Web/wwwroot/og-image.png` (dégradé hero 135°, tuile « K » corail→marine,
  « KairuFocus », accroche « The workspace to stay focused », ligne de fonctions).
- [ ] Contrôle visuel de l'image.

### Task 3 : balises, robots.txt, sitemap.xml, noindex de /login
**Files :** Modify `src/KairuFocus.Web/wwwroot/index.html` (`<head>`), `src/KairuFocus.Web/Pages/Login.razor`
(`<HeadContent><meta name="robots" content="noindex" /></HeadContent>`) ; Create
`src/KairuFocus.Web/wwwroot/robots.txt`, `src/KairuFocus.Web/wwwroot/sitemap.xml`.
- [ ] `dotnet build` puis `dotnet test` → tout vert.

### Task 4 : vérification et commit
- [ ] Lancer le Web, lire le HTML brut servi (`curl`), vérifier `/robots.txt`, `/sitemap.xml`, `/og-image.png`.
- [ ] Contrôler l'aperçu de partage à partir des balises servies, et `noindex` sur `/login` après chargement.
- [ ] Commit `feat(47): métadonnées de recherche et de partage` sur `feature/47-metadonnees-recherche`.
