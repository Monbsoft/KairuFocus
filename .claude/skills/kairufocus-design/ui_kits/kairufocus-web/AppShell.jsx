(() => {
// App shell — persistent 64px sidebar rail (Jakob) + top row + content.
const { Avatar } = window.KairuFocusDesignSystem_0d3740;
const { Home, Logout } = window.KFIcons;

const SHELL_CSS = `
.kf-app { display: flex; min-height: 100vh; background: var(--surface-base); }
.kf-side {
  width: var(--sidebar-width); flex-shrink: 0; position: sticky; top: 0; height: 100vh;
  background: var(--brand-sidebar); display: flex; flex-direction: column; align-items: center;
}
.kf-side__brand {
  height: var(--topbar-height); width: 100%; display: flex; align-items: center; justify-content: center;
  background: rgba(0,0,0,0.4); color: #fff; font-size: 1.4rem; font-weight: 700; letter-spacing: .05em;
}
.kf-side__nav { display: flex; flex-direction: column; align-items: center; gap: .35rem; padding-top: .75rem; flex: 1; width: 100%; }
.kf-navicon {
  position: relative; width: 3rem; height: 3rem; border: none; background: transparent;
  color: #d7d7d7; border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center;
  font-size: 1.4rem; cursor: pointer; transition: background-color var(--duration-fast) var(--ease-standard), color var(--duration-fast) var(--ease-standard);
}
.kf-navicon:hover { background: rgba(255,255,255,0.15); color: #fff; }
.kf-navicon.is-active { background: rgba(255,255,255,0.37); color: #fff; }
.kf-navicon:focus-visible { outline: 2px solid #fff; outline-offset: 2px; }
.kf-navicon__tip {
  position: absolute; left: calc(100% + 12px); top: 50%; transform: translateY(-50%);
  background: rgba(0,0,0,.85); color: #fff; padding: .3rem .65rem; border-radius: 6px;
  font-size: .8rem; white-space: nowrap; pointer-events: none; opacity: 0;
  transition: opacity var(--duration-fast) var(--ease-standard); z-index: 50;
}
.kf-navicon:hover .kf-navicon__tip { opacity: 1; }
.kf-side__settings { margin-top: auto; border-top: 1px solid rgba(255,255,255,.1); padding-top: .5rem; width: 100%; display: flex; justify-content: center; }
.kf-side__logout { border-top: 1px solid rgba(255,255,255,.1); padding: .5rem 0; width: 100%; display: flex; justify-content: center; }
.kf-main { flex: 1; display: flex; flex-direction: column; min-width: 0; }
.kf-top {
  height: var(--topbar-height); position: sticky; top: 0; z-index: 10;
  background: var(--surface-card); border-bottom: 1px solid var(--border-subtle);
  display: flex; align-items: center; justify-content: flex-end; gap: 1rem; padding: 0 1.5rem;
}
.kf-top__user { display: flex; align-items: center; gap: .6rem; font-size: var(--text-sm); font-weight: 600; color: var(--text-secondary); }
.kf-theme {
  width: 40px; height: 40px; border-radius: var(--radius-md); border: 1px solid var(--border-subtle);
  background: var(--surface-card); color: var(--text-secondary); cursor: pointer; font-size: 1.1rem;
  display: flex; align-items: center; justify-content: center;
}
.kf-theme:hover { background: var(--surface-hover); }
.kf-content { padding: 1.5rem 2rem; flex: 1; }
`;

const NAV = [
  { id: "dashboard", icon: <Home size={20} />, label: "Tableau de bord" },
  { id: "tasks", icon: <span>☑️</span>, label: "Tâches" },
  { id: "pomodoro", icon: <span>🍅</span>, label: "Pomodoro" },
  { id: "journal", icon: <span>📖</span>, label: "Journal" },
];

function NavIcon({ item, active, onNavigate }) {
  return (
    <button className={`kf-navicon ${active === item.id ? "is-active" : ""}`}
            aria-label={item.label} aria-current={active === item.id ? "page" : undefined}
            onClick={() => onNavigate(item.id)}>
      <span aria-hidden="true" style={{ display: "inline-flex" }}>{item.icon}</span>
      <span className="kf-navicon__tip">{item.label}</span>
    </button>
  );
}

function AppShell({ active, onNavigate, onLogout, theme, onToggleTheme, user, children }) {
  React.useEffect(() => {
    if (document.getElementById("kf-shell-css")) return;
    const s = document.createElement("style"); s.id = "kf-shell-css"; s.textContent = SHELL_CSS; document.head.appendChild(s);
  }, []);
  return (
    <div className="kf-app">
      <aside className="kf-side">
        <div className="kf-side__brand">K</div>
        <nav className="kf-side__nav">
          {NAV.map((it) => <NavIcon key={it.id} item={it} active={active} onNavigate={onNavigate} />)}
          <div className="kf-side__settings">
            <NavIcon item={{ id: "settings", icon: <span>⚙️</span>, label: "Paramètres" }} active={active} onNavigate={onNavigate} />
          </div>
          <div className="kf-side__logout">
            <button className="kf-navicon" aria-label="Déconnexion" onClick={onLogout}>
              <Logout size={20} /><span className="kf-navicon__tip">Déconnexion</span>
            </button>
          </div>
        </nav>
      </aside>
      <div className="kf-main">
        <header className="kf-top">
          <button className="kf-theme" aria-label={theme === "dark" ? "Passer en clair" : "Passer en sombre"} onClick={onToggleTheme}>
            {theme === "dark" ? "☀️" : "🌙"}
          </button>
          <div className="kf-top__user">
            <span>{user.name}</span>
            <Avatar name={user.name} size="sm" />
          </div>
        </header>
        <main className="kf-content">{children}</main>
      </div>
    </div>
  );
}

window.AppShell = AppShell;
})();
