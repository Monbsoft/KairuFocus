// Shared mock data for the UI kit.
window.KFData = {
  user: { name: "dev", login: "kairu-dev" },
  focus: { focusMinutes: 85, focusGoalMinutes: 120, sprints: 3, sprintGoal: 4, streak: 5 },
  week: [
    { d: "Jeu", n: 4 }, { d: "Ven", n: 6 }, { d: "Sam", n: 2 }, { d: "Dim", n: 0 },
    { d: "Lun", n: 5 }, { d: "Mar", n: 7 }, { d: "Auj", n: 3 },
  ],
  // 30 derniers jours (du plus ancien au plus récent ; dernier = aujourd'hui)
  month: [2,4,3,0,0,5,6, 3,4,2,1,0,4,5, 6,3,0,0,2,4,5, 7,3,4,2,0,1,5, 6,3],
  initialTasks: [
    { id: "t1", title: "Refactor du handler d'authentification JWT", status: "InProgress", tags: ["backend", "auth"], description: "Extraire la logique JWT dans son propre service." },
    { id: "t2", title: "Écrire les tests d'intégration du JournalApiClient", status: "Todo", tags: ["tests"], description: "" },
    { id: "t3", title: "Corriger le focus clavier sur la NavMenu", status: "Todo", tags: ["a11y", "ui"], description: "Ajouter un aria-label et un focus visible." },
    { id: "t4", title: "Centraliser les design tokens (light/dark)", status: "InProgress", tags: ["design-system"], description: "Couleurs, typo, spacing, radius, shadow, motion." },
    { id: "t5", title: "Mettre à jour la doc OAuth GitHub", status: "Done", tags: ["docs"], description: "" },
    { id: "t6", title: "Skeleton loaders sur le Dashboard", status: "Done", tags: ["ui", "perf"], description: "" },
  ],
  journal: [
    { id: "j1", time: "09:12", type: "SprintStarted", icon: "🍅", variant: "sprint", label: "Sprint #1 démarré", tasks: [{ title: "Refactor du handler d'authentification JWT", tags: ["backend"] }] },
    { id: "j2", time: "09:37", type: "SprintCompleted", icon: "✅", variant: "sprint", label: "Sprint #1 complété", tasks: [] },
    { id: "j3", time: "09:38", type: "BreakStarted", icon: "☕", variant: "break", label: "Pause #1 démarrée", tasks: [] },
    { id: "j4", time: "09:43", type: "TaskCompleted", icon: "🎉", variant: "task", label: "Tâche complétée", tasks: [{ title: "Skeleton loaders sur le Dashboard", tags: ["ui"] }], comment: "Enfin ! Beaucoup plus fluide au chargement." },
    { id: "j5", time: "10:05", type: "SprintStarted", icon: "🍅", variant: "sprint", label: "Sprint #2 démarré", tasks: [{ title: "Centraliser les design tokens", tags: ["design-system"] }] },
    { id: "j6", time: "10:30", type: "SprintInterrupted", icon: "⏸️", variant: "warn", label: "Sprint #2 interrompu", tasks: [] },
  ],
};
