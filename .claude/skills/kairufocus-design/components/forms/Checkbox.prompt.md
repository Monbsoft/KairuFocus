**Checkbox** — the conventional way to complete a task (Jakob's Law). Visible box is 22px; clickable area is a full 44px (Fitts). Checking strikes through the label.

```jsx
<Checkbox label="Écrire les tests d'intégration" checked={done} onChange={e=>complete(e.target.checked)} />
```
