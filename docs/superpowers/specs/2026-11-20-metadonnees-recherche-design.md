# Design — Métadonnées de recherche et de partage

**Date :** 2026-11-20
**Branche :** `feature/47-metadonnees-recherche` (partie de `main`)
**Taille :** petit changement — choix par défaut validés par le fondateur le 2026-11-20.

## Intention
Que les moteurs de recherche indexent la page d'accueil publique et que les liens partagés
(Slack, Discord, LinkedIn, X) affichent un aperçu propre. L'application est un Blazor
WebAssembly : une partie des robots n'exécute pas le JS, donc l'essentiel est **statique** dans
`wwwroot/index.html`.

## Constat
- L'API sert le même `index.html` pour toute route (`MapFallbackToFile`) : les balises statiques
  valent pour toutes les adresses, pages privées comprises.
- Pages publiques : `/` et `/login`. Toutes les autres exigent une connexion.
- `kairufocus.com` et `www.kairufocus.com` répondent tous deux 200, sans redirection.
- `robots.txt` et `sitemap.xml` répondent 404 ; aucun visuel de partage n'existe.

## Décisions
- **Langue :** anglais (`lang="en"` déjà posé ; itération « Première visite » : anglais par défaut).
- **Titre :** `KairuFocus — Your developer workspace` (texte anglais de la page d'accueil).
- **Description :** version courte (< 160 caractères) du texte anglais de la page d'accueil.
- **`canonical` / `og:url` :** `https://kairufocus.com/` (domaine nu).
- **`meta robots` :** `index, follow` en statique ; `/login` ajoute `noindex` par `<HeadContent>`
  (pris en compte seulement par les robots qui exécutent le JS).
- **Open Graph + Twitter Card :** `summary_large_image`, image `og-image.png` 1200×630 aux
  couleurs du hero (dégradé `#1a1a2e → #16213e → #0f3460`, tuile « K » corail→marine, accroche).
- **`robots.txt` :** autorise `/`, bloque `/api/`, `/mcp` et les routes privées ; pointe le sitemap.
- **`sitemap.xml` :** une seule URL, `https://kairufocus.com/`.

## Approches écartées
- **Pré-rendu côté serveur** des pages publiques : écarté par la décision « sans pré-rendu ».
- **Balises seulement via `<HeadContent>`** : invisibles pour les robots qui n'exécutent pas le JS.

## Hors périmètre
- Redirection 301 de `www` vers le domaine nu (infra Azure).
- Passage de la culture par défaut à l'anglais et description française de `manifest.json`.
- Données structurées (JSON-LD).

## Critères de succès
- Les balises sont présentes dans le HTML brut servi (sans exécuter le JS).
- `/robots.txt`, `/sitemap.xml` et `/og-image.png` répondent 200 avec le bon type.
- `dotnet build` et `dotnet test` verts.
- Aperçu de partage contrôlé dans le navigateur à partir des balises servies.
