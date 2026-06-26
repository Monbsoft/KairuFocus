(() => {
// Login screen (/login) — focused card, single action, reassuring. Mirrors Login.razor.
const { Button } = window.KairuFocusDesignSystem_0d3740;
const { Github } = window.KFIcons;

const LOGIN_CSS = `
.kf-login { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: var(--surface-base); padding: 1.5rem; }
.kf-login__card { display: flex; flex-direction: column; align-items: center; gap: 1rem; padding: 2.5rem 2rem; background: var(--surface-card);
  border: 1px solid var(--border-subtle); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); max-width: 380px; width: 100%; text-align: center; }
.kf-login__logo { width: 64px; height: 64px; border-radius: var(--radius-md); background: var(--brand-mark-bg); display: flex; align-items: center; justify-content: center; font-size: 2rem; font-weight: 800; color: #fff; }
.kf-login__title { font-size: 1.75rem; font-weight: 700; margin: .25rem 0 0; color: var(--text-primary); }
.kf-login__sub { color: var(--text-muted); margin: 0 0 .5rem; font-size: .9rem; }
`;

function LoginScreen({ onLogin }) {
  React.useEffect(() => {
    if (document.getElementById("kf-login-css")) return;
    const s = document.createElement("style"); s.id = "kf-login-css"; s.textContent = LOGIN_CSS; document.head.appendChild(s);
  }, []);
  return (
    <div className="kf-login">
      <div className="kf-login__card">
        <div className="kf-login__logo">K</div>
        <h1 className="kf-login__title">KairuFocus</h1>
        <p className="kf-login__sub">Gestion d'activité pour développeurs</p>
        <Button variant="accent" icon={<Github size={18} />} onClick={onLogin}>Se connecter avec GitHub</Button>
      </div>
    </div>
  );
}

window.LoginScreen = LoginScreen;
})();
