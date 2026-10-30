import React from "react";

const CSS = `
/* Sits on the dark hero (the same in both themes): its colours are fixed, like the hero's. */
.kf-sstatus {
  flex: 0 1 340px; min-width: 0; display: flex; flex-direction: column; gap: var(--space-2);
  padding: var(--space-4); border-radius: var(--radius-md);
  background: rgba(255, 255, 255, 0.08); color: var(--kf-white); font-family: var(--font-sans);
}
.kf-sstatus__pill {
  display: inline-flex; align-items: center; gap: var(--space-2); width: fit-content; max-width: 100%;
  padding: 2px var(--space-3); border-radius: var(--radius-pill);
  font-size: var(--text-sm); font-weight: var(--weight-bold);
  background: color-mix(in srgb, var(--kf-green-400) 18%, transparent); color: var(--kf-green-400);
}
/* A fixed dot, in the colour of the text: the status does not blink, it would distract from the focus. */
.kf-sstatus__pill::before { content: ""; flex: 0 0 auto; width: 8px; height: 8px; border-radius: 50%; background: currentColor; }
.kf-sstatus__clock { font-family: var(--font-mono); font-variant-numeric: tabular-nums; }
.kf-sstatus__with { display: flex; align-items: center; gap: var(--space-2); min-width: 0; --kf-avatar-ring: var(--kf-ink-800); }
.kf-sstatus__names, .kf-sstatus__note { min-width: 0; margin: 0; font-size: var(--text-sm); color: var(--kf-blue-100); overflow-wrap: anywhere; }
.kf-sstatus__closed { font-weight: var(--weight-bold); }
.kf-sstatus__join {
  display: inline-flex; align-items: center; justify-content: center; align-self: flex-start;
  min-height: var(--target-min); padding: 0 var(--space-5); border: 0; border-radius: var(--radius-sm);
  font-family: inherit; font-size: var(--text-base); font-weight: var(--weight-semibold); cursor: pointer;
  background: var(--primary); color: var(--on-primary);
  transition: background-color var(--duration-fast) var(--ease-standard), transform var(--duration-fast) var(--ease-standard);
}
.kf-sstatus__join:hover { background: var(--primary-hover); }
.kf-sstatus__join:active { transform: translateY(1px); }
.kf-sstatus__join:focus-visible { outline: 2px solid var(--kf-blue-200); outline-offset: 2px; }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-sstatus-css")) return;
    const s = document.createElement("style");
    s.id = "kf-sstatus-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Sprint status panel of a profile, meant to sit in a `ProfileHero`.
 * `open`: "En sprint · encore 18:24", who is in it (`avatars` slot + `withText`) and a single primary button.
 * `closed`: "Pas de sprint ouvert pour le moment" and the number of sprints done together. The wording never says
 * "pas en sprint": that would be false for a closed sprint, a free sprint or a break.
 */
export function SprintStatus({
  state = "closed",
  timeLeft,
  openBefore = "En sprint · encore ",
  avatars,
  withText,
  joinLabel = "Rejoindre ce sprint",
  onJoin,
  closedLabel = "Pas de sprint ouvert pour le moment",
  togetherText,
}) {
  useStyle();
  if (state === "open") {
    return (
      <section className="kf-sstatus" aria-label="Statut du sprint">
        <span className="kf-sstatus__pill">
          {openBefore}<span className="kf-sstatus__clock">{timeLeft}</span>
        </span>
        {(avatars || withText) && (
          <div className="kf-sstatus__with">
            {avatars}
            {withText && <span className="kf-sstatus__names">{withText}</span>}
          </div>
        )}
        {onJoin && (
          <button type="button" className="kf-sstatus__join" onClick={onJoin}>{joinLabel}</button>
        )}
      </section>
    );
  }
  return (
    <section className="kf-sstatus" aria-label="Statut du sprint">
      <strong className="kf-sstatus__closed">{closedLabel}</strong>
      {togetherText && <p className="kf-sstatus__note">{togetherText}</p>}
    </section>
  );
}
