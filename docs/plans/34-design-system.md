# Plan — Adoption du design system KairuFocus dans l'app Blazor
**Date :** 2026-06-24
**Itération :** #34
**Statut :** ready

---

## Objectif et périmètre

Câbler le design system KairuFocus (tokens CSS + composants Blazor) comme
source de vérité visuelle pour `KairuFocus.Web` (Blazor WebAssembly, .NET 10,
Bootstrap 5 CSS-only, PWA).

**In scope :**
- Tous les écrans de `KairuFocus.Web` : Layout, Auth, Pages (10 fichiers .razor).
- Police JetBrains Mono en self-host (woff2).
- Refactor de `app.css` token par token.
- Composants Blazor partagés portés depuis les .jsx du DS.

**Out of scope :**
- `KairuFocus.Maui` — les pages jumelles seront traitées dans une future itération.
  Le plan doit mentionner en commentaire que les tokens copiés dans
  `wwwroot/css/design-system/` seront réutilisés tel quel par Maui.
- Logique métier, API, Domain, Application, Infrastructure : aucune touche.
- Ajout de framework JS supplémentaire.

**Architecture :** couche Présentation uniquement. Aucune dépendance vers les
couches intérieures n'est créée ni modifiée.

---

## Observations issues de l'exploration

### `app.css` actuel — problèmes identifiés

| Ligne | Problème | Correction |
|---|---|---|
| 5 | `h1:focus { outline: none }` — violation WCAG 2.1 AA | Supprimer |
| 10–11 | `.btn-primary` hardcodé `#1b6ec2` / `#1861ac` | Remplacer par tokens |
| 19–21 | `.btn:focus` glow blanc box-shadow override | Supprimer (`:focus-visible` géré par `base.css`) |
| 25 | `a, .btn-link { color: #0071c1 }` | Remplacer par `var(--primary)` |
| 170–225 | `.landing-hero` gradient hex en dur | Remplacer par `var(--brand-hero)` |
| 229 | `[data-bs-theme="dark"] .landing-features { background: #1a1a2e }` | Remplacer par `var(--surface-base)` |
| 232–249 | `.feature-card` hex + doublons dark | Remplacer par tokens |
| 299–333 | `.stat-card` + dark doublons hex | Remplacer par tokens + `StatCard` composant |
| 363 | `.dashboard-section-title color: #6c757d` | `var(--text-muted)` |
| 375–408 | `.quicklink-card` + dark doublons | Remplacer par tokens |

### `Journal.razor` — CSS inline dans `<style>` embarqué
Le journal embeds ses styles directement dans le fichier .razor (lignes 128–449).
Ils doivent être extraits vers `Journal.razor.css` et portés sur tokens.
Les variables `--nb-*` locales survivent (elles sont déjà thème-aware) mais leurs
valeurs hex doivent pointer vers les tokens DS quand ils existent.

### Chaînes en dur détectées
- `Dashboard.razor` ligne 25 : `"Bonjour, @_userName &#128075;"` — pas de `@Loc`.
- `Dashboard.razor` ligne 26 : date formatée en `"fr-FR"` hardcodé — idem.
- `Pomodoro.razor` : labels de session types en dur ("Sprint en cours", etc.) — accepté car non-i18n pour l'instant (hors scope de cette PR, noter dans Écarts).
- `Home.razor` : utilise correctement `@Loc` pour tous ses textes.
- `Login.razor` : chaînes en dur ("Connexion -- KairuFocus", "Se connecter avec GitHub", sous-titre) — à passer via `@Loc` **dans cette PR**.
- `SprintLibre.razor` : labels "Démarrer", "Terminer", "Interrompre", "Sprints du jour" en dur.

### Service worker
`service-worker.published.js` précache via `service-worker-assets.js` généré au
`dotnet publish` — toute ressource dans `wwwroot/` est automatiquement incluse.
**Aucune entrée manuelle n'est nécessaire** pour les polices ajoutées dans
`wwwroot/css/design-system/fonts/`.
Le filtre `offlineAssetsInclude` couvre déjà `.css` et tout fichier statique.

### Bootstrap vars — pont existant vs complet
`base.css` câble 6 vars Bootstrap. Manquants à ajouter dans le pont :

| `--bs-*` à ajouter | Source token |
|---|---|
| `--bs-primary-rgb` | `13, 110, 253` (kf-blue-600 décomposé) |
| `--bs-link-color` | `var(--primary)` |
| `--bs-link-hover-color` | `var(--primary-hover)` |
| `--bs-success` | `var(--success)` |
| `--bs-danger` | `var(--danger)` |
| `--bs-warning` | `var(--warning)` |
| `--bs-info` | `var(--info)` |
| `--bs-card-bg` | `var(--surface-card)` |
| `--bs-card-border-color` | `var(--border-subtle)` |
| `--bs-modal-bg` | `var(--surface-elevated)` |

---

## Phase 0 — Fondation CSS

### 0.1 Copie des tokens dans wwwroot

Créer la structure :

```
src/KairuFocus.Web/wwwroot/css/design-system/
  colors.css
  typography.css
  spacing.css
  radius-elevation-motion.css
  base.css
  design-system.css          ← manifeste @import (copié de styles.css)
  fonts/
    JetBrainsMono-Regular.woff2
    JetBrainsMono-Medium.woff2
    JetBrainsMono-SemiBold.woff2
    JetBrainsMono-Bold.woff2
```

Copier mot pour mot les fichiers de `.claude/skills/kairufocus-design/tokens/`.
Ne pas modifier leur contenu : ils sont la source de vérité.

### 0.2 Self-host JetBrains Mono

**Télécharger** les 4 fichiers `.woff2` (Regular 400, Medium 500, SemiBold 600,
Bold 700) depuis la release officielle JetBrains Mono (licence OFL, libre de
redistribution). Les placer dans `wwwroot/css/design-system/fonts/`.

**Dans `typography.css` copié**, à la ligne 10, **supprimer** :
```css
@import url('https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500;600;700&display=swap');
```

**Remplacer par** le bloc `@font-face` suivant (à insérer en tête du fichier) :
```css
@font-face {
  font-family: 'JetBrains Mono';
  src: url('fonts/JetBrainsMono-Regular.woff2') format('woff2');
  font-weight: 400; font-style: normal; font-display: swap;
}
@font-face {
  font-family: 'JetBrains Mono';
  src: url('fonts/JetBrainsMono-Medium.woff2') format('woff2');
  font-weight: 500; font-style: normal; font-display: swap;
}
@font-face {
  font-family: 'JetBrains Mono';
  src: url('fonts/JetBrainsMono-SemiBold.woff2') format('woff2');
  font-weight: 600; font-style: normal; font-display: swap;
}
@font-face {
  font-family: 'JetBrains Mono';
  src: url('fonts/JetBrainsMono-Bold.woff2') format('woff2');
  font-weight: 700; font-style: normal; font-display: swap;
}
```

Note : `font-display: swap` garantit du texte visible pendant le chargement
(Doherty — pas de FOIT).

### 0.3 Câblage dans index.html

Dans `wwwroot/index.html`, **avant** `<link rel="stylesheet" href="css/app.css" />`,
insérer :
```html
<link rel="stylesheet" href="css/design-system/design-system.css" />
```

L'ordre final des feuilles dans `<head>` :
1. `bootstrap.min.css`
2. `css/design-system/design-system.css`  ← **nouveau**
3. `css/app.css`
4. `KairuFocus.Web.styles.css`

Aucune touche au service worker : les fichiers dans `wwwroot/` sont
automatiquement précachés par le pipeline `dotnet publish`.

### 0.4 Compléter le pont Bootstrap dans base.css

Ajouter dans le bloc `:root, [data-bs-theme="light"], [data-bs-theme="dark"]`
(ou en surcharge dans le bloc `:root` uniquement pour les valeurs non-thémées)
les variables manquantes listées dans la section Observations ci-dessus.

