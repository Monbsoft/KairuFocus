(() => {
// Statistiques de focus — dedicated page (keeps the Dashboard scannable, Hick).
// GitHub-style 30-day activity heatmap (brand blue), summary tiles.
const { Card, Button } = window.KairuFocusDesignSystem_0d3740;
const { ArrowLeft } = window.KFIcons;

const STATS_CSS = `
.kf-stats2 { max-width: var(--container-lg); }
.kf-stats2 h1 { font-size: var(--text-xl); font-weight: 700; margin: .75rem 0 1.25rem; color: var(--text-primary); }
.kf-stats2__tiles { display: grid; grid-template-columns: repeat(4, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
.kf-st { padding: 1.1rem 1.25rem; }
.kf-st__val { font-family: var(--font-mono); font-size: var(--text-2xl); font-weight: 700; line-height: 1; color: var(--text-primary); font-variant-numeric: tabular-nums; }
.kf-st__lbl { font-size: var(--text-xs); color: var(--text-muted); margin-top: .4rem; text-transform: uppercase; letter-spacing: .04em; font-weight: 700; }
.kf-heatcard { padding: 1.5rem 1.6rem; }
.kf-heatcard__head { display: flex; align-items: baseline; justify-content: space-between; margin-bottom: 1.2rem; gap: 1rem; flex-wrap: wrap; }
.kf-heatcard__title { font-size: var(--text-md); font-weight: 700; color: var(--text-primary); }
.kf-heatcard__sub { font-size: var(--text-xs); color: var(--text-muted); font-family: var(--font-mono); }
.kf-heat { display: grid; grid-template-rows: repeat(7, 17px); grid-auto-flow: column; grid-auto-columns: 17px; gap: 5px; }
.kf-cell { width: 17px; height: 17px; border-radius: 4px; }
.kf-cell--0 { background: var(--surface-sunken); border: 1px solid var(--border-subtle); }
.kf-cell--1 { background: color-mix(in srgb, var(--primary) 28%, var(--surface-sunken)); }
.kf-cell--2 { background: color-mix(in srgb, var(--primary) 52%, var(--surface-sunken)); }
.kf-cell--3 { background: color-mix(in srgb, var(--primary) 76%, var(--surface-sunken)); }
.kf-cell--4 { background: var(--primary); }
.kf-cell--today { box-shadow: 0 0 0 2px var(--surface-card), 0 0 0 3px var(--accent); }
.kf-legend { display: flex; align-items: center; gap: 6px; margin-top: 1.2rem; font-size: var(--text-xs); color: var(--text-muted); }
.kf-legend .kf-cell { width: 14px; height: 14px; }
@media (max-width: 720px) { .kf-stats2__tiles { grid-template-columns: 1fr 1fr; } .kf-heat { overflow-x: auto; } }
`;

function level(n, goal) {
  if (n <= 0) return 0;
  if (n < Math.ceil(goal / 2)) return 1;
  if (n < goal) return 2;
  if (n === goal) return 3;
  return 4;
}

function StatsScreen({ onBack }) {
  React.useEffect(() => {
    if (document.getElementById("kf-stats2-css")) return;
    const s = document.createElement("style"); s.id = "kf-stats2-css"; s.textContent = STATS_CSS; document.head.appendChild(s);
  }, []);
  const f = (window.KFData && window.KFData.focus) || { sprintGoal: 4, streak: 5 };
  const month = (window.KFData && window.KFData.month) || [];
  const goal = f.sprintGoal;
  const total = month.reduce((s, n) => s + n, 0);
  const avg = month.length ? Math.round((total / month.length) * 10) / 10 : 0;
  const activeDays = month.filter((n) => n > 0).length;
  const goalHit = month.filter((n) => n >= goal).length;

  // date label for each cell (J-29 … aujourd'hui)
  const label = (i) => {
    const dt = new Date(); dt.setDate(dt.getDate() - (month.length - 1 - i));
    return dt.toLocaleDateString("fr-FR", { weekday: "short", day: "numeric", month: "short" });
  };

  return (
    <div className="kf-stats2">
      <Button variant="ghost" size="sm" icon={<ArrowLeft size={16} />} onClick={onBack}>Retour</Button>
      <h1>📊 Statistiques de focus</h1>

      <div className="kf-stats2__tiles">
        <Card className="kf-st"><div className="kf-st__val">{total}</div><div className="kf-st__lbl">Sprints · 30 j</div></Card>
        <Card className="kf-st"><div className="kf-st__val">{avg}</div><div className="kf-st__lbl">Moyenne / jour</div></Card>
        <Card className="kf-st"><div className="kf-st__val">{activeDays}/30</div><div className="kf-st__lbl">Jours actifs</div></Card>
        <Card className="kf-st"><div className="kf-st__val">{f.streak} j</div><div className="kf-st__lbl">Série en cours</div></Card>
      </div>

      <Card className="kf-heatcard">
        <div className="kf-heatcard__head">
          <span className="kf-heatcard__title">Activité — 30 derniers jours</span>
          <span className="kf-heatcard__sub">{goalHit} jours d'objectif atteint · 1 carré = 1 jour</span>
        </div>
        <div className="kf-heat">
          {month.map((n, i) => (
            <div key={i}
                 className={`kf-cell kf-cell--${level(n, goal)} ${i === month.length - 1 ? "kf-cell--today" : ""}`}
                 title={`${label(i)} : ${n} sprint${n > 1 ? "s" : ""}`} />
          ))}
        </div>
        <div className="kf-legend">
          <span>Moins</span>
          <span className="kf-cell kf-cell--0" /><span className="kf-cell kf-cell--1" />
          <span className="kf-cell kf-cell--2" /><span className="kf-cell kf-cell--3" /><span className="kf-cell kf-cell--4" />
          <span>Plus</span>
        </div>
      </Card>
    </div>
  );
}

window.StatsScreen = StatsScreen;
})();
