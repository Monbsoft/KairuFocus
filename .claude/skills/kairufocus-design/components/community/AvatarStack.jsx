import React from "react";

const CSS = `
.kf-avstack { display: inline-flex; align-items: center; }
.kf-avstack__item {
  flex: 0 0 auto; display: inline-grid; place-items: center; overflow: hidden;
  border-radius: 50%; line-height: 1; user-select: none;
  background: linear-gradient(135deg, var(--kf-blue-600), var(--kf-purple-500));
  color: var(--kf-white); font-family: var(--font-sans); font-weight: var(--weight-bold);
  /* Ring in the colour of the surface behind: the overlap reads on light, dark and the dark hero. */
  box-shadow: 0 0 0 2px var(--kf-avatar-ring, var(--surface-card));
}
.kf-avstack__item img { width: 100%; height: 100%; object-fit: cover; display: block; }
.kf-avstack__item + .kf-avstack__item { margin-left: -8px; }
.kf-avstack__more { background: var(--surface-sunken); color: var(--text-secondary); }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-avstack-css")) return;
    const s = document.createElement("style");
    s.id = "kf-avstack-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Overlapping avatars of the other members of a sprint, with a "+N" bubble for the rest.
 * Decorative (`aria-hidden`): write the names next to it. A member without `src` shows the initial of the
 * name, never a GitHub photo (a chosen name must not reveal the GitHub account).
 */
export function AvatarStack({ members = [], max = 3, size = 32, ringColor }) {
  useStyle();
  const shown = members.slice(0, Math.max(0, max));
  const hidden = Math.max(0, members.length - shown.length);
  const box = { width: size, height: size };
  return (
    <span className="kf-avstack" aria-hidden="true" style={ringColor ? { "--kf-avatar-ring": ringColor } : undefined}>
      {shown.map((m) => (
        <span key={m.name} className="kf-avstack__item" style={{ ...box, fontSize: size * 0.42 }}>
          {m.src ? <img src={m.src} alt="" /> : (m.name ? m.name[0].toUpperCase() : "?")}
        </span>
      ))}
      {hidden > 0 && (
        <span className="kf-avstack__item kf-avstack__more" style={{ ...box, fontSize: size * 0.38 }}>+{hidden}</span>
      )}
    </span>
  );
}
