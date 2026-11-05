import React from "react";

const CSS = `
.kf-ats {
  display: flex; flex-wrap: wrap; align-items: center; gap: var(--space-2) var(--space-4);
  padding-top: var(--space-4); border-top: 1px solid rgba(255, 255, 255, 0.18);
  font-family: var(--font-sans); color: var(--kf-blue-100); font-size: var(--text-sm);
}
.kf-ats__title { margin: 0; font-weight: var(--weight-bold); }
.kf-ats__list { display: flex; flex-wrap: wrap; gap: 0 var(--space-4); min-width: 0; margin: 0; padding: 0; list-style: none; }
.kf-ats__list > li { min-width: 0; }
.kf-ats__chip {
  display: inline-flex; align-items: center; gap: var(--space-2); max-width: 100%; min-height: var(--target-min);
  color: var(--kf-white); font-weight: var(--weight-semibold); text-decoration: none;
}
.kf-ats__chip:hover .kf-ats__name { text-decoration: underline; }
.kf-ats__chip:focus-visible { outline: 2px solid var(--kf-blue-100); outline-offset: 2px; border-radius: var(--radius-sm); }
.kf-ats__avatar {
  flex: 0 0 auto; width: 28px; height: 28px; display: inline-grid; place-items: center; overflow: hidden;
  border-radius: 50%; font-size: 12px; font-weight: var(--weight-bold); line-height: 1;
  background: linear-gradient(135deg, var(--kf-blue-600), var(--kf-purple-500)); color: var(--kf-white);
}
.kf-ats__avatar img { width: 100%; height: 100%; object-fit: cover; display: block; }
/* A long name without spaces wraps instead of widening the line. */
.kf-ats__name { min-width: 0; overflow-wrap: anywhere; }
.kf-ats__empty { margin: 0; min-height: var(--target-min); display: flex; align-items: center; }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-ats-css")) return;
    const s = document.createElement("style");
    s.id = "kf-ats-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * "Actifs aujourd'hui": the last line of the live strip (Community page, iteration 45), under a rule, on the dark
 * brand hero. Up to five members who finished a sprint today, avatar and name, each a link to their profile, then
 * "et N autres". The viewer is in the list when they are one of them: `isMe` adds "(toi)" and the link goes to their
 * own profile. No live region: the list refreshes quietly.
 */
export function ActiveTodayStrip({
  title = "Actifs aujourd'hui", members = [], total, youLabel = "toi",
  moreText, emptyText = "Personne n'a encore terminé de sprint aujourd'hui.", hrefFor, meHref = "#",
}) {
  useStyle();
  const count = total ?? members.length;
  const others = Math.max(0, count - members.length);
  const href = (m) => (m.isMe ? meHref : hrefFor ? hrefFor(m) : "#");
  return (
    <div className="kf-ats">
      <p className="kf-ats__title" id="kf-ats-title">{title}</p>
      {members.length === 0 ? (
        <p className="kf-ats__empty">{emptyText}</p>
      ) : (
        <>
          <ul className="kf-ats__list" aria-labelledby="kf-ats-title">
            {members.map((m) => (
              <li key={m.name}>
                <a className="kf-ats__chip" href={href(m)}>
                  <span className="kf-ats__avatar" aria-hidden="true">
                    {m.src ? <img src={m.src} alt="" /> : (m.name ? m.name[0].toUpperCase() : "?")}
                  </span>
                  <span className="kf-ats__name">{m.isMe ? `${m.name} (${youLabel})` : m.name}</span>
                </a>
              </li>
            ))}
          </ul>
          {others > 0 && <span>{moreText ?? `et ${others} ${others === 1 ? "autre" : "autres"}`}</span>}
        </>
      )}
    </div>
  );
}
