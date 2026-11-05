**ActiveTodayStrip** — "Actifs aujourd'hui", the last line of the live strip on the Community page (iteration 45), under a light rule, on the dark brand hero. Up to five members who finished a sprint today (UTC day), most recent first, each avatar + name linking to the profile, then "et N autres".

- The viewer is listed like anyone else when active (`isMe`): "(toi)" after the name, link to their own profile.
- Empty: "Personne n'a encore terminé de sprint aujourd'hui." Error: inline alert with "Réessayer", the rest of the strip stays.
- No live region: it refreshes with the presence loop (60 s, paused when the tab is hidden), quietly.

```jsx
<ActiveTodayStrip total={7} meHref="/profil" hrefFor={(m) => `/membres/${m.name}`}
  members={[{ name: "lea_dev" }, { name: "nora", isMe: true }, { name: "tomtom" }, { name: "ada_l" }, { name: "linus" }]} />

<ActiveTodayStrip members={[]} />
```

Laws of UX: Social proof through Proximity (same block as the open sprints), Chunking (five names, then a count), Fitts (44px links), Jakob (same avatars as the cards). Data in cards and mocks must stay fictitious.
