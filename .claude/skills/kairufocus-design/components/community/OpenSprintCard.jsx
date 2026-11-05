import React from "react";

const CSS = `
.kf-osc {
  display: flex; flex-direction: column; gap: var(--space-3); min-width: 0; padding: var(--space-4);
  border-radius: var(--radius-md); font-family: var(--font-sans);
  background: rgba(255, 255, 255, 0.08); border: 1px solid rgba(255, 255, 255, 0.12);
}
.kf-osc--mine { border-style: dashed; }
.kf-osc__who {
  display: flex; align-items: center; gap: var(--space-3); min-width: 0; min-height: var(--target-min);
  color: var(--kf-white); font-weight: var(--weight-semibold); text-decoration: none;
}
.kf-osc__who:hover .kf-osc__name { text-decoration: underline; }
.kf-osc__avatar {
  flex: 0 0 auto; width: 48px; height: 48px; display: inline-grid; place-items: center; overflow: hidden;
  border-radius: 50%; font-size: 20px; font-weight: var(--weight-bold); line-height: 1;
  background: linear-gradient(135deg, var(--kf-blue-600), var(--kf-purple-500)); color: var(--kf-white);
}
.kf-osc__avatar img { width: 100%; height: 100%; object-fit: cover; display: block; }
/* A long name without spaces wraps instead of widening the card. */
.kf-osc__name { min-width: 0; overflow-wrap: anywhere; }
.kf-osc__time {
  display: flex; align-items: baseline; flex-wrap: wrap; gap: 0 var(--space-2); margin: 0;
  color: var(--kf-blue-100); font-size: var(--text-sm);
}
.kf-osc__clock {
  color: var(--kf-white); font-size: var(--text-xl); font-weight: var(--weight-bold); line-height: 1.2;
  font-variant-numeric: tabular-nums;
}
.kf-osc__with { margin: 0; color: var(--kf-blue-100); font-size: var(--text-sm); }
.kf-osc__join {
  margin-top: auto; display: inline-flex; align-items: center; justify-content: center; width: 100%;
  min-height: var(--target-min); padding: 0 var(--space-4); border: 0; border-radius: var(--radius-md);
  font-family: inherit; font-size: var(--text-sm); font-weight: var(--weight-semibold); cursor: pointer;
  /* Same blue in both themes: the card always sits on the dark brand hero. */
  background: var(--kf-blue-600); color: var(--kf-white);
  transition: background-color var(--duration-fast) var(--ease-standard), transform var(--duration-fast) var(--ease-standard);
}
.kf-osc__join:hover { background: var(--kf-blue-700); }
.kf-osc__join:active { transform: translateY(1px); }
.kf-osc__mine {
  margin-top: auto; display: flex; flex-wrap: wrap; align-items: center; gap: var(--space-1) var(--space-3);
  color: var(--kf-blue-100); font-size: var(--text-sm);
}
.kf-osc__mine strong { font-weight: var(--weight-bold); }
.kf-osc__mine a {
  display: inline-flex; align-items: center; min-height: var(--target-min);
  color: var(--kf-white); font-weight: var(--weight-semibold);
}
/* The page focus ring is blue on blue here: a light one on the dark hero. */
.kf-osc a:focus-visible, .kf-osc button:focus-visible { outline: 2px solid var(--kf-blue-100); outline-offset: 2px; }
@media (prefers-reduced-motion: reduce) { .kf-osc__join { transition: none; } }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-osc-css")) return;
    const s = document.createElement("style");
    s.id = "kf-osc-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * One card of the live strip "En sprint maintenant" (Community page, iteration 45): who, the countdown in big,
 * how many are in, and the way in. Always on the dark brand hero; render the cards in a `<ul>` grid of
 * `minmax(250px, 1fr)` columns. One filled action at most: the join button for a member, nothing for a
 * visitor outside the community (the strip says it once, above the cards), a link to my own session on my card.
 * Nothing is announced: the countdown has no live region.
 */
export function OpenSprintCard({
  name, src, href = "#", timeLeft, remainingLabel = "encore", participantsText = "Personne ne l'a encore rejoint",
  mine = false, mineLabel = "Ton sprint", mineLinkLabel = "Aller à ma session", mineHref = "#",
  canJoin = true, joinLabel = "Rejoindre ce sprint", onJoin,
}) {
  useStyle();
  return (
    <li className={`kf-osc${mine ? " kf-osc--mine" : ""}`} style={{ listStyle: "none" }}>
      <a className="kf-osc__who" href={href} aria-label={`Profil de ${name}`}>
        <span className="kf-osc__avatar" aria-hidden="true">
          {src ? <img src={src} alt="" /> : (name ? name[0].toUpperCase() : "?")}
        </span>
        <span className="kf-osc__name">{name}</span>
      </a>
      <p className="kf-osc__time">
        <span>{remainingLabel}</span>
        <span className="kf-osc__clock">{timeLeft}</span>
      </p>
      <p className="kf-osc__with">{participantsText}</p>
      {mine ? (
        <p className="kf-osc__mine">
          <strong>{mineLabel}</strong>
          <a href={mineHref}>{mineLinkLabel}</a>
        </p>
      ) : canJoin ? (
        <button type="button" className="kf-osc__join" onClick={onJoin} aria-label={`${joinLabel} de ${name}`}>{joinLabel}</button>
      ) : null}
    </li>
  );
}
