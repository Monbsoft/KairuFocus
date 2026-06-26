import React from "react";

const CSS = `
.kf-alert {
  display: flex; gap: 0.65rem; align-items: flex-start;
  font-family: var(--font-sans); font-size: var(--text-sm); line-height: var(--leading-normal);
  border: 1px solid transparent; border-radius: var(--radius-md);
  padding: 0.75rem 0.9rem;
}
.kf-alert__icon { flex-shrink: 0; line-height: 1.2; font-size: 1rem; }
.kf-alert__body { flex: 1; }
.kf-alert__body strong { font-weight: var(--weight-bold); }
.kf-alert--success { background: var(--success-subtle); color: var(--success-text); border-color: color-mix(in srgb, var(--success) 30%, transparent); }
.kf-alert--danger  { background: var(--danger-subtle);  color: var(--danger-text);  border-color: color-mix(in srgb, var(--danger) 30%, transparent); }
.kf-alert--warning { background: var(--warning-subtle); color: var(--warning-text); border-color: color-mix(in srgb, var(--warning) 35%, transparent); }
.kf-alert--info    { background: var(--info-subtle);    color: var(--info-text);    border-color: color-mix(in srgb, var(--info) 30%, transparent); }
`;

const ICONS = { success: "✓", danger: "⚠", warning: "ℹ", info: "ℹ" };

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-alert-css")) return;
    const s = document.createElement("style");
    s.id = "kf-alert-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/** Inline feedback banner (success / danger / warning / info). */
export function Alert({ tone = "info", icon, children, className = "" }) {
  useStyle();
  return (
    <div className={`kf-alert kf-alert--${tone} ${className}`} role={tone === "danger" ? "alert" : "status"}>
      <span className="kf-alert__icon" aria-hidden="true">{icon ?? ICONS[tone]}</span>
      <div className="kf-alert__body">{children}</div>
    </div>
  );
}
