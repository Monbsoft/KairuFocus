**Button** — the primary clickable action across KairuFocus; `primary`/`accent` carry CTA weight (Von Restorff), so keep one per view.

```jsx
<Button variant="primary" size="lg" onClick={start}>Démarrer un sprint</Button>
<Button variant="secondary" icon={<PencilIcon/>}>Modifier</Button>
<Button variant="ghost" size="sm">Annuler</Button>
<Button variant="danger">Supprimer</Button>
<Button as="a" href="/api/auth/github" variant="accent">Se connecter</Button>
```

- **variant**: `primary` (blue) · `secondary` (outline) · `ghost` · `danger` · `accent` (coral)
- **size**: `sm` (36px, dense rows only) · `md` (44px, default) · `lg` (48px, hero CTAs)
- All sizes meet the 44px tap target except `sm`. Use `as="a"` for navigation/login links.
