**IconButton** — icon-only action with a mandatory `label` (real aria-label, not just a CSS tooltip) and a 44px tap target. Use for task row actions, journal note edit/delete, nav.

```jsx
<IconButton label="Modifier la tâche" tone="primary"><PencilIcon/></IconButton>
<IconButton label="Compléter la tâche" tone="success"><CheckIcon/></IconButton>
<IconButton label="Supprimer la tâche" tone="danger" outline><CrossIcon/></IconButton>
```

- **tone**: `default` · `primary` · `success` · `danger` (hover-tinted)
- **size**: `md` (44px, default) · `sm` (36px). Always supply `label` for accessibility.
