(() => {
// Dashboard — Zeigarnik resume block, Goal-Gradient day progress,
// secondary grouped stats, reduced quick links (Hick), skeleton on load.
const { Card, StatCard, Badge, Button, Tag, Skeleton } = window.KairuFocusDesignSystem_0d3740;

const DASH_CSS = `
.kf-dash { max-width: var(--container-lg); }
.kf-dash__head { margin-bottom: 1.75rem; }
.kf-dash__greet { font-size: var(--text-2xl); font-weight: 700; margin: 0; color: var(--text-primary); }
.kf-dash__date { color: var(--text-muted); margin: .25rem 0 0; text-transform: capitalize; }
.kf-eyebrow { font-size: var(--text-xs); font-weight: 700; text-transform: uppercase; letter-spacing: var(--tracking-wide); color: var(--text-muted); margin: 0 0 .85rem; }
.kf-resume { padding: 0; overflow: hidden; }
.kf-resume__in { display: flex; align-items: center; gap: 1.25rem; padding: 1.25rem 1.5rem; }
.kf-resume__icon { width: 52px; height: 52px; border-radius: var(--radius-md); background: var(--warning-subtle); display: flex; align-items: center; justify-content: center; font-size: 1.7rem; flex-shrink: 0; }
.kf-resume__body { flex: 1; min-width: 0; }
.kf-resume__title { font-size: var(--text-md); font-weight: 700; color: var(--text-primary); margin: 0 0 .2rem; }
.kf-resume__meta { font-size: var(--text-sm); color: var(--text-muted); display: flex; gap: .4rem; align-items: center; flex-wrap: wrap; }
.kf-progress { display: flex; flex-direction: column; gap: .5rem; margin: 1.5rem 0 2rem; }
.kf-progress__row { display: flex; justify-content: space-between; align-items: baseline; }
.kf-progress__label { font-size: var(--text-sm); font-weight: 600; color: var(--text-secondary); }
.kf-progress__count { font-family: var(--font-mono); font-size: var(--text-sm); color: var(--text-muted); }
.kf-progress__track { height: 10px; border-radius: var(--radius-pill); background: var(--surface-sunken); overflow: hidden; }
.kf-progress__fill { height: 100%; border-radius: var(--radius-pill); background: linear-gradient(90deg, var(--primary), var(--success)); transition: width var(--duration-slow) var(--ease-out); }
.kf-focuscard { padding: 1.4rem 1.6rem; margin-bottom: 1.5rem; }
.kf-focushero__top { display: flex; align-items: baseline; justify-content: space-between; margin-bottom: .85rem; }
.kf-focushero__lbl { font-size: var(--text-sm); font-weight: 600; color: var(--text-secondary); }
.kf-focushero__count { font-family: var(--font-mono); font-size: var(--text-lg); font-weight: 700; color: var(--text-primary); font-variant-numeric: tabular-nums; }
.kf-focushero__hint { font-size: var(--text-sm); color: var(--text-muted); margin-top: .8rem; }
.kf-dots { display: flex; gap: 8px; flex-wrap: wrap; }
.kf-dot { width: 22px; height: 22px; border-radius: 50%; transition: background-color var(--duration-base) var(--ease-out), box-shadow var(--duration-base) var(--ease-out); }
.kf-dot--on { background: var(--pomo-sprint); box-shadow: 0 0 0 3px var(--primary-subtle); }
.kf-dot--off { background: var(--surface-sunken); border: 1px solid var(--border-default); }
.kf-focusmeta { display: flex; gap: 2rem; margin-top: 1.3rem; padding-top: 1.1rem; border-top: 1px solid var(--border-subtle); }
.kf-fi { display: flex; align-items: center; gap: .8rem; }
.kf-fi__icon { width: 44px; height: 44px; border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center; font-size: 1.35rem; flex-shrink: 0; }
.kf-fi__val { font-family: var(--font-mono); font-size: var(--text-lg); font-weight: 700; line-height: 1; color: var(--text-primary); font-variant-numeric: tabular-nums; }
.kf-fi__lbl { font-size: var(--text-xs); color: var(--text-muted); margin-top: .3rem; text-transform: uppercase; letter-spacing: .04em; font-weight: 700; }
.kf-statlink { display: flex; align-items: center; justify-content: space-between; width: 100%; margin-top: 1.2rem; padding: .7rem 0 0; border: none; border-top: 1px solid var(--border-subtle); background: none; cursor: pointer; font-family: var(--font-sans); font-size: var(--text-sm); font-weight: 600; color: var(--text-secondary); transition: color var(--duration-fast) var(--ease-standard); }
.kf-statlink:hover { color: var(--primary); }
.kf-statlink span:first-child { display: flex; align-items: center; gap: .5rem; }
@media (max-width: 720px) { .kf-focusmeta { gap: 1.25rem; } }
.kf-stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(190px, 1fr)); gap: 1rem; margin-bottom: 2rem; }
.kf-quick { display: flex; flex-wrap: wrap; gap: .75rem; }
.kf-quick a {
  display: flex; flex-direction: column; align-items: center; gap: .5rem; text-decoration: none;
  background: var(--surface-card); border: 1px solid var(--border-subtle); border-radius: var(--radius-lg);
  padding: 1rem 1.5rem; min-width: 96px; color: var(--text-secondary); cursor: pointer;
  transition: transform var(--duration-base) var(--ease-standard), box-shadow var(--duration-base) var(--ease-standard), border-color var(--duration-base) var(--ease-standard), color var(--duration-base) var(--ease-standard);
}
.kf-quick a:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); border-color: var(--primary-border); color: var(--primary); }
.kf-quick__icon { font-size: 1.5rem; }
.kf-quick__label { font-size: var(--text-xs); font-weight: 600; }
.kf-skelrow { display: flex; gap: 1rem; }
`;

function DashboardScreen({ user, tasks, currentSession, resumeTask, onNavigate, focus }) {
  React.useEffect(() => {
    if (!document.getElementById("kf-dash-css")) {
      const s = document.createElement("style"); s.id = "kf-dash-css"; s.textContent = DASH_CSS; document.head.appendChild(s);
    }
  }, []);
  const [loaded, setLoaded] = React.useState(false);
  React.useEffect(() => { const t = setTimeout(() => setLoaded(true), 650); return () => clearTimeout(t); }, []);

  const pending = tasks.filter((t) => t.status !== "Done").length;
  const done = tasks.filter((t) => t.status === "Done").length;
  const total = tasks.length;
  const pct = total ? Math.round((done / total) * 100) : 0;
  const date = new Date().toLocaleDateString("fr-FR", { weekday: "long", day: "numeric", month: "long", year: "numeric" });

  const f = focus || (window.KFData && window.KFData.focus) || { focusMinutes: 85, sprints: 3, sprintGoal: 4, streak: 5 };
  const fmtMin = (m) => `${Math.floor(m / 60)}h ${String(m % 60).padStart(2, "0")}`;
  const sprintsLeft = Math.max(0, f.sprintGoal - f.sprints);
  const goalReached = f.sprints >= f.sprintGoal;

  if (!loaded) {
    return (
      <div className="kf-dash">
        <div className="kf-dash__head">
          <Skeleton width={260} height={30} /><div style={{ height: 8 }} /><Skeleton width={200} height={16} />
        </div>
        <Skeleton width="100%" height={92} radius={12} />
        <div style={{ height: 24 }} />
        <Skeleton width="100%" height={140} radius={12} />
        <div style={{ height: 24 }} />
        <div className="kf-skelrow">
          {[0,1,2].map(i => <Skeleton key={i} width="100%" height={84} radius={12} />)}
        </div>
      </div>
    );
  }

  return (
    <div className="kf-dash">
      <div className="kf-dash__head">
        <h1 className="kf-dash__greet">Bonjour, {user.name} <span aria-hidden="true">👋</span></h1>
        <p className="kf-dash__date">{date}</p>
      </div>

      {/* Zeigarnik — what should I finish / resume? */}
      <h2 className="kf-eyebrow">À reprendre</h2>
      <Card interactive className="kf-resume" onClick={() => onNavigate(resumeTask ? "pomodoro" : "tasks")}>
        <div className="kf-resume__in">
          <div className="kf-resume__icon" aria-hidden="true">{resumeTask ? "⏸️" : "🚀"}</div>
          <div className="kf-resume__body">
            <p className="kf-resume__title">{resumeTask ? "Sprint #2 interrompu" : "Reprendre une tâche en cours"}</p>
            <div className="kf-resume__meta">
              <span>{resumeTask ? resumeTask.title : "Refactor du handler d'authentification JWT"}</span>
              <Badge status="InProgress" />
            </div>
          </div>
          <Button variant="primary">{resumeTask ? "Reprendre le sprint" : "Continuer"}</Button>
        </div>
      </Card>

      {/* Daily focus tracking — ONE goal in sprints (Goal-Gradient via dots,
          matching the Pomodoro mental model — Jakob). Focus time is info only. */}
      <h2 className="kf-eyebrow">Focus aujourd'hui</h2>
      <Card className="kf-focuscard">
        <div className="kf-focushero">
          <div className="kf-focushero__top">
            <span className="kf-focushero__lbl">Objectif de sprints</span>
            <span className="kf-focushero__count">{f.sprints} / {f.sprintGoal}</span>
          </div>
          <div className="kf-dots" role="img" aria-label={`${f.sprints} sprints effectués sur un objectif de ${f.sprintGoal}`}>
            {Array.from({ length: f.sprintGoal }).map((_, i) => (
              <span key={i} className={`kf-dot ${i < f.sprints ? "kf-dot--on" : "kf-dot--off"}`} />
            ))}
          </div>
          <div className="kf-focushero__hint">
            {goalReached ? "🎉 Objectif atteint — belle journée de focus !" : `Plus que ${sprintsLeft} sprint${sprintsLeft > 1 ? "s" : ""} pour atteindre votre objectif.`}
          </div>
        </div>
        <div className="kf-focusmeta">
          <div className="kf-fi">
            <div className="kf-fi__icon" style={{ background: "var(--primary-subtle)" }} aria-hidden="true">⏱️</div>
            <div><div className="kf-fi__val">{fmtMin(f.focusMinutes)}</div><div className="kf-fi__lbl">Temps de focus</div></div>
          </div>
          <div className="kf-fi">
            <div className="kf-fi__icon" style={{ background: "var(--danger-subtle)" }} aria-hidden="true">🔥</div>
            <div><div className="kf-fi__val">{f.streak} j</div><div className="kf-fi__lbl">Série de focus</div></div>
          </div>
        </div>
        <button className="kf-statlink" onClick={() => onNavigate("stats")}>
          <span><span aria-hidden="true">📊</span> Statistiques de focus</span>
          <span aria-hidden="true">→</span>
        </button>
      </Card>

      {/* Secondary, grouped stats */}
      <h2 className="kf-eyebrow">Aperçu</h2>
      <div className="kf-stats">
        <StatCard as="button" icon="☑️" value={pending} label="Tâches en cours" onClick={() => onNavigate("tasks")} />
        <StatCard as="button" icon="✓" value={done} label="Tâches terminées" tone="success" onClick={() => onNavigate("tasks")} />
        <StatCard as="button" icon="🍅" value={currentSession ? "En cours" : "—"} label={currentSession ? "Sprint actif" : "Aucun sprint actif"} tone="warning" active={!!currentSession} onClick={() => onNavigate("pomodoro")} />
        <StatCard as="button" icon="📖" value={6} label="Entrées aujourd'hui" tone="task" onClick={() => onNavigate("journal")} />
      </div>

      {/* Reduced quick links (Hick) */}
      <h2 className="kf-eyebrow">Accès rapides</h2>
      <div className="kf-quick">
        <a onClick={() => onNavigate("tasks")}><span className="kf-quick__icon" aria-hidden="true">☑️</span><span className="kf-quick__label">Mes tâches</span></a>
        <a onClick={() => onNavigate("pomodoro")}><span className="kf-quick__icon" aria-hidden="true">🍅</span><span className="kf-quick__label">Pomodoro</span></a>
        <a onClick={() => onNavigate("journal")}><span className="kf-quick__icon" aria-hidden="true">📖</span><span className="kf-quick__label">Journal</span></a>
      </div>
    </div>
  );
}

window.DashboardScreen = DashboardScreen;
})();
