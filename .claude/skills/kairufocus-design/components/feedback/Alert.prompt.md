**Alert** — inline feedback banner. `tone`: success / danger / warning / info. `danger` gets `role="alert"`.

```jsx
<Alert tone="success">Paramètres enregistrés avec succès !</Alert>
<Alert tone="danger">Erreur lors de l'ajout de la tâche.</Alert>
```

**ProgressRing** — the Pomodoro timer. `progress` 0–1, `state` sets the arc color, `label`/`sublabel` fill the center.

```jsx
<ProgressRing progress={0.62} state="sprint" label="24:59" sublabel="Sprint en cours" />
<ProgressRing progress={1} state="done" label="✓" sublabel="Sprint terminé !" />
```

**Skeleton** — loading placeholder, replaces "Chargement…" text (Doherty). Compose to mimic the final layout.

```jsx
<Skeleton width={180} height={20} />
<Skeleton circle width={44} />
```
