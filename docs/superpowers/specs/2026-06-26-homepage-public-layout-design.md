# Design — Homepage : layout public dédié

**Date :** 2026-06-26
**Branche :** `feature/34-dashboard-focus`
**Périmètre choisi :** Structurel + polish léger

---

## Problème

Deux symptômes signalés sur la homepage (`/`), avec une **cause racine unique** :

1. **Le menu d'app s'affiche pour un visiteur non connecté.**
   `App.razor` applique `MainLayout` par défaut à toutes les pages. `MainLayout` rend
   toujours la sidebar (`NavMenu` : Dashboard, Tâches, Pomodoro, Journal, Réglages,
   Déconnexion) et la `top-row` (sélecteur de langue + lien « About »), quel que soit
   l'état d'authentification. La landing `[AllowAnonymous]` hérite donc du chrome de
   l'app — des liens qui ne mènent qu'à des redirections vers login.

2. **Le fond hero n'occupe pas toute la largeur.**
   La landing est rendue dans `<article class="content px-4">`, à droite de la sidebar
   64px, avec un padding `2rem`/`1.5rem !important` injecté sur desktop
   (`MainLayout.razor.css:73-76`). Le dégradé `.landing-hero` est donc encadré au lieu
   d'occuper tout le viewport.

La page `/login` présente **le même défaut** (elle hérite aussi de `MainLayout`).

> Note chromatique : les couleurs hero codées en dur (`#1a1a2e → #16213e → #0f3460`)
> correspondent **exactement** au token `--brand-hero` du design-system. Le problème est
> structurel, pas chromatique.

---

## Décision d'architecture

**Option retenue : layout public dédié (Option A).**

Créer un `PublicLayout` minimal (rend uniquement `@Body`, sans sidebar ni top-row) et
l'appliquer aux pages publiques via `@layout`. Séparation nette public / app, idiomatique
Blazor, aucune logique conditionnelle dans le layout.

Alternative rejetée (Option B) : masquer conditionnellement la sidebar/top-row dans
`MainLayout` selon l'état d'auth ou la route → logique fragile, couple public et app.

---

## Changements

### Fichiers créés
- `src/KairuFocus.Web/Layout/PublicLayout.razor`
  Shell public : `@inherits LayoutComponentBase`, rend `@Body`. Applique `data-bs-theme`
  selon `prefers-color-scheme` (un visiteur n'a pas de préférence enregistrée).
- `src/KairuFocus.Web/Layout/PublicLayout.razor.css`
  Reset plein écran (le conteneur public n'impose ni largeur ni padding).

### Fichiers modifiés
- `src/KairuFocus.Web/Pages/Home.razor` → ajout `@layout PublicLayout`.
- `src/KairuFocus.Web/Pages/Login.razor` → ajout `@layout PublicLayout`.
- `src/KairuFocus.Web/wwwroot/css/app.css` (section landing) :
  - Bloc de tokens scopé `.landing` (light + dark), miroir du pattern `.kf-dash` déjà
    en place. Les valeurs ne changent pas ; on remplace les littéraux par des variables.
  - Accessibilité : `:focus-visible` visible sur le bouton GitHub et les `.feature-card` ;
    vérification du contraste de `.feature-desc` / `.landing-desc` (≥ 4.5:1).
  - Light/dark : la section `.landing-features` bascule déjà sur `[data-bs-theme="dark"]` ;
    s'assurer que le `PublicLayout` pose bien le `data-bs-theme`.

---

## Hors-scope (décidé)

- **Sélecteur de langue sur la landing** : non remis pour l'instant. La culture suit le
  navigateur. À rediscuter si un switch visible avant login est souhaité.
- **Lien « About »** (learn.microsoft.com) : retiré pour les visiteurs (reliquat du
  template Blazor par défaut).
- **Aucune refonte visuelle** du hero ou des feature-cards (mise en page inchangée).

---

## Critères de succès

- [ ] Visiteur non connecté sur `/` : **aucune** sidebar ni top-row d'app visible.
- [ ] Le fond hero occupe **toute la largeur** du viewport (desktop + mobile).
- [ ] `/login` : même comportement (pas de chrome d'app).
- [ ] Un utilisateur connecté arrivant sur `/` est toujours redirigé vers `/dashboard`
      (comportement existant `OnAfterRenderAsync` préservé).
- [ ] Les pages internes (Dashboard, Tâches, …) conservent `MainLayout` intact.
- [ ] Landing correcte en thème clair **et** sombre.
- [ ] `dotnet build` vert, aucune régression.

---

## Vérification (étape « Tester »)

1. Avant code : reproduire visuellement les 2 défauts dans le preview (menu visible +
   fond encadré).
2. Après code : confirmer fond plein écran, absence de menu, light/dark, `/login`.
