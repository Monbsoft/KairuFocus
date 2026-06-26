# Homepage — Layout public dédié — Plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sortir les pages publiques (Home, Login) du shell applicatif via un `PublicLayout` dédié, pour que le fond hero occupe tout le viewport et que le menu d'app disparaisse pour les visiteurs non connectés.

**Architecture:** Nouveau layout Blazor minimal (`PublicLayout`) qui rend uniquement `@Body` (ni sidebar `NavMenu`, ni `top-row`) et pose `data-bs-theme` selon `prefers-color-scheme`. `Home.razor` et `Login.razor` déclarent `@layout PublicLayout`. Polish CSS léger sur la section landing de `app.css` (tokens scopés `.landing` + focus visible), sans refonte visuelle.

**Tech Stack:** Blazor WebAssembly (.NET 10), Bootstrap 5 (CSS only), CSS custom dans `wwwroot/css/app.css`, JS interop `eval` (pattern existant dans `MainLayout`).

**Méthode de vérification :** changement layout/CSS — pas de logique C# unitaire. Chaque jalon se valide par `dotnet build` vert **et** vérification visuelle dans le preview (menu absent, fond plein écran, light/dark, `/login`). Aucun test xUnit n'est pertinent ici.

---

## Structure des fichiers

**Créés**
- `src/KairuFocus.Web/Layout/PublicLayout.razor` — layout public : `@Body` + thème système. Une seule responsabilité : présenter une page publique sans chrome d'app.

**Modifiés**
- `src/KairuFocus.Web/Pages/Home.razor` — ajout directive `@layout PublicLayout` (1 ligne).
- `src/KairuFocus.Web/Pages/Login.razor` — ajout directive `@layout PublicLayout` (1 ligne).
- `src/KairuFocus.Web/wwwroot/css/app.css` — section landing : bloc tokens scopé `.landing` (light + dark) + focus visible sur le bouton GitHub. Valeurs visuelles inchangées.

> **Décision (YAGNI) :** pas de `PublicLayout.razor.css`. Les règles `.page`/`.sidebar`/`article` proviennent du CSS *scopé* de `MainLayout` et ne s'appliquent donc pas à `PublicLayout`. `.landing` et `.login-container` ont déjà `min-height: 100vh`, et `bootstrap-reboot` met `body { margin: 0 }` → le plein écran est acquis sans CSS de layout supplémentaire. Si la vérification visuelle (Task 2) révèle une contrainte résiduelle, ajouter le fichier à ce moment-là.

---

## Task 1 : Créer le `PublicLayout`

**Files:**
- Create: `src/KairuFocus.Web/Layout/PublicLayout.razor`

- [ ] **Step 1 : Écrire le composant**

Créer `src/KairuFocus.Web/Layout/PublicLayout.razor` avec exactement ce contenu :

```razor
@inherits LayoutComponentBase
@inject IJSRuntime JSRuntime

@Body

@code {
    // Pages publiques : aucun chrome d'app. On applique le thème du système
    // (le visiteur n'a pas de préférence enregistrée). Même pattern eval que MainLayout.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        try
        {
            var isDark = await JSRuntime.InvokeAsync<bool>(
                "eval", "window.matchMedia('(prefers-color-scheme: dark)').matches");
            await JSRuntime.InvokeVoidAsync(
                "eval",
                isDark
                    ? "document.documentElement.setAttribute('data-bs-theme', 'dark')"
                    : "document.documentElement.setAttribute('data-bs-theme', 'light')");
        }
        catch
        {
            // Si l'interop échoue, la landing reste en thème clair par défaut.
        }
    }
}
```

- [ ] **Step 2 : Vérifier le build**

Run: `dotnet build src/KairuFocus.Web/KairuFocus.Web.csproj`
Expected: `Build succeeded`, 0 erreur. (Le composant n'est encore référencé par aucune page — c'est normal.)

- [ ] **Step 3 : Commit**

```bash
git add src/KairuFocus.Web/Layout/PublicLayout.razor
git commit -m "feat(34): layout public dédié (sans chrome d'app)"
```

---

## Task 2 : Brancher Home + Login sur le `PublicLayout`

