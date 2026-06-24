(() => {
// Settings — grouped sections (Common Region + chunking), clear labels,
// save confirmation toast, sensible defaults.
const { Card, Select, Input, Button, Alert, Badge } = window.KairuFocusDesignSystem_0d3740;

const SET_CSS = `
.kf-settings { max-width: var(--container-md); }
.kf-settings h1 { font-size: var(--text-xl); font-weight: 700; margin: 0 0 1.5rem; color: var(--text-primary); }
.kf-section { margin-bottom: 1.25rem; }
.kf-section h2 { font-size: var(--text-md); font-weight: 700; margin: 0 0 1rem; color: var(--text-primary); display: flex; align-items: center; gap: .5rem; }
.kf-field-row { margin-bottom: 1rem; }
.kf-hint { font-size: var(--text-xs); color: var(--text-muted); margin-top: .35rem; }
.kf-inline { display: flex; gap: .6rem; align-items: flex-end; }
.kf-grid3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: .75rem; }
.kf-actions { display: flex; gap: .6rem; margin-top: 1rem; }
`;

function SettingsScreen({ sprintGoal, onSprintGoal }) {
  React.useEffect(() => {
    if (document.getElementById("kf-set-css")) return;
    const s = document.createElement("style"); s.id = "kf-set-css"; s.textContent = SET_CSS; document.head.appendChild(s);
  }, []);
  const [theme, setTheme] = React.useState(document.documentElement.getAttribute("data-bs-theme") === "dark" ? "Dark" : "Light");
  const [ringtone, setRingtone] = React.useState("AlarmClock");
  const [sprint, setSprint] = React.useState(25);
  const [shortB, setShortB] = React.useState(5);
  const [longB, setLongB] = React.useState(15);
  const storeGoal = (window.KFData && window.KFData.focus) || {};
  const [sGoal, setSGoal] = React.useState(sprintGoal != null ? sprintGoal : (storeGoal.sprintGoal || 4));
  const [saved, setSaved] = React.useState(false);

  const applyTheme = (v) => { setTheme(v); document.documentElement.setAttribute("data-bs-theme", v.toLowerCase()); };
  const changeSprintGoal = (v) => { const n = Number(v); setSGoal(n); onSprintGoal && onSprintGoal(n); };
  const save = () => { setSaved(true); setTimeout(() => setSaved(false), 2600); };

  return (
    <div className="kf-settings">
      <h1>⚙ Paramètres</h1>

      <Card className="kf-section">
        <h2>🎨 Thème</h2>
        <div className="kf-field-row">
          <Select label="Préférence de thème" value={theme} onChange={(e) => applyTheme(e.target.value)}
            options={[{value:"Light",label:"☀️ Clair"},{value:"Dark",label:"🌙 Sombre"},{value:"System",label:"⚙️ Système"}]} />
          <div className="kf-hint">Le thème est appliqué immédiatement et sauvegardé.</div>
        </div>
      </Card>

      <Card className="kf-section">
        <h2>🔔 Sonnerie Pomodoro</h2>
        <div className="kf-inline">
          <div style={{ flex: 1 }}>
            <Select label="Son de fin de session" value={ringtone} onChange={(e) => setRingtone(e.target.value)}
              options={[{value:"None",label:"🔇 Aucun son"},{value:"AlarmClock",label:"⏰ Réveil"},{value:"Bird",label:"🐦 Oiseau"}]} />
          </div>
          <Button variant="secondary" disabled={ringtone === "None"}>▶ Tester</Button>
        </div>
        <div className="kf-hint">Joué automatiquement à la fin de chaque sprint ou pause.</div>
      </Card>

      <Card className="kf-section">
        <h2>Token MCP</h2>
        <div style={{ display: "flex", alignItems: "center", gap: ".6rem", marginBottom: ".75rem", fontSize: "var(--text-sm)", color: "var(--text-secondary)" }}>
          <Badge status="Done">Actif</Badge> Expire le <strong>03/03/2026</strong>
        </div>
        <div className="kf-actions" style={{ marginTop: 0 }}>
          <Button variant="secondary">Régénérer</Button>
          <Button variant="danger">Révoquer</Button>
        </div>
      </Card>

      <Card className="kf-section">
        <h2>🎯 Objectif de focus</h2>
        <div className="kf-field-row">
          <Select label="Sprints visés par jour" value={String(sGoal)} onChange={(e) => changeSprintGoal(e.target.value)}
            options={[{value:"2",label:"2 sprints"},{value:"4",label:"4 sprints"},{value:"6",label:"6 sprints"},{value:"8",label:"8 sprints"},{value:"10",label:"10 sprints"}]} />
          <div className="kf-hint">Votre objectif quotidien, affiché sur le Dashboard (« Focus aujourd'hui »). Un sprint = une session Pomodoro.</div>
        </div>
      </Card>

      <Card className="kf-section">
        <h2>🍅 Pomodoro</h2>
        <div className="kf-grid3">
          <Input label="Sprint (min)" type="number" value={sprint} onChange={(e) => setSprint(e.target.value)} />
          <Input label="Pause courte" type="number" value={shortB} onChange={(e) => setShortB(e.target.value)} />
          <Input label="Pause longue" type="number" value={longB} onChange={(e) => setLongB(e.target.value)} />
        </div>
        <div className="kf-hint">Entre 1 et 120 minutes. Une pause longue est recommandée tous les 4 sprints.</div>
        {saved && <div style={{ marginTop: "1rem" }}><Alert tone="success">Paramètres enregistrés avec succès !</Alert></div>}
        <div className="kf-actions">
          <Button variant="primary" onClick={save}>Enregistrer</Button>
          <Button variant="secondary" onClick={() => { setSprint(25); setShortB(5); setLongB(15); }}>Réinitialiser</Button>
        </div>
      </Card>
    </div>
  );
}

window.SettingsScreen = SettingsScreen;
})();
