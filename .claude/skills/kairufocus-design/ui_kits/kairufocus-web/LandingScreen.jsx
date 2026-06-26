(() => {
// Landing / Home screen — hero + 4 feature blocks (the public "/" page).
const { Button } = window.KairuFocusDesignSystem_0d3740;
const { Github } = window.KFIcons;

const LANDING_CSS = `
.kf-landing { min-height: 100%; display: flex; flex-direction: column; }
.kf-hero {
  background: var(--brand-hero); color: #fff; text-align: center;
  padding: 5rem 1.5rem; display: flex; align-items: center; justify-content: center;
}
.kf-hero__in { max-width: 620px; }
.kf-hero__logo {
  width: 72px; height: 72px; border-radius: var(--radius-xl);
  background: linear-gradient(135deg, var(--kf-coral-500), var(--kf-ink-700));
  box-shadow: var(--shadow-brand); display: inline-flex; align-items: center; justify-content: center;
  font-size: 2rem; font-weight: 800; color: #fff; margin-bottom: 1.5rem;
}
.kf-hero__title { font-size: var(--text-3xl); font-weight: 800; letter-spacing: var(--tracking-tight); margin: 0 0 .5rem; }
.kf-hero__tag { font-size: var(--text-md); opacity: .82; font-weight: 300; margin: 0 0 1rem; }
.kf-hero__desc { font-size: var(--text-base); opacity: .68; line-height: var(--leading-relaxed); margin: 0 auto 2rem; max-width: 520px; }
.kf-features { padding: 4rem 1.5rem; background: var(--surface-base); }
.kf-features__grid { max-width: 960px; margin: 0 auto; display: grid; grid-template-columns: repeat(4, 1fr); gap: 1.25rem; }
.kf-feature {
  background: var(--surface-card); border: 1px solid var(--border-subtle); border-radius: var(--radius-lg);
  padding: 2rem 1.5rem; text-align: center; box-shadow: var(--shadow-sm);
  transition: transform var(--duration-base) var(--ease-standard), box-shadow var(--duration-base) var(--ease-standard);
}
.kf-feature:hover { transform: translateY(-4px); box-shadow: var(--shadow-lg); }
.kf-feature__icon { font-size: 2.5rem; display: block; margin-bottom: 1rem; }
.kf-feature__title { font-size: var(--text-md); font-weight: 700; margin: 0 0 .5rem; color: var(--text-primary); }
.kf-feature__desc { font-size: var(--text-sm); color: var(--text-muted); line-height: var(--leading-relaxed); margin: 0; }
@media (max-width: 820px) { .kf-features__grid { grid-template-columns: repeat(2, 1fr); } }
`;

const FEATURES = [
  { icon: "☑️", title: "Tâches", desc: "Micro-tâches quotidiennes avec statuts, descriptions et édition Markdown." },
  { icon: "🍅", title: "Pomodoro", desc: "Sprints et pauses minutés, suggestions et liens vers vos tâches." },
  { icon: "📖", title: "Journal", desc: "Timeline d'activité générée automatiquement. Annotez, parcourez l'historique." },
  { icon: "🤖", title: "IA & MCP", desc: "Serveur MCP intégré : votre IA crée, liste et complète vos tâches." },
];

function LandingScreen({ onLogin }) {
  React.useEffect(() => {
    if (document.getElementById("kf-landing-css")) return;
    const s = document.createElement("style"); s.id = "kf-landing-css"; s.textContent = LANDING_CSS; document.head.appendChild(s);
  }, []);
  return (
    <div className="kf-landing">
      <section className="kf-hero">
        <div className="kf-hero__in">
          <div className="kf-hero__logo">K</div>
          <h1 className="kf-hero__title">KairuFocus</h1>
          <p className="kf-hero__tag">L'espace de travail pour rester focus</p>
          <p className="kf-hero__desc">Tâches, sprints Pomodoro, journal d'activité — tout votre workflow en un seul endroit. Connectez votre IA favorite via le protocole MCP et laissez-la gérer vos tâches.</p>
          <Button variant="accent" size="lg" icon={<Github size={18} />} onClick={onLogin}>Se connecter avec GitHub</Button>
        </div>
      </section>
      <section className="kf-features">
        <div className="kf-features__grid">
          {FEATURES.map((f) => (
            <div className="kf-feature" key={f.title}>
              <span className="kf-feature__icon" aria-hidden="true">{f.icon}</span>
              <h3 className="kf-feature__title">{f.title}</h3>
              <p className="kf-feature__desc">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}

window.LandingScreen = LandingScreen;
})();
