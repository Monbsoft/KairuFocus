**Input** — single-line text field wired to the token system, with optional `label`, `hint` and `error`.

```jsx
<Input label="Titre de la tâche" placeholder="Titre de la tâche…" value={title} onChange={e=>setTitle(e.target.value)} />
<Input label="Recherche" type="search" hint="Filtre par titre" />
<Input label="Durée" error="Entre 1 et 120 minutes" />
```

Error sets `aria-invalid` and reddens the border + focus ring.
