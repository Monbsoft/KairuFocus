(() => {
// Pomodoro — focus mode (Flow), smooth ring (Goal-Gradient), big primary
// controls (Fitts + Von Restorff), sprint tasks, celebratory end (Peak-End).
const { ProgressRing, Button, Badge, Alert, Card, IconButton } = window.KairuFocusDesignSystem_0d3740;
const { Play, Stop, Check, Cross } = window.KFIcons;

const POMO_CSS = `
.kf-pomo { max-width: 560px; margin: 0 auto; display: flex; flex-direction: column; align-items: center; gap: 1.25rem; }
.kf-pomo__head { width: 100%; display: flex; align-items: center; justify-content: space-between; }
.kf-pomo__head h1 { font-size: var(--text-xl); font-weight: 700; margin: 0; color: var(--text-primary); }
.kf-tabs { display: inline-flex; background: var(--surface-sunken); border-radius: var(--radius-md); padding: 4px; gap: 4px; }
.kf-tab {
  border: none; background: transparent; color: var(--text-secondary); cursor: pointer;
  padding: .5rem .9rem; border-radius: var(--radius-sm); font-size: var(--text-sm); font-weight: 600;
  font-family: var(--font-sans); transition: background-color var(--duration-fast) var(--ease-standard), color var(--duration-fast) var(--ease-standard);
}
.kf-tab:hover { color: var(--text-primary); }
.kf-tab.is-active { background: var(--surface-card); color: var(--text-primary); box-shadow: var(--shadow-xs); }
.kf-pomo__ring { padding: .5rem 0; }
.kf-sprinttasks { width: 100%; }
.kf-sprinttasks h2 { font-size: var(--text-md); font-weight: 700; margin: 0 0 .75rem; color: var(--text-primary); }
.kf-linked { display: flex; flex-direction: column; gap: .5rem; }
.kf-linkedrow {
  display: flex; align-items: center; justify-content: space-between; gap: .75rem;
  padding: .65rem .9rem; background: var(--surface-card); border: 1px solid var(--border-subtle); border-radius: var(--radius-md);
}
.kf-linkedrow__title { font-size: var(--text-sm); color: var(--text-primary); }
.kf-linkedrow__actions { display: flex; gap: .35rem; align-items: center; }
/* Focus mode (Flow) — strip the chrome */
.kf-focus { position: fixed; inset: 0; z-index: 100; background: var(--brand-hero); display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 2rem; }
.kf-focus__task { color: rgba(255,255,255,.85); font-size: var(--text-md); font-weight: 600; }
.kf-focus__exit { position: absolute; top: 1.5rem; right: 1.5rem; }
`;

const TYPES = [
  { id: "Sprint", label: "🍅 Sprint", min: 25, state: "sprint", status: "Sprint en cours" },
  { id: "ShortBreak", label: "☕ Pause courte", min: 5, state: "shortBreak", status: "Pause courte" },
  { id: "LongBreak", label: "🌙 Pause longue", min: 15, state: "longBreak", status: "Pause longue" },
];

function fmt(s) { return `${String(Math.floor(s / 60)).padStart(2, "0")}:${String(s % 60).padStart(2, "0")}`; }

function PomodoroScreen({ linkedTasks, onCompleteLinked, onUnlink, running, onRunningChange, onSprintLibre }) {
  React.useEffect(() => {
    if (document.getElementById("kf-pomo-css")) return;
    const s = document.createElement("style"); s.id = "kf-pomo-css"; s.textContent = POMO_CSS; document.head.appendChild(s);
  }, []);
  const [typeId, setTypeId] = React.useState("Sprint");
  const type = TYPES.find((t) => t.id === typeId);
  const total = type.min * 60;
  const [remaining, setRemaining] = React.useState(total);
  const [active, setActive] = React.useState(false);
  const [done, setDone] = React.useState(false);
  const [focus, setFocus] = React.useState(false);

  React.useEffect(() => { if (!active) { setRemaining(total); setDone(false); } }, [typeId]);
  React.useEffect(() => {
    if (!active) return;
    const id = setInterval(() => setRemaining((r) => {
      if (r <= 1) { clearInterval(id); setActive(false); setDone(true); onRunningChange && onRunningChange(false); return 0; }
      return r - 1;
    }), 1000);
    return () => clearInterval(id);
  }, [active]);

  const start = () => { setRemaining(total); setActive(true); setDone(false); onRunningChange && onRunningChange(true); };
  const interrupt = () => { setActive(false); setRemaining(total); onRunningChange && onRunningChange(false); };

  const progress = total ? remaining / total : 0;
  const ringState = done ? "done" : active ? type.state : "idle";
  const centerLabel = done ? "✓" : fmt(remaining);
  const centerSub = done ? "Terminé !" : active ? type.status : "Prêt";

  const ring = <ProgressRing progress={done ? 1 : progress} state={ringState} size={236} label={centerLabel} sublabel={centerSub} />;

  if (focus) {
    return (
      <div className="kf-focus">
        <div className="kf-focus__exit"><Button variant="ghost" onClick={() => setFocus(false)} style={{ color: "#fff" }}>Quitter le focus</Button></div>
        <div style={{ filter: "saturate(1.1)" }}>{ring}</div>
        {linkedTasks[0] && <div className="kf-focus__task">▸ {linkedTasks[0].title}</div>}
        <Button variant="accent" size="lg" icon={<Stop size={18} />} onClick={() => { interrupt(); setFocus(false); }}>Interrompre</Button>
      </div>
    );
  }

  return (
    <div className="kf-pomo">
      <div className="kf-pomo__head">
        <h1>Pomodoro</h1>
        {active ? <Button variant="secondary" onClick={() => setFocus(true)}>Mode focus</Button>
               : <Button variant="ghost" onClick={onSprintLibre}>Sprint libre</Button>}
      </div>

      {!active && !done && (
        <div className="kf-tabs" role="tablist" aria-label="Type de session">
          {TYPES.map((t) => (
            <button key={t.id} role="tab" aria-selected={t.id === typeId} className={`kf-tab ${t.id === typeId ? "is-active" : ""}`} onClick={() => setTypeId(t.id)}>
              {t.label} ({t.min} min)
            </button>
          ))}
        </div>
      )}

      <div className="kf-pomo__ring">{ring}</div>

      <div>
        {active ? (
          <Button variant="secondary" size="lg" icon={<Stop size={18} />} onClick={interrupt}>Interrompre</Button>
        ) : (
          <Button variant="primary" size="lg" icon={<Play size={16} />} onClick={start}>Démarrer</Button>
        )}
      </div>

      {done && (
        <Alert tone="success">🍅 Sprint terminé ! <strong>Pause recommandée : courte</strong></Alert>
      )}

      {active && typeId === "Sprint" && linkedTasks.length > 0 && (
        <div className="kf-sprinttasks">
          <h2>Tâches du sprint</h2>
          <div className="kf-linked">
            {linkedTasks.map((t) => (
              <div className="kf-linkedrow" key={t.id}>
                <span className="kf-linkedrow__title">{t.title}</span>
                <span className="kf-linkedrow__actions">
                  <Badge status={t.status} />
                  {t.status !== "Done" && <IconButton label={`Terminer ${t.title}`} tone="success" outline size="sm" onClick={() => onCompleteLinked(t.id)}><Check size={14} /></IconButton>}
                  <IconButton label={`Retirer ${t.title}`} tone="danger" outline size="sm" onClick={() => onUnlink(t.id)}><Cross size={13} /></IconButton>
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

window.PomodoroScreen = PomodoroScreen;
})();