**Files:**
- Modify: `src/KairuFocus.Web/Pages/Home.razor:1`
- Modify: `src/KairuFocus.Web/Pages/Login.razor:1`

- [ ] **Step 1 : Ajouter `@layout` sur Home**

Dans `src/KairuFocus.Web/Pages/Home.razor`, insérer la directive `@layout` juste après la directive `@page`. Les premières lignes doivent devenir :

```razor
@page "/"
@layout PublicLayout
@attribute [AllowAnonymous]
@inject AuthService AuthService
```

- [ ] **Step 2 : Ajouter `@layout` sur Login**

Dans `src/KairuFocus.Web/Pages/Login.razor`, insérer la directive `@layout` juste après la directive `@page`. Les premières lignes doivent devenir :

```razor
@page "/login"
@layout PublicLayout
@using KairuFocus.Web.Auth
@inject NavigationManager Nav
```

> `@using KairuFocus.Web.Layout` est déjà présent dans `_Imports.razor` — aucun import à ajouter.

- [ ] **Step 3 : Build**

Run: `dotnet build src/KairuFocus.Web/KairuFocus.Web.csproj`
Expected: `Build succeeded`, 0 erreur.

- [ ] **Step 4 : Vérification visuelle dans le preview**

Démarrer le preview (`preview_start` si besoin), puis sur `/` (déconnecté) :
- `preview_snapshot` + `preview_screenshot` : **aucune** sidebar (icônes Dashboard/Tâches/…), **aucune** top-row (sélecteur de langue / « About »).
- Le dégradé hero `.landing-hero` s'étend **sur toute la largeur** du viewport (pas d'encoche à gauche, pas de padding latéral).
- `preview_console_logs` : pas d'erreur JS.
- Naviguer sur `/login` : même résultat (pas de chrome d'app, carte centrée plein écran).

Si une contrainte de largeur résiduelle apparaît : inspecter (`preview_inspect`) l'élément `.landing` / `body`, identifier la règle fautive, et créer `PublicLayout.razor.css` avec un reset ciblé (ex. `::deep .landing { width: 100%; }`).

- [ ] **Step 5 : Commit**

```bash
git add src/KairuFocus.Web/Pages/Home.razor src/KairuFocus.Web/Pages/Login.razor
git commit -m "feat(34): Home + Login utilisent le layout public (fond plein écran, sans menu)"
```

---

## Task 3 : Polish CSS de la landing (tokens scopés + focus visible)

**Files:**
- Modify: `src/KairuFocus.Web/wwwroot/css/app.css` (section `/* ── Landing page ── */`, ~lignes 163-271)

- [ ] **Step 1 : Ajouter le bloc de tokens scopé `.landing`**

Dans `app.css`, remplacer la règle `.landing { … }` existante (~ligne 164) par le bloc suivant (tokens light + override dark, miroir du pattern `.kf-dash`) :

```css
/* ── Landing page ──────────────────────────────────────────────────────── */
.landing {
    /* Tokens scopés (light) — valeurs identiques à l'existant, factorisées. */
    --landing-hero-bg: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
    --landing-features-bg: #f8f9fa;
    --landing-card-bg: #ffffff;
    --landing-card-shadow: 0 2px 12px rgba(0, 0, 0, 0.06);
    --landing-card-shadow-hover: 0 8px 24px rgba(0, 0, 0, 0.12);
    --landing-desc-color: #6c757d;
    --landing-focus-ring: #2b7dff;

    min-height: 100vh;
    display: flex;
    flex-direction: column;
}

[data-bs-theme="dark"] .landing {
    --landing-features-bg: #1a1a2e;
    --landing-card-bg: #16213e;
    --landing-card-shadow: 0 2px 12px rgba(0, 0, 0, 0.30);
    --landing-desc-color: #adb5bd;
}
```

- [ ] **Step 2 : Câbler les règles existantes sur les tokens**

Toujours dans la section landing de `app.css`, modifier ces règles pour consommer les tokens (remplacer les littéraux). Cible chaque règle :

`.landing-hero` — remplacer la ligne `background: linear-gradient(...)` par :
```css
    background: var(--landing-hero-bg);
```

`.landing-features` — remplacer `background: #f8f9fa;` par :
```css
    background: var(--landing-features-bg);
```

Supprimer la règle devenue inutile :
```css
[data-bs-theme="dark"] .landing-features {
    background: #1a1a2e;
}
```
(elle est désormais portée par le token `--landing-features-bg` dans le bloc dark `.landing`.)

`.feature-card` — remplacer `background: #fff;` par `background: var(--landing-card-bg);` et `box-shadow: 0 2px 12px rgba(0,0,0,0.06);` par `box-shadow: var(--landing-card-shadow);`.

`.feature-card:hover` — remplacer `box-shadow: 0 8px 24px rgba(0,0,0,0.12);` par :
```css
    box-shadow: var(--landing-card-shadow-hover);
```

Supprimer la règle devenue inutile :
```css
[data-bs-theme="dark"] .feature-card {
    background: #16213e;
    box-shadow: 0 2px 12px rgba(0,0,0,0.3);
}
```

`.feature-desc` — remplacer `color: #6c757d;` par `color: var(--landing-desc-color);`.

Supprimer la règle devenue inutile :
```css
[data-bs-theme="dark"] .feature-desc {
    color: #adb5bd;
}
```

- [ ] **Step 3 : Ajouter le focus visible (WCAG AA) sur le bouton GitHub**

Ajouter à la fin de la section landing de `app.css` :

```css
.landing .btn-github:focus-visible {
    outline: 2px solid var(--landing-focus-ring);
    outline-offset: 2px;
}
```

- [ ] **Step 4 : Build**

Run: `dotnet build src/KairuFocus.Web/KairuFocus.Web.csproj`
Expected: `Build succeeded`, 0 erreur.

- [ ] **Step 5 : Vérification visuelle light + dark**

Dans le preview, sur `/` (déconnecté) :
- **Light** : hero sombre plein écran, section features gris clair (`#f8f9fa`), cartes blanches. Identique à avant (aucune régression visuelle).
- **Dark** : `preview_resize` / forcer le thème système sombre (ou `preview_eval` : `document.documentElement.setAttribute('data-bs-theme','dark')`). Section features → `#1a1a2e`, cartes → `#16213e`, texte `.feature-desc` lisible (`#adb5bd`).
- **Focus** : `preview_eval` `document.querySelector('.btn-github').focus()` puis `preview_screenshot` — anneau de focus bleu visible (2px) autour du bouton GitHub.
- `preview_inspect` sur `.feature-desc` (light) : confirmer `color` calculé `rgb(108,117,125)` sur fond blanc (contraste ≈ 4.6:1, AA OK).

- [ ] **Step 6 : Commit**

```bash
git add src/KairuFocus.Web/wwwroot/css/app.css
git commit -m "feat(34): landing — tokens scopés .landing + focus visible (a11y)"
```

---

## Task 4 : Non-régression du shell applicatif

**Files:** (aucune modif — vérification seule)

- [ ] **Step 1 : Vérifier les pages internes**

Dans le preview, se connecter (ou simuler un état authentifié), puis ouvrir `/dashboard` :
- La sidebar `NavMenu` et la `top-row` sont **toujours présentes** (MainLayout intact).
- `preview_screenshot` du dashboard : mise en page inchangée.

- [ ] **Step 2 : Vérifier la redirection des authentifiés**

Sur `/` en étant authentifié : redirection vers `/dashboard` toujours fonctionnelle (`Home.OnAfterRenderAsync` inchangé). Le `PublicLayout` n'apparaît qu'un court instant avant la redirection — acceptable.

- [ ] **Step 3 : Build + tests globaux (garde-fou)**

Run: `dotnet build` (solution) puis `dotnet test`
Expected: `Build succeeded` ; **279 tests** passent (aucun test n'est censé être impacté par un changement purement présentation).

---

## Critères de succès (rappel du spec)

- [ ] Visiteur non connecté sur `/` : aucune sidebar ni top-row d'app.
- [ ] Fond hero plein écran (desktop + mobile).
- [ ] `/login` : même comportement.
- [ ] Authentifié sur `/` → redirigé vers `/dashboard` (préservé).
- [ ] Pages internes : `MainLayout` intact.
- [ ] Landing correcte light **et** dark.
- [ ] `dotnet build` vert, 279 tests verts.
