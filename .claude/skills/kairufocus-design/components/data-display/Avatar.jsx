import React from "react";

const CSS = `
.kf-avatar {
  display: inline-flex; align-items: center; justify-content: center;
  border-radius: var(--radius-pill); overflow: hidden; flex-shrink: 0;
  background: var(--primary); color: var(--on-primary);
  font-family: var(--font-sans); font-weight: var(--weight-bold);
  border: 1px solid var(--border-subtle);
}
.kf-avatar img { width: 100%; height: 100%; object-fit: cover; display: block; }
`;

const SIZES = { sm: 28, md: 36, lg: 48 };

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-avatar-css")) return;
    const s = document.createElement("style");
    s.id = "kf-avatar-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

function initials(name) {
  if (!name) return "?";
  return name.trim().split(/\s+/).slice(0, 2).map((w) => w[0]).join("").toUpperCase();
}

/** User avatar — GitHub photo if `src`, else initials on brand blue. */
export function Avatar({ src, name, size = "md" }) {
  useStyle();
  const px = SIZES[size] || size;
  return (
    <span className="kf-avatar" style={{ width: px, height: px, fontSize: px * 0.42 }} title={name}>
      {src ? <img src={src} alt={name || ""} /> : initials(name)}
    </span>
  );
}
