import React from "react";

const CSS = `
.kf-card {
  background: var(--surface-card); border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg); box-shadow: var(--shadow-sm);
  padding: var(--space-6); color: var(--text-primary);
}
.kf-card--flush { padding: 0; }
.kf-card--interactive { cursor: pointer; transition: box-shadow var(--duration-base) var(--ease-standard), transform var(--duration-base) var(--ease-standard), border-color var(--duration-base) var(--ease-standard); }
.kf-card--interactive:hover { box-shadow: var(--shadow-md); transform: translateY(-2px); }
.kf-card--interactive:focus-visible { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
.kf-card--active { border-color: var(--success); background: var(--success-subtle); }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-card-css")) return;
    const s = document.createElement("style");
    s.id = "kf-card-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/** Surface container. `interactive` adds the hover-lift affordance (Fitts). */
export function Card({ interactive = false, active = false, flush = false, as = "div", children, className = "", ...rest }) {
  useStyle();
  const Tag = as;
  const cls = [
    "kf-card",
    interactive ? "kf-card--interactive" : "",
    active ? "kf-card--active" : "",
    flush ? "kf-card--flush" : "",
    className,
  ].filter(Boolean).join(" ");
  return <Tag className={cls} {...rest}>{children}</Tag>;
}