Note sur `--bs-primary-rgb` : Bootstrap l'utilise pour les opacités
(`rgba(var(--bs-primary-rgb), 0.15)` dans les `.btn-check` actifs).
Valeur fixe `13, 110, 253` en light, `43, 125, 255` en dark.

### 0.5 Refactor app.css

Règle : **aucun hex en dur ne subsiste** dans `app.css` après cette phase,
sauf pour les assets qui n'ont pas de token (ex. `loading-progress` couleur
initiale peut rester `#e0e0e0` ou passer sur `var(--border-default)`).

Transformations token par token :

**Focus (bugs WCAG — priorité absolue) :**
- Supprimer `h1:focus { outline: none }` (ligne 5).
- Supprimer le bloc `.btn:focus, .btn:active:focus, ... { box-shadow: ... white ... }` (lignes 19–21).
- `base.css` prend le relais avec `:focus-visible { outline: 2px solid var(--focus-ring) }`.

**Liens :**
- `.btn-primary` → supprimer (Bootstrap + pont `--bs-primary` suffisent).
- `a, .btn-link { color: #0071c1 }` → `color: var(--primary)` ou supprimer
  (déjà géré par `base.css`).

**Landing :**
- `.landing-hero { background: linear-gradient(135deg, #1a1a2e...) }` → `background: var(--brand-hero)`.
- `.landing-logo { background: linear-gradient(135deg, #e94560, #0f3460) }` → `background: linear-gradient(135deg, var(--kf-coral-500), var(--kf-ink-700))` + `box-shadow: var(--shadow-brand)`.
- `.landing-title { font-size: 3rem; font-weight: 800; letter-spacing: -0.02em }` → `font-size: var(--text-3xl); font-weight: var(--weight-extra); letter-spacing: var(--tracking-tight)`.
- `.landing-tagline { font-size: 1.2rem }` → `font-size: var(--text-md)`.
- `.landing-desc { font-size: 1rem; line-height: 1.7 }` → `font-size: var(--text-base); line-height: var(--leading-relaxed)`.
- `.landing-features { background: #f8f9fa }` → `background: var(--surface-base)`.
- `[data-bs-theme="dark"] .landing-features { background: #1a1a2e }` → **supprimer** (token `--surface-base` gère le dark automatiquement).
- `.feature-card { background: #fff; border-radius: 12px; box-shadow: ... }` → `background: var(--surface-card); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm)`.
- `.feature-card:hover { box-shadow: 0 8px 24px ... }` → `box-shadow: var(--shadow-lg)`.
- Transition : `transition: transform var(--duration-base) var(--ease-standard), box-shadow var(--duration-base) var(--ease-standard)`.
- `[data-bs-theme="dark"] .feature-card { background: #16213e }` → **supprimer**.
- `.feature-desc { color: #6c757d }` + dark override → `color: var(--text-muted)` (supprimer le dark override).

**Dashboard :**
- `.dashboard-greeting { font-size: 1.8rem }` → `font-size: var(--text-2xl)`.
- `.dashboard-date { color: #6c757d }` → `color: var(--text-muted)`.
- `.stat-card { background: #fff; border: 1px solid #e9ecef; border-radius: 12px }` → `background: var(--surface-card); border-color: var(--border-subtle); border-radius: var(--radius-lg)`.
- `.stat-card:hover { box-shadow: 0 4px 16px rgba(0,0,0,0.1) }` → `box-shadow: var(--shadow-md)`.
- `[data-bs-theme="dark"] .stat-card { background: #1e293b; border-color: #334155 }` → **supprimer**.
- `.stat-card.stat-active { border-color: #198754; background: #f0fdf4 }` → `border-color: var(--success); background: var(--success-subtle)`.
- `[data-bs-theme="dark"] .stat-card.stat-active { background: #052e16; border-color: #16a34a }` → **supprimer**.
- `.stat-label { color: #6c757d }` → `color: var(--text-muted)`.
- `.dashboard-section-title { color: #6c757d; letter-spacing: 0.08em }` → `color: var(--text-muted); letter-spacing: var(--tracking-wide)`.
- `.quicklink-card { background: #fff; border: 1px solid #e9ecef; border-radius: 12px }` → tokens.
- `[data-bs-theme="dark"] .quicklink-card { background: #1e293b; ... }` → **supprimer**.
- `.quicklink-card:hover { border-color: #0d6efd; color: #0d6efd }` → `border-color: var(--primary-border); color: var(--primary)`.

