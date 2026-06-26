import React from "react";

const CSS = `
.kf-stat {
  display: flex; align-items: center; gap: var(--space-4);
  background: var(--surface-card); border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg); padding: var(--space-5);
  cursor: pointer; text-decoration: none; color: var(--text-primary);
  transition: box-shadow var(--duration-base) var(--ease-standard), transform var(--duration-base) var(--ease-standard), border-color var(--duration-base) var(--ease-standard);
}
.kf-stat:hover { box-shadow: var(--shadow-md); transform: translateY(-2px); }
.kf-stat:focus-visible { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
.kf-stat--active { border-color: var(--success); background: var(--success-subtle); }
.kf-stat__icon { width: 44px; height: 44px; flex-shrink: 0; border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center; font-size: 1.5rem; background: var(--surface-sunken); }
.kf-stat__text { display: flex; flex-direction: column; gap: 0.25rem; min-width: 0; }
.kf-stat__value { display: block; font-size: var(--text-xl); font-weight: var(--weight-bold); line-height: 1; }
.kf-stat__value--active { color: var(--success); }
.kf-stat__label { display: block; font-size: var(--text-sm); color: var(--text-muted); }
`;

const ICON_TINT = {
  primary: "var(--primary-subtle)", success: "var(--success-subtle)",
  warning: "var(--warning-subtle)", info: "var(--info-subtle)", task: "var(--task-subtle)",
};

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-stat-css")) return;
    const s = document.createElement("style");
    s.id = "kf-stat-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Dashboard stat tile (Miller/chunking). Clickable, with an icon, a big
 * value and a muted label. `active` highlights a live state (e.g. running sprint).
 */
export function StatCard({ icon, value, label, tone = "primary", active = false, as = "a", ...rest }) {
  useStyle();
  const Tag = as;
  return (
    <Tag className={`kf-stat ${active ? "kf-stat--active" : ""}`} {...rest}>
      <span className="kf-stat__icon" style={{ background: ICON_TINT[tone] }} aria-hidden="true">{icon}</span>
      <span className="kf-stat__text">
        <span className={`kf-stat__value ${active ? "kf-stat__value--active" : ""}`}>{value}</span>
        <span className="kf-stat__label">{label}</span>
      </span>
    </Tag>
  );
}
