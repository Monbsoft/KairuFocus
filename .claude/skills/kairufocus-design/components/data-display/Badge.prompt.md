**Badge** — task status pill. Pass `status` for the app states (auto French label + color) or `variant`+children for custom.

```jsx
<Badge status="InProgress" /> <Badge status="Done" /> <Badge status="Todo" dot />
```

**Tag** — hashed-color category chip (same tag → same color, matching `TagColors.cs`). `onRemove` adds a removable ×.

```jsx
<Tag hash>backend</Tag>
<Tag hash onRemove={()=>drop("wip")}>wip</Tag>
```

**Card** — base surface; add `interactive` for clickable hover-lift, `active` for selected.

```jsx
<Card interactive onClick={open}>…</Card>
```

**StatCard** — dashboard metric tile (icon · value · label), clickable; `active` for live state.

```jsx
<StatCard icon="☑️" value={6} label="Tâches en cours" href="/tasks" />
<StatCard icon="🍅" value="En cours" label="Sprint" tone="success" active />
```

**Avatar** — GitHub photo or initials on brand blue. `size`: sm/md/lg or px.
