import React from "react";

const CSS = `
.kf-iconbtn {
  display: inline-flex; align-items: center; justify-content: center;
  width: var(--target-min); height: var(--target-min);
  border: 1px solid transparent; border-radius: var(--radius-md);
  background: transparent; color: var(--text-secondary);
  cursor: pointer; flex-shrink: 0;
  transition: background-color var(--duration-fast) var(--ease-standard),
              color var(--duration-fast) var(--ease-standard),
              border-color var(--duration-fast) var(--ease-standard);
}
.kf-iconbtn:hover:not(:disabled) { background: var(--surface-hover); color: var(--text-primary); }
.kf-iconbtn:focus-visible { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
.kf-iconbtn:disabled { opacity: 0.5; cursor: not-allowed; }
.kf-iconbtn--sm { width: 36px; height: 36px; }
.kf-iconbtn--outline { border-color: var(--border-default); }
.kf-iconbtn--primary:hover:not(:disabled) { background: var(--primary-subtle); color: var(--primary); border-color: var(--primary-border); }
.kf-iconbtn--success:hover:not(:disabled) { background: var(--success-subtle); color: var(--success); border-color: var(--success); }
.kf-iconbtn--danger:hover:not(:disabled) { background: var(--danger-subtle); color: var(--danger); border-color: var(--danger); }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-iconbtn-css")) return;
    const s = document.createElement("style");
    s.id = "kf-iconbtn-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Icon-only button. ALWAYS pass `label` — it becomes the accessible name
 * (aria-label) and the native tooltip. 44px target by default (Fitts/AA).
 */
export function IconButton({
  label,
  tone = "default",
  size = "md",
  outline = false,
  children,
  className = "",
  ...rest
}) {
  useStyle();
  const cls = [
    "kf-iconbtn",
    `kf-iconbtn--${tone}`,
    size === "sm" ? "kf-iconbtn--sm" : "",
    outline ? "kf-iconbtn--outline" : "",
    className,
  ].filter(Boolean).join(" ");
  return (
    <button type="button" className={cls} aria-label={label} title={label} {...rest}>
      <span aria-hidden="true" style={{ display: "inline-flex", lineHeight: 0 }}>{children}</span>
    </button>
  );
}
