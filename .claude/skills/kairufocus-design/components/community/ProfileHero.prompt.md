**ProfileHero** — profile banner on the brand hero gradient (same in light and dark). Identity on the left, the sprint status as `children` on the right. Pass `src` and `githubLogin` only when the name comes from GitHub: a chosen name shows its initial and no GitHub link.

**SprintStatus** — the status panel that sits in the hero. `state="open"` shows "En sprint · encore 18:24", who is in it and one primary button; `state="closed"` shows "Pas de sprint ouvert pour le moment" and the count of sprints done together. Never write "pas en sprint": it would be false for a closed sprint, a free sprint or a break. Omit `onJoin` on your own profile.

**AvatarStack** — overlapping avatars with a "+N" bubble. Decorative (`aria-hidden`): always write the names next to it. Set `ringColor` to the surface behind the stack.

**OpenSprintRow** — one row of "En sprint maintenant", one per open group (host, time left, participants, button). Put the rows in a `Card`. No live region: rows come and go quietly.

```jsx
<ProfileHero name="lea_dev" src={ghAvatar} githubLogin="lea_dev" since="Dans la communauté depuis octobre 2026">
  <SprintStatus state="open" timeLeft="18:24"
    avatars={<AvatarStack members={[{ name: "tomtom" }, { name: "ada" }, { name: "linus" }]} max={2} />}
    withText="avec tomtom et 2 autres" onJoin={join} />
</ProfileHero>

<ProfileHero name="tomtom" since="Dans la communauté depuis septembre 2026">
  <SprintStatus state="closed" togetherText="38 sprints faits ensemble" />
</ProfileHero>

<Card>
  <OpenSprintRow name="lea_dev" timeLeft="18:24" participantsText="3 participants" onJoin={join} />
  <OpenSprintRow name="tomtom" timeLeft="09:02" participantsText="1 participant" mine />
</Card>
```

Laws of UX: Jakob (same dark block as the Community page), Von Restorff (one primary button per panel), Doherty (the click reacts at once), Fitts (44px targets). Data in cards and mocks must stay fictitious.
