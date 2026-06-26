(() => {
// Sprint libre (/pomodoro/libre) — open-ended count-up timer with optional
// note + linked task, finish/interrupt, and today's sprint history. Mirrors SprintLibre.razor.
const { Input, Select, Button, Badge, Card, Alert } = window.KairuFocusDesignSystem_0d3740;
const { ArrowLeft, Play, Check, Stop } = window.KFIcons;

const SL_CSS = `
.kf-sl { max-width: 560px; margin: 0 auto; display: flex; flex-direction: column; align-items: center; gap: 1.25rem; }
.kf-sl__head { width: 100%; }
.kf-sl__head h1 { font-size: var(--text-xl); font-weight: 700; margin: .75rem 0 0; color: var(--text-primary); }
.kf-sl__config { width: 100%; max-width: 480px; display: flex; flex-direction: column; gap: 1rem; }
.kf-sl__clock { font-family: var(--font-mono); font-size: 4rem; font-weight: 700; letter-spacing: .04em; font-variant-numeric: tabular-nums; line-height: 1; }
.kf-sl__status { color: var(--text-muted); font-size: var(--text-sm); }
.kf-sl__history { width: 100%; max-width: 560px; }
.kf-sl__history h2 { font-size: var(--text-md); font-weight: 700; margin: 0 0 .75rem; color: var(--text-primary); }
.kf-sl__row { display: flex; align-items: center; justify-content: space-between; gap: .75rem; padding: .65rem .9rem; background: var(--surface-card); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); }
.kf-sl__row + .kf-sl__row { margin-top: .5rem; }
.kf-sl__rowtask { font-size: var(--text-sm); color: var(--text-secondary); }
.kf-sl__rowdur { font-family: var(--font-mono); font-size: var(--text-sm); color: var(--text-muted); }
`;

function fmt(s) {
  return `${String(Math.floor(s / 3600)).padStart(2, "0")}:${String(Math.floor((s % 3600) / 60)).padStart(2, "0")}:${String(s % 60).padStart(2, "0")}`;
}

function SprintLibreScreen({ tasks, onBack }) {
  React.useEffect(() => {
    if (document.getElementById("kf-sl-css")) return;
    const s = document.createElement("style"); s.id = "kf-sl-css"; s.textContent = SL_CSS; document.head.appendChild(s);
  }, []);
  const [note, setNote] = React.useState("");
  const [taskId, setTaskId] = React.useState("");
  const [elapsed, setElapsed] = React.useState(0);
  const [running, setRunning] = React.useState(false);
  const [last, setLast] = React.useState(null);
  const [history, setHistory] = React.useState([
    { id: "h1", title: "Refactor du handler d'authentification JWT", dur: 1845, status: "Completed" },
    { id: "h2", title: null, dur: 612, status: "Interrupted" },
  ]);

  React.useEffect(() => {
    if (!running) return;
    const id = setInterval(() => setElapsed((e) => e + 1), 1000);
    return () => clearInterval(id);
  }, [running]);

  const available = tasks.filter((t) => t.status !== "Done");
  const start = () => { setElapsed(0); setRunning(true); setLast(null); };
  const stop = (status) => {
    setRunning(false);
    const t = available.find((x) => x.id === taskId);
    const entry = { id: "h" + Date.now(), title: t ? t.title : null, dur: elapsed, status };
    setHistory((h) => [...h, entry]); setLast(entry);
    setNote(""); setTaskId("");
  };

  return (
    <div className="kf-sl">
      <div className="kf-sl__head">
        <Button variant="ghost" size="sm" icon={<ArrowLeft size={16} />} onClick={onBack}>Retour</Button>
        <h1>Sprint libre</h1>
      </div>

      {!running && (
        <div className="kf-sl__config">
          <Input label="Note (optionnelle)" value={note} onChange={(e) => setNote(e.target.value)} placeholder="Ex. daily un peu long, bloqué sur X…" />
          <Select label="Tâche liée (optionnel)" value={taskId} onChange={(e) => setTaskId(e.target.value)}
            options={[{ value: "", label: "— Aucune tâche —" }, ...available.map((t) => ({ value: t.id, label: t.title }))]} />
        </div>
      )}

      <div className="kf-sl__clock" style={{ color: running ? "var(--primary)" : "var(--text-muted)" }}>{fmt(elapsed)}</div>
      {running && <div className="kf-sl__status">Sprint #{history.length + 1} en cours</div>}

      <div style={{ display: "flex", gap: ".6rem" }}>
        {!running ? (
          <Button variant="primary" size="lg" icon={<Play size={16} />} onClick={start}>Démarrer</Button>
        ) : (
          <>
            <Button variant="primary" size="lg" icon={<Check size={18} />} onClick={() => stop("Completed")}>Terminer</Button>
            <Button variant="secondary" size="lg" icon={<Stop size={16} />} onClick={() => stop("Interrupted")}>Interrompre</Button>
          </>
        )}
      </div>

      {last && (
        <Alert tone="success">Sprint enregistré — {fmt(last.dur)} — {last.status === "Completed" ? "Terminé" : "Interrompu"}</Alert>
      )}

      {history.length > 0 && (
        <div className="kf-sl__history">
          <h2>Sprints du jour ({history.length})</h2>
          {history.map((s) => (
            <div className="kf-sl__row" key={s.id}>
              <span className="kf-sl__rowtask">{s.title ? `🔗 ${s.title}` : <span style={{ color: "var(--text-muted)" }}>Sans tâche</span>}</span>
              <span style={{ display: "flex", alignItems: "center", gap: ".5rem" }}>
                <span className="kf-sl__rowdur">{fmt(s.dur)}</span>
                <Badge status={s.status === "Completed" ? "Done" : undefined} variant={s.status === "Completed" ? undefined : "inprogress"}>{s.status === "Completed" ? "Terminé" : "Interrompu"}</Badge>
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

window.SprintLibreScreen = SprintLibreScreen;
})();
