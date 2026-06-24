**Textarea** — multi-line field. Pass `maxLength` to show a live `len/maxLength` counter that turns red past the limit (matches the task-description limit).

```jsx
<Textarea label="Description" rows={3} maxLength={500} value={desc} onChange={e=>setDesc(e.target.value)} placeholder="Description (optionnelle)…" />
```
