import React from "react";

const CSS = `
/* The brand hero gradient is the same in light and dark, so the text on it is fixed light in both themes. */
.kf-phero {
  display: flex; flex-wrap: wrap; gap: var(--space-6); justify-content: space-between; align-items: center;
  padding: var(--space-6) var(--space-8); border-radius: var(--radius-lg);
  background: var(--brand-hero); color: var(--kf-white); box-shadow: var(--shadow-md);
  font-family: var(--font-sans);
}
.kf-phero__id { display: flex; gap: var(--space-4); align-items: center; min-width: 0; }
.kf-phero__avatar {
  flex: 0 0 auto; display: inline-grid; place-items: center; overflow: hidden; border-radius: 50%;
  width: 80px; height: 80px; font-size: 2rem; font-weight: var(--weight-bold); line-height: 1;
  background: linear-gradient(135deg, var(--kf-blue-600), var(--kf-purple-500)); color: var(--kf-white);
}
.kf-phero__avatar img { width: 100%; height: 100%; object-fit: cover; display: block; }
.kf-phero__who { min-width: 0; display: flex; flex-direction: column; gap: var(--space-1); }
.kf-phero__name {
  margin: 0; font-size: 1.75rem; font-weight: var(--weight-bold); line-height: 1.2;
  letter-spacing: var(--tracking-tight); overflow-wrap: anywhere;
}
.kf-phero__link {
  display: inline-flex; align-items: center; gap: var(--space-1); min-height: var(--target-min);
  font-size: var(--text-sm); color: inherit; text-decoration: underline; text-underline-offset: 2px;
}
.kf-phero__link:hover { color: var(--kf-blue-100); }
.kf-phero__link:focus-visible { outline: 2px solid var(--kf-blue-200); outline-offset: 2px; }
.kf-phero__since { margin: 0; font-size: var(--text-sm); color: var(--kf-blue-100); }
@media (max-width: 640px) {
  .kf-phero { padding: var(--space-5); }
  .kf-phero > .kf-sstatus { flex-basis: 100%; }
}
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-phero-css")) return;
    const s = document.createElement("style");
    s.id = "kf-phero-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Profile banner on the brand hero gradient: identity on the left (avatar, name, GitHub link, join date),
 * the sprint status on the right (`children`, usually a `SprintStatus`). The status keeps its place with or
 * without an open sprint (Jakob: same block as the Community page).
 */
export function ProfileHero({ name, src, githubLogin, since, children }) {
  useStyle();
  return (
    <header className="kf-phero">
      <div className="kf-phero__id">
        <span className="kf-phero__avatar" aria-hidden="true">
          {src ? <img src={src} alt="" /> : (name ? name[0].toUpperCase() : "?")}
        </span>
        <div className="kf-phero__who">
          <h1 className="kf-phero__name">{name}</h1>
          {githubLogin && (
            <a className="kf-phero__link" href={`https://github.com/${encodeURIComponent(githubLogin)}`}
               target="_blank" rel="noopener noreferrer">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"
                   strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M9 19c-5 1.5-5-2.5-7-3m14 6v-3.87a3.37 3.37 0 0 0-.94-2.61c3.14-.35 6.44-1.54 6.44-7A5.44 5.44 0 0 0 20 4.77 5.07 5.07 0 0 0 19.91 1S18.73.65 16 2.48a13.38 13.38 0 0 0-7 0C6.27.65 5.09 1 5.09 1A5.07 5.07 0 0 0 5 4.77a5.44 5.44 0 0 0-1.5 3.78c0 5.42 3.3 6.61 6.44 7A3.37 3.37 0 0 0 9 18.13V22" />
              </svg>
              <span>Compte GitHub de {githubLogin}</span>
            </a>
          )}
          {since && <p className="kf-phero__since">{since}</p>}
        </div>
      </div>
      {children}
    </header>
  );
}
