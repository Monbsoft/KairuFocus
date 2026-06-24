import React from "react";

const CSS = `
.kf-badge {
  display: inline-flex; align-items: center; gap: 0.3rem;
  font-family: var(--font-sans); font-size: var(--text-xs); font-weight: var(--weight-bold);
  line-height: 1; padding: 0.3rem 0.55rem; border-radius: var(--radius-pill);
  white-space: nowrap; border: 1px solid transparent;
}
.kf-badge__dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
.kf-badge--todo       { background: var(--surface-sunken); color: var(--text-secondary); border-color: var(--border-subtle); }
.kf-badge--inprogress { background: var(--warning-subtle); color: var(--warning-text); }
.kf-badge--done       { background: var(--success-subtle); color: var(--success-text); }
.kf-badge--info       { background: var(--info-subtle); color: var(--info-text); }
.kf-badge--danger     { background: var(--danger-subtle); color: var(--danger-text); }
.kf-badge--solid      { background: var(--primary); color: var(--on-primary); }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-badge-css")) return;
    const s = document.createElement("style");
    s.id = "kf-badge-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

const STATUS = {
  Todo: { variant: "todo", label: "Todo" },
  Pending: { variant: "todo", label: "En attente" },
  InProgress: { variant: "inprogress", label: "En cours" },
  Done: { variant: "done", label: "Terminé" },
};

/**
 * Status / category badge. Pass `status` for the app's task states
 * (Todo / InProgress / Done) or `variant` + children for a custom label.
 */
export function Badge({ status, variant, dot = false, children, className = "" }) {
  useStyle();
  const meta = status ? STATUS[status] : null;
  const v = variant || (meta ? meta.variant : "todo");
  const text = children != null ? children : meta ? meta.label : status;
  return (
    <span className={`kf-badge kf-badge--${v} ${className}`}>
      {dot && <span className="kf-badge__dot" aria-hidden="true" />}
      {text}
    </span>
  );
}
