**OpenSprintCard** — one card of the live strip "En sprint maintenant", the first block of the Community page (iteration 45). It replaces `OpenSprintRow` there. Always on the dark brand hero (`--brand-hero`): put the cards in a `<ul>` grid of `repeat(auto-fill, minmax(min(250px, 100%), 1fr))` columns, show 6 at most, then a "Voir les N autres sprints" link button with `aria-expanded`.

- Countdown in big (`--text-xl`), the one figure that changes the decision to join.
- One action at most: "Rejoindre ce sprint" (full width) for a member; nothing for a visitor outside the community (`canJoin={false}`, one notice "Activer la communauté" above the cards); "Ton sprint" + "Aller à ma session" on my own card (`mine`, dashed border).
- One participant: "Personne ne l'a encore rejoint" instead of "1 participant".
- No live region: cards come and go quietly; the countdown is never announced.

```jsx
<ul className="grid">
  <OpenSprintCard name="lea_dev" timeLeft="18:24" participantsText="3 participants" onJoin={join} />
  <OpenSprintCard name="tomtom" timeLeft="09:02" mine mineHref="/pomodoro" />
  <OpenSprintCard name="grace" timeLeft="21:40" canJoin={false} />
</ul>
```

Laws of UX: Serial Position (first block under the title), Von Restorff (one filled button per card), Fitts (full-width 44px button), Hick (no second action), Chunking (6 cards, then fold). Data in cards and mocks must stay fictitious; a chosen name shows its initial, never a GitHub avatar.
