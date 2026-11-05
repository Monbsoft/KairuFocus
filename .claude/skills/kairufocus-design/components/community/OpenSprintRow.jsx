import React from "react";

const CSS = `
.kf-orow {
  display: grid; grid-template-columns: minmax(0, 1fr) auto auto; align-items: center;
  column-gap: var(--space-4); row-gap: var(--space-2); padding: var(--space-3);
  border-radius: var(--radius-md); font-family: var(--font-sans); color: var(--text-primary);
}
.kf-orow + .kf-orow { border-top: 1px solid var(--border-subtle); }
.kf-orow__who {
  display: inline-flex; align-items: center; gap: var(--space-3); min-width: 0; min-height: var(--target-min);
  color: var(--text-primary); font-weight: var(--weight-semibold); text-decoration: none;
}
.kf-orow__who:hover .kf-orow__name { text-decoration: underline; }
.kf-orow__who:focus-visible { outline: 2px solid var(--focus-ring); outline-offset: 2px; border-radius: var(--radius-sm); }
.kf-orow__avatar {
  flex: 0 0 auto; width: 32px; height: 32px; display: inline-grid; place-items: center; overflow: hidden;
  border-radius: 50%; font-size: 13px; font-weight: var(--weight-bold); line-height: 1;
  background: linear-gradient(135deg, var(--kf-blue-600), var(--kf-purple-500)); color: var(--kf-white);
}
.kf-orow__avatar img { width: 100%; height: 100%; object-fit: cover; display: block; }
.kf-orow__name { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.kf-orow__meta { display: flex; flex-wrap: wrap; gap: var(--space-1) var(--space-2); margin: 0; font-size: var(--text-sm); color: var(--text-secondary); }
.kf-orow__clock { font-family: var(--font-mono); font-variant-numeric: tabular-nums; color: var(--text-primary); }
.kf-orow__join {
  display: inline-flex; align-items: center; justify-content: center; min-height: var(--target-min);
  padding: 0 var(--space-4); border: 0; border-radius: var(--radius-sm);
  font-family: inherit; font-size: var(--text-sm); font-weight: var(--weight-semibold); cursor: pointer;
  background: var(--primary); color: var(--on-primary);
  transition: background-color var(--duration-fast) var(--ease-standard), transform var(--duration-fast) var(--ease-standard);
}
.kf-orow__join:hover { background: var(--primary-hover); }
.kf-orow__join:active { transform: translateY(1px); }
.kf-orow__join:focus-visible { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
.kf-orow__mine {
  display: inline-flex; align-items: center; min-height: var(--target-min); padding: 0 var(--space-3);
  border-radius: var(--radius-sm); background: var(--primary-subtle); color: var(--text-primary);
  font-size: var(--text-sm); font-weight: var(--weight-bold);
}
@media (max-width: 767px) { .kf-orow { grid-template-columns: minmax(0, 1fr); } }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-orow-css")) return;
    const s = document.createElement("style");
    s.id = "kf-orow-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * One row of "En sprint maintenant": one row per open group, with the host (or the oldest participant),
 * the time left, the number of participants and a way in. Put the rows in a `Card`.
 * Nothing is announced when rows come and go (no live region): a sprint must not be interrupted.
 */
export function OpenSprintRow({
  name, src, href = "#", timeLeft, remainingLabel = "encore", participantsText,
  mine = false, mineLabel = "Ton sprint", joinLabel = "Rejoindre ce sprint", onJoin,
}) {
  useStyle();
  return (
    <div className="kf-orow">
      <a className="kf-orow__who" href={href} aria-label={`Profil de ${name}`}>
        <span className="kf-orow__avatar" aria-hidden="true">
          {src ? <img src={src} alt="" /> : (name ? name[0].toUpperCase() : "?")}
        </span>
        <span className="kf-orow__name">{name}</span>
      </a>
      <p className="kf-orow__meta">
        <span>{remainingLabel} <span className="kf-orow__clock">{timeLeft}</span></span>
        <span aria-hidden="true">·</span>
        <span>{participantsText}</span>
      </p>
      <div>
        {mine
          ? <span className="kf-orow__mine">{mineLabel}</span>
          : <button type="button" className="kf-orow__join" onClick={onJoin} aria-label={`${joinLabel} de ${name}`}>{joinLabel}</button>}
      </div>
    </div>
  );
}
