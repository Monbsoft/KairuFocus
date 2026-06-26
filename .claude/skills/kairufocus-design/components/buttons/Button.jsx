import React from "react";

const CSS = `
.kf-btn {
  display: inline-flex; align-items: center; justify-content: center; gap: 0.5rem;
  font-family: var(--font-sans); font-weight: var(--weight-semibold);
  border: 1px solid transparent; border-radius: var(--radius-sm);
  cursor: pointer; text-decoration: none; white-space: nowrap;
  transition: background-color var(--duration-fast) var(--ease-standard),
              border-color var(--duration-fast) var(--ease-standard),
              color var(--duration-fast) var(--ease-standard),
              box-shadow var(--duration-fast) var(--ease-standard),
              transform var(--duration-fast) var(--ease-standard);
}
.kf-btn:focus-visible { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
.kf-btn:active:not(:disabled) { transform: translateY(1px); }
.kf-btn:disabled { opacity: 0.55; cursor: not-allowed; }

/* sizes — all >= 44px tall except sm (compact rows) */
.kf-btn--sm { height: 36px; padding: 0 0.75rem; font-size: var(--text-sm); }
.kf-btn--md { height: var(--target-min); padding: 0 1.1rem; font-size: var(--text-base); }
.kf-btn--lg { height: var(--target-comfy); padding: 0 1.6rem; font-size: var(--text-md); }

/* variants */
.kf-btn--primary { background: var(--primary); color: var(--on-primary); }
.kf-btn--primary:hover:not(:disabled) { background: var(--primary-hover); }
.kf-btn--secondary { background: var(--surface-card); color: var(--text-primary); border-color: var(--border-default); }
.kf-btn--secondary:hover:not(:disabled) { background: var(--surface-hover); border-color: var(--border-strong); }
.kf-btn--ghost { background: transparent; color: var(--text-secondary); }
.kf-btn--ghost:hover:not(:disabled) { background: var(--surface-hover); color: var(--text-primary); }
.kf-btn--danger { background: var(--danger); color: #fff; }
.kf-btn--danger:hover:not(:disabled) { filter: brightness(0.94); }
.kf-btn--accent { background: var(--accent); color: #fff; box-shadow: var(--shadow-sm); }
.kf-btn--accent:hover:not(:disabled) { background: var(--accent-hover); }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-button-css")) return;
    const s = document.createElement("style");
    s.id = "kf-button-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Primary KairuFocus button. Use for any clickable action — Von Restorff
 * gives `primary`/`accent` their visual weight; keep one per view.
 */
export function Button({
  variant = "primary",
  size = "md",
  icon = null,
  iconRight = null,
  as = "button",
  children,
  className = "",
  ...rest
}) {
  useStyle();
  const Tag = as;
  return (
    <Tag className={`kf-btn kf-btn--${variant} kf-btn--${size} ${className}`} {...rest}>
      {icon && <span aria-hidden="true" style={{ display: "inline-flex" }}>{icon}</span>}
      {children}
      {iconRight && <span aria-hidden="true" style={{ display: "inline-flex" }}>{iconRight}</span>}
    </Tag>
  );
}