**Pomodoro menu / logout :**
- `.btn-github { background: #ffffff; color: #24292e; border-radius: 6px }` → `background: #fff; color: #24292e; border-radius: var(--radius-sm)` (garder la couleur blanche ici : c'est la couleur brand GitHub, invariante).
- `transition: background 0.15s, box-shadow 0.15s` → `transition: background var(--duration-fast) var(--ease-standard), box-shadow var(--duration-fast) var(--ease-standard)`.
- `.btn-logout { transition: opacity 0.2s }` → `transition: opacity var(--duration-base) var(--ease-standard)`.

**Journal timeline (inline dans Journal.razor) :**
- Extraire tout le `<style>` en `Journal.razor.css`.
- Les variables locales `--nb-*` peuvent rester, mais pointer vers les tokens DS quand ils existent :
  - `--nb-sprint-dot-bg: var(--primary-subtle)`
  - `--nb-break-dot-bg: var(--success-subtle)`
  - `--nb-task-dot-bg: var(--task-subtle)`
  - Lignes de l'entrée sprint `.nb-sprint .nb-dot { border-color: #0d6efd }` → `border-color: var(--primary)`.
  - `.nb-break .nb-dot { border-color: #198754 }` → `border-color: var(--success)`.
  - `.nb-task .nb-dot { border-color: #6f42c1 }` → `border-color: var(--task)`.
  - `.nb-warn .nb-dot { border-color: #fd7e14 }` → `border-color: var(--kf-orange-500)`.
  - `.nb-add-btn:hover { border-color: #0d6efd; color: #0d6efd }` → tokens primary.
  - `.btn-icon:hover { color: #0d6efd }` → `color: var(--primary)`.
  - `.btn-icon-danger:hover { color: #dc3545 }` → `color: var(--danger)`.
  - `.notebook-header { background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%) }` → `background: var(--brand-hero)`.

**Pomodoro.razor (SVG inline) :**
- `stroke="#e9ecef"` (track) → utiliser `var(--pomo-track)` via attribut `stroke` inline ou
  passer en CSS isolé `.pomodoro-track { stroke: var(--pomo-track) }`.
- `stroke="@ArcColor"` (arc computed) → remplacer la méthode `ArcColor` retournant des hex
  par un mapping sur les tokens CSS : utiliser `fill="currentColor"` + classe CSS, ou
  conserver les hex mais les mapper sur les valeurs exactes des tokens
  (`var(--pomo-sprint)=#0d6efd` light, `#2b7dff` dark — le SVG inline ne voit pas les CSS vars,
  donc extraire le cercle SVG dans un composant `ProgressRing.razor` avec CSS isolé).
  **Recommandation :** créer le composant `ProgressRing.razor` (voir Phase 1.4).
- `fill="#6c757d"` (statut text) → le SVG `<text>` n'interprète pas les CSS vars directement
  via `fill`. Utiliser `fill="currentColor"` + surcharger `color` en CSS isolé.

**Login.razor.css :**
- `.login-logo { background: #0d6efd }` → `background: var(--brand-mark-bg)`.
- `.login-card { border: 1px solid rgba(255,255,255,0.1) }` → `border: 1px solid var(--border-subtle)`.
- `.login-subtitle { color: var(--bs-secondary) }` → `color: var(--text-muted)` (évite la dépendance à la var Bootstrap non-pontée).
- Ajouter `box-shadow: var(--shadow-sm)` à `.login-card` (alignement DS LoginScreen.jsx).

**Blazor error UI (garder, mais tokeniser) :**
- `#blazor-error-ui { background: lightyellow }` → peut rester (c'est un état d'erreur framework, pas UX produit).
- Loading progress `stroke: #1b6ec2` → `stroke: var(--primary)`.

---

## Phase 1 — Composants Blazor partagés ✅ complétée le 2026-06-24

Tous les composants vivent dans `src/KairuFocus.Web/Components/Design/`.
Chaque composant = un fichier `.razor` + un `.razor.css` isolé.
Aucun code C# métier dans ces composants : présentation pure.

### 1.1 Button.razor

**Emplacement :** `Components/Design/Button.razor`

**Paramètres :**
```csharp
[Parameter] public string Variant { get; set; } = "primary";
// primary | secondary | ghost | danger | accent
[Parameter] public string Size { get; set; } = "md";
// sm (36px) | md (44px) | lg (48px)
[Parameter] public string? As { get; set; }
// null = <button> | "a" = <a>
[Parameter] public string? Href { get; set; }
[Parameter] public bool Disabled { get; set; }
[Parameter] public RenderFragment? Icon { get; set; }
[Parameter] public RenderFragment? IconRight { get; set; }
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public EventCallback OnClick { get; set; }
[Parameter(CaptureUnmatchedValues = true)]
public Dictionary<string, object>? AdditionalAttributes { get; set; }
```

**Markup pattern :**
- Si `As == "a"` : `<a href="@Href" class="kf-btn kf-btn--@Variant kf-btn--@Size" @attributes="AdditionalAttributes">`.
- Sinon : `<button type="button" class="kf-btn kf-btn--@Variant kf-btn--@Size" disabled="@Disabled" @onclick="OnClick" @attributes="AdditionalAttributes">`.
- Icônes wrappées dans `<span aria-hidden="true">`.

**CSS isolé — classes (`Button.razor.css`) :**
Porter exactement le CSS du `Button.jsx` source. Classes : `.kf-btn`, `.kf-btn--{variant}`, `.kf-btn--{size}`. Toutes les valeurs via tokens.

**Lois UX :** Von Restorff (un seul `primary`/`accent` par vue), Fitts (44px minimum md/lg).

**États :** hover, active (translateY 1px), disabled (opacity 0.55, cursor not-allowed), focus-visible.

### 1.2 IconButton.razor

**Emplacement :** `Components/Design/IconButton.razor`

**Paramètres :**
```csharp
[Parameter, EditorRequired] public string Label { get; set; } = "";
// aria-label obligatoire
[Parameter] public string Tone { get; set; } = "default";
// default | primary | success | danger
[Parameter] public string Size { get; set; } = "md";
// md (44px) | sm (36px)
[Parameter] public bool Outline { get; set; }
[Parameter] public bool Disabled { get; set; }
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public EventCallback OnClick { get; set; }
```

**Markup :** `<button type="button" aria-label="@Label" class="kf-icon-btn kf-icon-btn--@Tone ..." disabled="@Disabled" @onclick="OnClick">`.

**CSS :** cible carrée `--target-min` × `--target-min` (44px), `border-radius: var(--radius-sm)`, couleur hover selon `tone`.

**Accessibilité :** `aria-label` est `EditorRequired` — erreur de compilation si manquant.

**Lois UX :** Fitts (44px), accessibilité (label réel, pas tooltip seulement).

### 1.3 Card.razor

**Paramètres :**
```csharp
[Parameter] public bool Interactive { get; set; }
[Parameter] public bool Active { get; set; }
[Parameter] public bool Flush { get; set; }
[Parameter] public string? As { get; set; } // "div" | "button" | "a"
[Parameter] public RenderFragment? ChildContent { get; set; }
[Parameter] public EventCallback OnClick { get; set; }
[Parameter(CaptureUnmatchedValues = true)]
public Dictionary<string, object>? AdditionalAttributes { get; set; }
```

**Classes :** `kf-card`, `kf-card--interactive`, `kf-card--active`, `kf-card--flush`.

**CSS :** `background: var(--surface-card); border: 1px solid var(--border-subtle); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); padding: var(--space-6)`.

**Hover lift (interactive) :** `transform: translateY(-2px); box-shadow: var(--shadow-md)`.

**Active :** `border-color: var(--success); background: var(--success-subtle)`.

**Focus-visible :** `outline: 2px solid var(--focus-ring); outline-offset: 2px`.

**Loi UX :** Fitts (zone cliquable élargie), feedback immédiat.

### 1.4 ProgressRing.razor

**Emplacement :** `Components/Design/ProgressRing.razor`

**Paramètres :**
```csharp
[Parameter] public double Progress { get; set; } = 1.0;
// 0.0 – 1.0 (fraction restante)
[Parameter] public string State { get; set; } = "sprint";
// sprint | shortBreak | longBreak | interrupted | done | idle
[Parameter] public int Size { get; set; } = 220;
[Parameter] public int Stroke { get; set; } = 14;
[Parameter] public string? Label { get; set; }
// Texte centre (ex. "24:59")
[Parameter] public string? Sublabel { get; set; }
// Sous-texte centre (ex. "Sprint en cours")
```

**Markup SVG :** Porter exactement le SVG de `ProgressRing.jsx`.
- Track circle : `stroke="var(--pomo-track)"`.
- Arc circle : `stroke` = propriété CSS dérivée de l'état via classe `.kf-ring--sprint`, etc.
  Utiliser un attribut `style` calculé : `stroke:var(--pomo-@State.ToLowerInvariant())` est
  invalide (trait d'union). **Solution :** mapper l'état en classe CSS :
  `sprint` → `.kf-ring--sprint { stroke: var(--pomo-sprint) }`,
  `shortBreak` → `.kf-ring--short-break { stroke: var(--pomo-short-break) }`, etc.
- `<text>` centre : `font-family="var(--font-mono)"` et `fill="currentColor"`.
- `role="img" aria-label="@Label @Sublabel"`.
- Transition `stroke-dashoffset 0.8s linear, stroke 0.4s ease`.
- `prefers-reduced-motion` : les tokens de motion dans `radius-elevation-motion.css`
  appliquent `--duration-* = 0ms` automatiquement. La transition SVG doit être déclarée
  en CSS isolé (pas inline `style`) pour hériter de ce comportement.
  **Implémentation :** utiliser `.kf-ring__arc { transition: stroke-dashoffset var(--duration-slow) linear, stroke var(--duration-base) ease }`.

**Lois UX :** Goal-Gradient (progression visible), Peak-End (état done bien distinct).

### 1.5 StatCard.razor

**Paramètres :**
```csharp
[Parameter, EditorRequired] public string Icon { get; set; } = "";
[Parameter, EditorRequired] public string Value { get; set; } = "";
[Parameter, EditorRequired] public string Label { get; set; } = "";
[Parameter] public string Tone { get; set; } = "primary";
// primary | success | warning | info | task
[Parameter] public bool Active { get; set; }
[Parameter] public string? As { get; set; } // "button" | "a"
[Parameter] public EventCallback OnClick { get; set; }
[Parameter] public string? Href { get; set; }
```

**CSS :** Porter `.kf-stat` et ses variantes depuis `StatCard.jsx`.
Icon background teinté via `--{tone}-subtle`. Active state : `border-color: var(--success); background: var(--success-subtle)`.

**Lois UX :** Miller/chunking (grouper les métriques), affordance de clic.

### 1.6 Tag.razor

**Paramètres :**
```csharp
[Parameter] public string? ChildContent { get; set; } // texte du tag
[Parameter] public bool Hash { get; set; }            // affiche #
[Parameter] public EventCallback? OnRemove { get; set; } // si set → bouton ×
```

**Couleur hash déterministe :** porter l'algorithme djb2 de `Tag.jsx` en C# :
```csharp
private static readonly string[] Palette = [
    "var(--primary)", "var(--success)", "var(--danger)", "var(--warning)",
    "var(--info)", "var(--text-muted)", "var(--task)", "var(--kf-orange-500)",
    "var(--kf-teal-500)", "var(--kf-pink-500)"
];
private static readonly HashSet<int> DarkText = [3, 4]; // warning, info

private string BackgroundColor()
{
    var n = (ChildContent ?? "").ToLowerInvariant();
    uint hash = 5381;
    foreach (var c in n) hash = ((hash * 33) ^ c);
    var idx = (int)(hash % (uint)Palette.Length);
    return Palette[idx];
}
private string TextColor()
{
    var n = (ChildContent ?? "").ToLowerInvariant();
    uint hash = 5381;
    foreach (var c in n) hash = ((hash * 33) ^ c);
    var idx = (int)(hash % (uint)Palette.Length);
    return DarkText.Contains(idx) ? "#1a1a1a" : "#fff";
}
```

Cela garantit la **parité exacte** avec `TagColors.cs` côté application et `Tag.jsx` côté DS.

Le bouton `×` doit avoir `aria-label="Retirer @ChildContent"` et une cible ≥ 14px
avec padding élargi pour WCAG.

### 1.7 Badge.razor

**Paramètres :**
```csharp
[Parameter] public string? Status { get; set; }
// "Todo" | "InProgress" | "Done" — auto-label + couleur
[Parameter] public string? Variant { get; set; }
// "todo" | "inprogress" | "done" | "info" | "danger" | "solid"
[Parameter] public bool Dot { get; set; }
[Parameter] public RenderFragment? ChildContent { get; set; }
```

**Mapping status → variant + label français :**
```csharp
private (string variant, string label) Resolve() => Status switch
{
    "Todo"       => ("todo",       "En attente"),
    "InProgress" => ("inprogress", "En cours"),
    "Done"       => ("done",       "Terminé"),
    _            => (Variant ?? "todo", "")
};
```

**CSS :** Porter `.kf-badge` et variantes. `border-radius: var(--radius-pill)`.

### 1.8 Alert.razor

**Paramètres :**
```csharp
[Parameter] public string Tone { get; set; } = "info";
// success | danger | warning | info
[Parameter] public RenderFragment? ChildContent { get; set; }
```

**Markup :** `<div role="@Role" class="kf-alert kf-alert--@Tone">@ChildContent</div>`.
Role = `"alert"` si `Tone == "danger"`, `"status"` sinon (WCAG live region).

**CSS :** `background: var(--{tone}-subtle); color: var(--{tone}-text); border-left: 4px solid var(--{tone}); border-radius: var(--radius-sm); padding: var(--space-4) var(--space-5)`.

**Lois UX :** feedback immédiat, hiérarchie visuelle de sévérité.

---

## Phase 2 — AppShell (Layout) ✅ complétée le 2026-06-24

### MainLayout.razor + MainLayout.razor.css

**Objectif :** aligner sur `AppShell.jsx` : sidebar 64px, topbar token-aware, contenu flexible.

**Changements MainLayout.razor :**
- Classe `page` → renommer en `kf-app` pour cohérence DS (ou conserver et synchroniser le CSS).
- Classe `sidebar` → `kf-side`.
- `<main>` → `<div class="kf-main">`.
- Remplacer le `<div class="top-row px-4">` par `<header class="kf-top">`.
  - Retirer le lien "About" (lien de développement Blazor par défaut — hors scope produit).
  - Garder `<CultureSelector />` (composant existant).
  - Ajouter user info (si disponible) : `<div class="kf-top__user">@_userName <Avatar>` — à condition que `_userName` soit accessible depuis le layout (injecter `AuthService`).
- `<article class="content px-4">` → `<main class="kf-content">@Body</main>`.

**Changements MainLayout.razor.css :**
- `.sidebar { background-image: linear-gradient(180deg, rgb(5,39,103), #3a0647) }` → `background: var(--brand-sidebar)`.
- `.top-row { background-color: var(--bs-tertiary-bg); height: 3.5rem }` → `background: var(--surface-card); border-bottom: 1px solid var(--border-subtle); height: var(--topbar-height)`.
- `.sidebar { width: 64px }` → `width: var(--sidebar-width)`.
- `.content` → `.kf-content { padding: var(--space-6) var(--space-8) }`.

### NavMenu.razor + NavMenu.razor.css

**Objectif :** aligner sur les `kf-navicon` de `AppShell.jsx`.

**Changements NavMenu.razor :**
- Icônes actuelles : les emoji Unicode (`&#9745;`, `&#127813;`, etc.) restent valides pour l'instant, mais les icônes SVG du DS (`icons.jsx`) offrent un meilleur rendu.
  **Décision :** porter les SVG Home, CheckSquare, Tomato, Book, Settings, Logout depuis `icons.jsx` — comme Blazor markup `<svg>` inline. Envelopper dans un composant `NavIcon.razor` optionnel.
- Chaque `NavLink` + `button` de logout → wrapper dans un `<button class="kf-navicon" aria-label="@Label">` avec `<span class="kf-navicon__tip">@Label</span>` pour le tooltip CSS.
- `aria-current="page"` sur le lien actif (géré par `NavLinkMatch.Prefix` → Blazor ajoute `.active`, à combiner avec `aria-current`).
- Bouton de logout : `aria-label="Déconnexion"`.

**Changements NavMenu.razor.css :**
- `.navbar-brand-icon` → `.kf-side__brand`.
- `.nav-item ::deep a.nav-icon { color: #d7d7d7; border-radius: 8px; height: 3rem; width: 3rem }` → utiliser `var(--radius-md)`, garder 3rem (= ~48px ≥ `--target-comfy`).
- `.nav-item ::deep a.nav-icon.active { background-color: rgba(255,255,255,0.37) }` → garder (surface invariante sur fond sidebar sombre).
- Transitions → `var(--duration-fast) var(--ease-standard)`.
- Tooltip `::after` → garder le mécanisme CSS ; remplacer `border-radius: 6px` par `var(--radius-sm)`.
- `.nav-item-settings { border-top: 1px solid rgba(255,255,255,0.1) }` → garder (invariant sidebar).
- `height: calc(100vh - 3.5rem)` → `height: calc(100vh - var(--topbar-height))`.

---

## Phase 3 — Login et Landing ✅ complétée le 2026-06-24

### Login.razor + Login.razor.css

**Changements Login.razor :**
- Passer les chaînes en dur via `@Loc` (s'assurer que les ressources `.resx` existent) :
  - `@Loc["Login.Title"]` → "KairuFocus"
  - `@Loc["Login.Subtitle"]` → "Gestion d'activité pour développeurs"
  - `@Loc["Login.Button"]` → "Se connecter avec GitHub"
- La `<div class="login-container">` → utiliser les classes DS `kf-login` / `kf-login__card`.
- `.login-logo` → `.kf-login__logo` avec `background: var(--brand-mark-bg)`.
- Bouton GitHub → utiliser le composant `<Button Variant="accent" As="a" Href="@_githubLoginUrl">`.
  Icône SVG GitHub inline dans l'`Icon` RenderFragment.

**Login.razor.css :**
- Porter les 4 classes `kf-login*` depuis `LoginScreen.jsx`.
- `box-shadow: var(--shadow-sm)` sur `.kf-login__card`.
- `border: 1px solid var(--border-subtle)` (remplace le `rgba(255,255,255,0.1)` de la version actuelle).

### Home.razor

**Changements Home.razor :**
- Conserver `@Loc` pour tous les textes (déjà en place).
- `.landing-hero` → `.kf-hero` ; `.landing-hero-content` → `.kf-hero__in`.
- `.landing-logo` → `.kf-login__logo` ou `.kf-hero__logo` (gradient DS coral/ink).
- `.landing-title` → `.kf-hero__title`.
- `.landing-tagline` → `.kf-hero__tag`.
- `.landing-desc` → `.kf-hero__desc`.
- Bouton GitHub → `<Button Variant="accent" Size="lg" As="a" Href="@_githubLoginUrl">`.
- `.landing-features` → `.kf-features` ; grille Bootstrap → CSS grid DS `kf-features__grid`.
- `.feature-card` → `.kf-feature`.

Tous ces styles vont dans `app.css` (landing est one-off, pas de composant partagé).

---

## Phase 4 — Dashboard ✅ complétée le 2026-06-24

### Dashboard.razor

**Objectif :** coller à `DashboardScreen.jsx` — Zeigarnik resume block, Goal-Gradient dots, stats groupées, quick links réduits.

**Changements :**
- Spinner Blazor actuel → remplacer par un état skeleton (Doherty) : blocs `<div class="kf-skel">` avec animation `var(--surface-sunken)` pulsée. Composant `Skeleton.razor` optionnel si le budget le permet, sinon classes CSS seules.
- Section greeting : `<h1 class="kf-dash__greet">` + `<p class="kf-dash__date">`.
- Date formatée — la hardcoded `CultureInfo("fr-FR")` reste (i18n de date hors scope).
  **Note :** idéalement remplacer par `@Loc["Culture"]` + `CultureInfo(Loc["Culture"])` mais c'est un écart non bloquant.
- **DÉCISION DE CADRAGE (verrouillée — présentation seule) :** aucune touche backend dans cette PR.
  - **Bloc Zeigarnik « À reprendre » :** l'ajouter **uniquement si** la donnée nécessaire (session active et/ou première tâche `InProgress`) est déjà exposée par l'API que le Dashboard consomme aujourd'hui, **sans nouvel endpoint**. Si la donnée n'est pas déjà disponible côté client → **omettre** le bloc et le documenter en écart. Markup : `<Card Interactive>` + `<Button Variant="primary">Reprendre</Button>`.
  - **Bloc dots « Focus aujourd'hui » :** **OMIS** dans cette PR. Il exigerait un endpoint `GetTodaySprintCountAsync()` (couche API/Application) hors du périmètre présentation. À documenter en écart pour une itération future.
- Stats → utiliser `<StatCard>` avec `Tone`, `Active`, `OnClick`.
- Quick links → réduire à 3 (Tâches, Pomodoro, Journal) comme dans le DS (Hick's Law). Retirer le lien "Tickets" (BC désactivé depuis #19). Retirer "Paramètres" (accessible via sidebar).
- CSS dans `app.css` : garder `.kf-dash*` classes.

**i18n :** Les libellés "Tâches en cours", "Aucun sprint actif", "Acces rapides" etc. sont en dur en français. Hors scope de cette PR — documenter comme écart.

---

## Phase 5 — Tasks, TaskDetail, TaskEdit ✅ complétée le 2026-06-24

### Tasks.razor

**Objectif :** coller à `TasksScreen.jsx` — checkbox complétion (Jakob), big targets (Fitts), grouped meta.

**Changements :**
- `<h2>Tâches</h2>` → `<h1 class="kf-tasks__title">`.
- Zone d'ajout : `<form>` wrappée dans `<Card>` avec classe `kf-addcard`.
  - `<input class="form-control">` → `<input class="kf-input" …>` (styles CSS isolés Input).
  - `<textarea class="form-control">` → `<textarea class="kf-input kf-input--textarea">`.
  - Tags : `<span class="badge @TagColorClass(tag)">` → `<Tag Hash OnRemove="...">@tag</Tag>`.
  - Bouton Ajouter → `<Button Variant="primary" Disabled="@string.IsNullOrWhiteSpace(newTitle)">Ajouter</Button>`.
- Filtres :
  - `<input class="form-control">` recherche → CSS isolé Input.
  - `<select class="form-select">` → CSS isolé Select.
- Liste des tâches :
  - `<ul class="list-group">` → `<div class="kf-tasklist">`.
  - `<li class="list-group-item">` → `<div class="kf-taskrow">`.
  - Checkbox complétion : `<input type="checkbox">` (ou `<Checkbox>` composant si créé) à gauche, `aria-label="Compléter @task.Title"`.
  - Titre → bouton `<button class="kf-taskrow__title">@task.Title</button>`.
  - Status badge → `<Badge Status="@task.Status" />`.
  - Tags → `<Tag Hash>@tag</Tag>`.
  - Actions → `<IconButton Label="Modifier @task.Title" Tone="primary" Outline OnClick="...">` + `<IconButton Label="Supprimer @task.Title" Tone="danger" Outline OnClick="...">`.
  - Bouton "Compléter" checkbox (actuel) → géré par la checkbox de gauche.
- Erreur → `<Alert Tone="danger">@errorMessage</Alert>`.
- Empty state → `<div class="kf-empty">` avec icône + texte.

**Cibles tap :** tous les `IconButton` doivent être ≥ 44px (md par défaut).

### TaskDetail.razor

- Bouton Retour → `<Button Variant="ghost" Size="sm">← Retour</Button>`.
- Heading + badge → `<h1 class="kf-te__title"> + <Badge Status="@task.Status" />`.
- Description container → `<div class="kf-md">` (classe DS).
- Actions → `<Button Variant="primary">Modifier</Button>` + `<Button Variant="secondary">Retour</Button>`.

### TaskEdit.razor

- Identique à TaskDetail pour la structure de base.
- Tab Éditer/Prévisualiser : les `<button class="nav-link">` → classes `.kf-tab2` / `.kf-tab2.is-active`.
- `<textarea class="form-control font-monospace">` → `<textarea class="kf-md" style="font-family: var(--font-mono)">`.
- Compteur de caractères → `font-family: var(--font-mono); color: var(--danger)` si dépassement.
- Tags → `<Tag Hash OnRemove="...">`.

---

## Phase 6 — Pomodoro et Sprint Libre ✅ complétée le 2026-06-24

### Pomodoro.razor

**Objectif :** coller à `PomodoroScreen.jsx` — ProgressRing composant Blazor, tabs de type session, big controls (Fitts + Von Restorff), Peak-End.

**Changements :**
- En-tête → `<div class="kf-pomo__head"><h1>Pomodoro</h1> …</div>`.
- Remplacer le menu "···" / `dropdown-menu-simple` par le bouton `<Button Variant="ghost" OnClick="NavigateToSprintLibre">Sprint libre</Button>`.
- Onglets session type → `<div class="kf-tabs" role="tablist">` + `<button class="kf-tab is-active" role="tab">`.
- SVG timer → remplacer par `<ProgressRing Progress="@Progress" State="@RingState" Size="236" Label="@TimeDisplay" Sublabel="@StatusLabel" />`.
  - `Progress` : calculé comme `(double)_remainingSeconds / _totalSeconds` (fraction restante, identique au JS).
  - `RingState` : mapping depuis `_session?.Status` et `_session?.SessionType` :
    ```csharp
    private string RingState => _session?.Status switch
    {
        "Active" => _session.SessionType switch
        {
            "Sprint" => "sprint",
            "ShortBreak" => "shortBreak",
            "LongBreak" => "longBreak",
            _ => "sprint"
        },
        "Completed" => "done",
        "Interrupted" => "interrupted",
        _ => "idle"
    };
    ```
- Boutons contrôle → `<Button Variant="primary" Size="lg">▶ Démarrer</Button>` / `<Button Variant="secondary" Size="lg">⏹ Interrompre</Button>`.
- Résultat fin session → `<Alert Tone="success">@icon @label …</Alert>`.
- Liste tâches liées → `.kf-linkedrow` avec `<Badge Status="@task.Status" />` et `<IconButton>` pour compléter/retirer.
- Erreur → `<Alert Tone="danger">`.

**CSS :** Isoler dans `Pomodoro.razor.css` les classes `.kf-pomo*`, `.kf-tabs*`, `.kf-linkedrow*`.

### SprintLibre.razor

**Changements :**
- Bouton Retour → `<Button Variant="ghost" Size="sm">← Retour</Button>`.
- Chronomètre `<div class="display-1 font-monospace">` → `<div class="kf-sl__clock">` avec `color: running ? "var(--primary)" : "var(--text-muted)"`.
- Config avant démarrage → champs `<input>` et `<select>` CSS isolés.
- Boutons → `<Button Variant="primary" Size="lg">`, `<Button Variant="secondary" Size="lg">`.
- Résultat → `<Alert Tone="success">`.
- Historique → `.kf-sl__row` avec `<Badge>` et `font-family: var(--font-mono)` pour la durée.

---

## Phase 7 — Journal ✅ complétée le 2026-06-24

### Journal.razor

**Changements majeurs :**
- Extraire tout le bloc `<style>` (lignes 128–449) vers `Journal.razor.css`.
- Porter les variables `--nb-*` en tête de `Journal.razor.css` (gardées telles quelles, mais valeurs hex → tokens DS quand mappables).
- Header `.notebook-header { background: linear-gradient(135deg, #1a1a2e, #16213e) }` → `background: var(--brand-hero)`.
- `.nb-nav-btn { width: 32px; height: 32px }` → **problème WCAG** (< 44px).
  Agrandir à `min-width: var(--target-min); min-height: var(--target-min)` avec padding compensatoire.
- `.notebook-day { letter-spacing: 0.18em }` → `letter-spacing: var(--tracking-wider)`.
- `.nb-tasks-header { letter-spacing: 0.08em }` → `letter-spacing: var(--tracking-wide)`.
- `.nb-comment-body { transition: all 0.15s }` → `transition: opacity var(--duration-fast) var(--ease-standard)`.
- Transitions `.nb-add-btn` → tokens motion.
- `.btn-icon:hover { color: #0d6efd }` → `var(--primary)` ; `.btn-icon-danger:hover { color: #dc3545 }` → `var(--danger)`.
- Boutons commentaire `Enregistrer`/`Annuler`/`Ajouter` → `<Button Variant="primary" Size="sm">` / `<Button Variant="ghost" Size="sm">`.
- `<button class="nb-add-btn">` → garder la classe CSS, mais s'assurer que la cible est ≥ 44px (ajouter `min-height: var(--target-min)`).

---

## Phase 8 — Settings ✅ complétée le 2026-06-24

### Settings.razor

**Changements :**
- `<h2>⚙ Paramètres</h2>` → `<h1 class="kf-settings__title">⚙ Paramètres</h1>`.
- Chaque section → `<Card Class="kf-section"><h2 class="kf-section__title">…</h2>…</Card>`.
- `<select class="form-select">` × 2 (theme, ringtone) → CSS isolé Select (classe `kf-select`).
- Bouton "Tester" → `<Button Variant="secondary" Disabled="@(_selectedRingtone == "None")">▶ Tester</Button>`.
- Token MCP : status badge → `<Badge Variant="done">Actif</Badge>` ou `<Badge Variant="danger">Expiré</Badge>`.
  Actions → `<Button Variant="secondary">Régénérer</Button>` + `<Button Variant="danger">Révoquer</Button>`.
- Modal token MCP : `<div class="modal ...">` Bootstrap peut rester. Remplacer `box-shadow` du modal par `var(--shadow-xl)`.
- Pomodoro `<EditForm>` :
  - `<input type="number" class="form-control">` → CSS isolé Input.
  - `<div class="alert alert-info">` → `<Alert Tone="info">`.
  - `<div class="alert alert-success">` → `<Alert Tone="success">`.
  - `<div class="alert alert-danger">` → `<Alert Tone="danger">`.
  - Boutons → `<Button Variant="primary" Disabled="@_saving">` + `<Button Variant="secondary">`.
- `<p>Chargement…</p>` → spinner ou skeleton Doherty.
- Layout container → `<div style="max-width: 600px">` → `<div style="max-width: var(--container-md)">`.

---

## Critères d'acceptance (non-négociables)

**Visuels :**
- [x] Tous les tokens DS actifs en light et dark (pas de hex résiduel dans les fichiers portés). ← Phase 0 : app.css + design-system/
- [x] `data-bs-theme="dark"` fonctionne sans override manuel : les tokens CSS répondent automatiquement. ← Phase 0
- [x] Police JetBrains Mono chargée depuis `wwwroot/css/design-system/fonts/` (aucune requête vers Google). ← Phase 0 : @font-face self-hosted
- [x] Zéro `@import` Google Fonts dans les fichiers CSS actifs. ← Phase 0

**Accessibilité WCAG 2.1 AA :**
- [ ] Contraste texte ≥ 4.5:1 sur fond en light et dark (à valider avec un outil tel que axe ou Lighthouse).
- [x] Focus visible sur tous les éléments interactifs : `outline: 2px solid var(--focus-ring)`. ← Phase 0 : base.css :focus-visible
- [x] `h1:focus { outline: none }` supprimé. ← Phase 0 : app.css
- [ ] Cibles tap ≥ 44px : tous les boutons md/lg, IconButton md. Boutons sm (36px) uniquement en contexte dense (rangées de tâches).
- [x] `aria-label` sur tous les `IconButton` et icônes seules. ← Phase 2 : aria-label sur chaque NavLink + bouton logout
- [x] `aria-current="page"` sur le lien de navigation actif. ← Nettoyage #34 : NavMenu.razor s'abonne à LocationChanged et émet aria-current="page" explicitement (Blazor NavLink n'émet que la classe .active, pas l'attribut aria-current).
- [ ] `ProgressRing` avec `role="img" aria-label="@Label @Sublabel"`.
- [ ] Boutons de navigation journal (précédent/suivant) ≥ 44px.

**Motion :**
- [x] `prefers-reduced-motion: reduce` → toutes les transitions CSS à 0ms via les tokens. ← Phase 0 : radius-elevation-motion.css @media
  Vérifier avec DevTools "Emulate prefers-reduced-motion".

**Bootstrap 5 / aucun framework supplémentaire :**
- [ ] Aucun nouveau package npm ou CDN ajouté.
- [ ] Bootstrap 5 continue de fournir `form-control`, `modal`, `nav-tabs` là où non remplacés.

**i18n :**
- [ ] Toutes les chaînes visibles par l'utilisateur passent par `@Loc` / `.resx`.
- [x] Cas identifiés en dur dans Login.razor → portés en `.resx` dans cette PR. ← Phase 3
- [ ] Autres chaînes en dur (Dashboard date locale, labels session Pomodoro) documentés comme écarts.

**Build et tests :**
- [x] `dotnet build KairuFocus.Web` sans warning ni erreur. ← Phase 0 : 0 warning, 0 erreur / Phase 1 : 0 warning, 0 erreur
- [ ] `dotnet test` — zéro régression (les tests domain/application ne sont pas impactés par la présentation).
- [ ] L'app démarre et navigue sans erreur console JS.

**Offline / PWA :**
- [x] Les polices woff2 sont présentes dans `wwwroot/` → précachées automatiquement par `dotnet publish`. ← Phase 0 : 4 woff2 déjà dans wwwroot/css/design-system/fonts/
- [x] Les tokens CSS sont précachés (même mécanisme). ← Phase 0 : 5 fichiers CSS dans wwwroot/css/design-system/

---

## Risques et garde-fous

**Régression multi-écrans :** Modifier `app.css` et les layouts affecte tous les écrans simultanément.
Tester chaque page manuellement en light et dark avant commit. Utiliser un checklist visuel.

**`color-mix()` dans JournalScreen.jsx :** La règle `.kf-nb__entry.sprint .kf-nb__dot { background: color-mix(in srgb, var(--primary) 14%, var(--nb-body-bg)) }` utilise `color-mix()` (CSS Level 5, supporté dans tous les navigateurs modernes mais pas dans Edge < 109 / Firefox < 113). **Solution de remplacement :** utiliser les variables `--nb-sprint-dot-bg: var(--primary-subtle)` déjà présentes dans Journal.razor actuel.

**SVG et CSS custom properties :** Les SVG inline ne supportent pas `fill: var(--token)` dans tous les contextes. Le composant `ProgressRing.razor` doit utiliser `fill="currentColor"` et gérer la couleur via `color:` CSS sur l'élément SVG parent, ou via des classes CSS scoped.

**Service worker et cache :** Après mise à jour des CSS/polices, les utilisateurs existants peuvent voir l'ancienne version depuis le cache. Le mécanisme `kairufocus-cache-{version}` gère ce cas : le SW se met à jour au prochain reload. Pas d'action supplémentaire requise.

**Parité Maui future :** Les tokens dans `wwwroot/css/design-system/` sont en CSS pur — réutilisables dans Maui (Blazor Hybrid partage le même moteur de rendu). Documenter dans les commentaires du dossier que ces fichiers sont la source de vérité pour les deux cibles.

**Taille bundle WASM :** Cette PR n'ajoute aucun fichier C# supplémentaire significatif. Les composants Blazor partagés sont du HTML+CSS, impact négligeable.

---

## Séquencement des commits suggérés

```
feat(34): Phase 0a — copie tokens DS + @font-face JetBrains Mono self-hosted
feat(34): Phase 0b — câblage index.html + pont Bootstrap complet
feat(34): Phase 0c — refactor app.css token par token (suppr. hex, bugs focus)
feat(34): Phase 1 — composants Blazor partagés (Button, IconButton, Card, ProgressRing, StatCard, Tag, Badge, Alert)
feat(34): Phase 2 — AppShell (MainLayout + NavMenu)
feat(34): Phase 3 — Login + Landing
feat(34): Phase 4 — Dashboard
feat(34): Phase 5 — Tasks + TaskDetail + TaskEdit
feat(34): Phase 6 — Pomodoro + SprintLibre
feat(34): Phase 7 — Journal (extraction CSS inline + tokens)
feat(34): Phase 8 — Settings
feat(34): docs — mise à jour project-state.md + spec.md
```

**Branche :** `feature/34-design-system` (déjà créée).
**PR titre :** `feat(34): adoption design system KairuFocus — Blazor Web`.

---

## Agents impliqués

| Agent | Rôle dans cette itération |
|---|---|
| `blazor-ux` | Implémente toutes les phases 0–8 |
| `test-runner` | `dotnet build` + `dotnet test` après chaque phase |
| `reviewer` | Audit avant merge : WCAG, tokens, i18n, dépendances |
| `eva` | Critique UX optionnelle si des écarts visuels majeurs sont détectés |

---

## Écarts constatés

### Phase 1 (composants partagés) — complétée le 2026-06-24

- **ProgressRing.razor — tag `<text>` SVG** : le compilateur Razor (RZ1023) interprète `<text>` comme un fragment Razor spécial et refuse les attributs. Solution appliquée : les éléments `<text>` SVG sont rendus via `MarkupString` avec `HtmlEncode` sur les valeurs dynamiques (Label, Sublabel). Comportement identique au JSX source.

- **Tag.razor vs TagColors.cs** : `TagColors.cs` retourne des classes Bootstrap (`bg-primary`, `bg-warning text-dark`, etc.) pour un usage legacy dans les pages existantes. `Tag.razor` utilise les tokens CSS DS (`var(--primary)`, `var(--warning)`, etc.) pour la cohérence visuelle. L'algorithme djb2 est rigoureusement identique (même hash → même index de palette). L'écart est intentionnel : `TagColors.cs` est la source de vérité de l'algorithme, `Tag.razor` est la source de vérité du style DS. Un refactor de `TagColors.cs` vers des tokens CSS est possible mais hors scope de cette PR.

- **Alert.razor — `color-mix()`** : le JSX source utilise `color-mix(in srgb, ...)` pour les bordures. Conservé tel quel (Edge ≥109, Firefox ≥113, Chrome ≥111). Si color-mix() n'est pas supporté, `border-color: transparent` (inoffensif). Documenté en commentaire CSS.

- **`--pomo-done` absent des tokens** : le JSX source mappe l'état `done` sur `var(--pomo-done)` mais ce token n'existe pas dans `colors.css`. Solution appliquée : `.kf-ring--done { stroke: var(--success) }` (même valeur sémantique, cohérent avec le plan §1.4).

### Phase 3 (Login + Landing) — complétée le 2026-06-24

- **Login.resx — 8 langues non traduites** : les fichiers `.resx` créés sont `Login.resx` (fallback fr) et `Login.fr.resx`. Les 8 autres langues supportées par Home (en, de, es, pt, it, ru, ja, ko, zh) n'ont pas de traduction pour Login. Le fallback évite tout texte cassé — les utilisateurs dans ces locales verront les chaînes françaises jusqu'à ce que les traductions soient fournies. À documenter pour une itération i18n dédiée.

- **Icône SVG GitHub dans Button** : le composant `Button` accepte un `RenderFragment Icon`. L'icône SVG ne peut pas être insérée directement en Razor markup dans un slot nommé non-ChildContent (le compilateur Razor RZ9996 refuse `<Icon><svg>…</svg></Icon>` quand le composant déclare plusieurs RenderFragment). Solution appliquée : `RenderFragment` statique dans `@code` avec `builder.AddMarkupContent()`. Comportement identique, pas d'impact UX.

- **`btn-github` dans app.css conservé** : la classe `.btn-github` existe encore dans `app.css` pour d'éventuels usages legacy. Elle n'est plus référencée dans Login.razor ni Home.razor après cette phase. Peut être supprimée en nettoyage Phase 8 ou dans une PR dédiée.

### Phase 4 (Dashboard) — complétée le 2026-06-24

- **Bloc "À reprendre" (Zeigarnik) : INCLUS.** La session active (`_currentSession?.Status == "Active"`) et la première tâche `InProgress` (dérivée de `Tasks.GetAllAsync()` déjà chargé) sont disponibles sans nouvel appel API. La session active est prioritaire sur la tâche. Le bloc n'apparaît que si au moins l'une des deux données est présente.

- **Bloc "Focus aujourd'hui" (dots Goal-Gradient) : OMIS.** Il nécessiterait un compteur de sprints du jour. `GetTodaySprintSessionsAsync` existe dans `PomodoroApiClient` mais représenterait un appel API supplémentaire non prévu dans le cadrage "présentation seule". À implémenter dans une itération dédiée avec l'accord du backend.

- **Classes `.dashboard-*` / `.stat-card` / `.quicklink-card` conservées** dans `app.css` : elles ne sont plus référencées par `Dashboard.razor` mais leur suppression risque d'affecter d'autres pages non encore portées (hors scope Phase 4). Purge prévue en Phase 8.

- **Chaînes FR en dur conservées** : greeting ("Bonjour,"), date `fr-FR`, labels StatCard ("Taches en cours", "Sprint actif", etc.) et labels quick links — documentés comme dette i18n (#34 hors scope). Seules les nouvelles chaînes introduites par le bloc Zeigarnik passent par `@Loc` (fichier `Dashboard.resx` créé, fallback FR uniquement).

- **`Dashboard.resx` créé** avec 8 clés (Resume.*, Stats.SectionLabel, QuickLinks.SectionLabel, Loading). Pas de fichiers `.fr.resx` / `.en.resx` créés — même pattern que `Login.resx` (fallback FR seul, i18n multi-langue reportée).

### Phase 5 (Tasks, TaskDetail, TaskEdit) — complétée le 2026-06-24

- **`Tag.ChildContent` de type `string?`** : le compilateur Razor (RZ9986 / CS1660) refuse `<Tag Hash>@variable</Tag>` quand `ChildContent` est `string?` (contenu mixte non supporté en attribut de composant). Solution appliquée : syntaxe attribut explicite `<Tag Hash ChildContent="@variable" />` partout où le contenu est une expression C# dynamique. Les lambdas capturant des variables de boucle `foreach` nécessitent une capture locale explicite (`var current = task;` ou `var capturedTag = tag;`) pour éviter le piège de closure.

- **Chaînes FR en dur conservées** : titres "Tâches", "Modifier la tâche", labels de filtres, messages d'erreur, empty state — même pattern que Dashboard (dette i18n documentée, hors scope). Aucun `@inject IStringLocalizer` ajouté car ces pages n'utilisaient pas `@Loc` avant la phase 5.

- **`TagColors.cs` (Web) encore référencé dans** : `src/KairuFocus.Web/Pages/Journal.razor` (ligne 68, `@TagColors.GetClass(tag)`). Migration vers `<Tag>` prévue en Phase 7 (Journal). Le fichier n'est pas supprimé.

- **CSS isolé `.razor.css`** : `kf-input`, `kf-select`, `kf-md`, `kf-tab2` définis séparément dans `Tasks.razor.css`, `TaskDetail.razor.css` et `TaskEdit.razor.css` (CSS scoping Blazor interdit le partage entre composants). La duplication est intentionnelle et nécessaire.

### Phase 6 (Pomodoro + SprintLibre) — complétée le 2026-06-24

- **Mapping `RingState` réel** : les valeurs C# sont `Status = "Active"/"Completed"/"Interrupted"` et `SessionType = "Sprint"/"ShortBreak"/"LongBreak"`. Mapping appliqué : `Active+Sprint→"sprint"`, `Active+ShortBreak→"shortBreak"`, `Active+LongBreak→"longBreak"`, `Completed→"done"`, `Interrupted→"interrupted"`, `null/autre→"idle"`. Identique au plan §Phase 6.

- **Menu "···" supprimé** : le `<button class="btn-more">` et `dropdown-menu-simple` sont remplacés par un `<Button Variant="ghost">Sprint libre</Button>` (inactif) ou `<Button Variant="secondary">Mode focus</Button>` (actif). L'implémentation du mode focus (overlay plein écran) est omise dans cette PR — elle nécessiterait un état CSS `position:fixed` global potentiellement conflictuel avec le layout AppShell. Documenté comme amélioration future.

- **Libellés FR en dur conservés** : labels session "Sprint en cours", "Pause courte", "Pause longue", "Prêt", "Terminé", "Interrompu", "Tâches du sprint", "Démarrer", "Interrompre", "Terminer", "Sprints du jour", "Sans tâche" — dette i18n hors scope de cette PR, documentée selon le plan §Observations.

- **`kf-input` / `kf-select` définis dans les deux fichiers `.razor.css`** : le CSS scoping Blazor interdit le partage entre composants. La duplication est intentionnelle et nécessaire (même pattern que Phase 5).

- **`Progress` lors de l'état idle/done** : quand la session n'est pas active, `Progress = 1.0` (anneau plein) pour montrer un état initial complet plutôt qu'un anneau vide — cohérent avec la logique `DashOffset` d'origine (qui retournait `0` = pas de décalage = cercle plein).

- **Badge "Interrompu" dans SprintLibre** : la valeur `sprint.Status == "Interrupted"` ne correspond à aucun `Status` mappé dans `Badge.razor` (`Todo`/`InProgress`/`Done`). Rendu via `Variant="inprogress"` avec `ChildContent` explicite "Interrompu" (sémantiquement proche, pas de token dédié).

### Phase 7 (Journal) — complétée le 2026-06-24

- **Variables `--nb-*` "warm paper" conservées en hex** : les teintes spécifiques au cahier (`--nb-body-bg: #fafaf7`, `--nb-comment-bg: #f0ede6`, etc.) n'ont pas d'équivalent exact dans les tokens DS. Les tokens disponibles (`--surface-card`, `--surface-sunken`) auraient une apparence neutre-grise cassant l'esthétique "papier jauni" voulue par le DS. Seuls les tokens DS existants (`--primary-subtle`, `--success-subtle`, `--task-subtle`) sont utilisés pour les fonds des dots colorés. Décision intentionnelle, documentée.

- **`--nb-warn-dot-bg` sans token DS** : `var(--kf-orange-500)` n'a pas de variante `-subtle` dans `colors.css`. Valeurs hex conservées : `#fff3e8` (light) et `#3e1e0d` (dark). Si un token `--warning-subtle` ou `--kf-orange-subtle` est ajouté au DS, les remplacer directement.

- **`TagColors.cs` (Web) — plus aucune référence active** : après migration de `Journal.razor` vers `<Tag Hash>`, `TagColors.GetClass()` n'est plus appelé nulle part dans les pages Web. Le fichier `src/KairuFocus.Web/Helpers/TagColors.cs` subsiste mais est du code mort. Il n'a pas été supprimé dans cette PR (risque de régression sur d'éventuels usages hors-pages non détectés). Suppression propre recommandée dans une PR de nettoyage dédiée.

- **`IconButton Size="sm"` dans les actions commentaire** : les icônes Modifier/Supprimer opèrent dans un contexte commentaire très dense (ligne de 0.82rem de hauteur). `Size="sm"` (36px) est la taille tolérée par WCAG en contexte dense-layout. La cible reste visuellement petite — acceptable car `aria-label` est présent et la zone `nb-comment-body:focus-within` révèle les boutons au focus clavier.

- **Bouton `nb-add-btn` conservé natif** : le bouton "+ note" garde sa classe CSS propre plutôt que d'utiliser `<Button Variant="ghost" Size="...">` car son rendu "tiret pointillé inline" ne correspond à aucun variant DS existant. `min-height: var(--target-min)` (44px) appliqué directement dans le CSS isolé — correction WCAG effective.

- **`@using KairuFocus.Web.Helpers` conservé** : la directive `@using` pointe vers le namespace `Helpers` qui contient `TagColors.cs`. Elle ne cause aucune erreur de compilation même si `TagColors` n'est plus appelé — le using est simplement inutilisé mais inoffensif. Il peut être supprimé lors du nettoyage de `TagColors.cs`.

### Phase 8 (Settings) — complétée le 2026-06-24

- **`EditForm` conservé** : l'`EditForm` Blazor est maintenu avec `DataAnnotationsValidator` et un `<Button Type="submit">` natif (pas via le composant DS `Button`). La validation par annotation reste fonctionnelle et le comportement `OnValidSubmit` est inchangé. Le composant DS `Button` force `type="button"` mais n'est pas utilisé pour le bouton de soumission de ce formulaire — un `<button type="submit" class="kf-btn kf-btn--primary">` natif est utilisé à la place pour garantir la compatibilité EditForm.

- **Section "Objectif de focus" absente** : le JSX `SettingsScreen.jsx` inclut une section "🎯 Objectif de focus" (sprints visés par jour) absente dans le `Settings.razor` d'origine. Cette section n'a pas d'équivalent dans l'API (`SettingsApiClient` ne possède pas de propriété `SprintGoal`). Omise conformément à la règle "aucun nouvel endpoint, présentation seule".

- **Chaînes FR en dur conservées** : labels "Sprint (min)", "Pause courte", "Pause longue", messages de validation, texte du modal — `Settings.razor` n'utilisait pas `@Loc` avant Phase 8. Même dette i18n que les phases précédentes.

### Phase 0 (fondation CSS) — complétée le 2026-06-24

- `PomodoroApi.GetTodaySprintCountAsync()` — vérifier si l'endpoint existe pour le bloc dots du Dashboard.
- Chaînes en dur Pomodoro (labels session, status) — hors scope i18n de cette PR, à documenter.
- Date Dashboard formatée `fr-FR` hardcodée — noter comme dette.
- Dashboard (CADRAGE VERROUILLÉ — présentation seule) : bloc dots "Focus aujourd'hui" **omis** dans cette PR (nécessiterait un endpoint backend). Bloc "À reprendre" ajouté **seulement** si l'API actuelle expose déjà la donnée sans nouvel endpoint, sinon omis. Aucune modification API/Application/Domain.
- `app.css` ligne `code { color: ... }` : l'original utilisait `#c02d76` (rose vif). Remplacé par `var(--task)` (purple-500 light / a78bdb dark) — couleur la plus proche du DS pour les inline code. Si le rendu visuel est jugé trop éloigné, un token `--code-color` dédié peut être ajouté en Phase 1+.
- `app.css` `.loading-progress circle` stroke initial : l'original utilisait `#e0e0e0`. Remplacé par `var(--border-default)` (gray-300 light / slate-700 dark), seule valeur thème-aware équivalente disponible dans les tokens. Légèrement plus sombre en light (dee2e6 vs e0e0e0) — sans impact visible sur l'UX de chargement.
