**Select** — native `<select>` restyled with a custom chevron and 44px height. Pass `options` data or `<option>` children.

```jsx
<Select label="Statut" value={filter} onChange={e=>setFilter(e.target.value)}
  options={[{value:"OpenOnly",label:"Ouvertes"},{value:"All",label:"Toutes"},{value:"Done",label:"Terminées"}]} />
```
