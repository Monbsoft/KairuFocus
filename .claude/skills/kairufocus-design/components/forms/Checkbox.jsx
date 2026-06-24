import React from "react";

const CSS = `
.kf-check { display: inline-flex; align-items: flex-start; gap: 0.6rem; cursor: pointer; font-family: var(--font-sans); user-select: none; }
.kf-check input { position: absolute; opacity: 0; width: 0; height: 0; }
.kf-check__box {
  flex-shrink: 0; width: 22px; height: 22px; margin-top: 1px;
  border: 2px solid var(--border-strong); border-radius: var(--radius-xs);
  background: var(--surface-card); color: #fff;
  display: inline-flex; align-items: center; justify-content: center;
  transition: background-color var(--duration-fast) var(--ease-standard),
              border-color var(--duration-fast) var(--ease-standard);
}
/* generous hit area around the 22px box (Fitts) */
.kf-check__hit { display: inline-flex; align-items: center; justify-content: center; min-width: var(--target-min); min-height: var(--target-min); margin: calc(var(--target-min)/-2 + 11px) 0; }
.kf-check__box svg { opacity: 0; transform: scale(0.6); transition: all var(--duration-fast) var(--ease-out); }
.kf-check input:checked + .kf-check__hit .kf-check__box { background: var(--success); border-color: var(--success); }
.kf-check input:checked + .kf-check__hit .kf-check__box svg { opacity: 1; transform: scale(1); }
.kf-check input:focus-visible + .kf-check__hit .kf-check__box { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
.kf-check__label { font-size: var(--text-base); color: var(--text-primary); padding-top: 1px; }
.kf-check input:checked ~ .kf-check__label { color: var(--text-muted); text-decoration: line-through; }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-check-css")) return;
    const s = document.createElement("style");
    s.id = "kf-check-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Conventional task checkbox (Jakob's Law) — checking strikes through the
 * label. The visible box is 22px but the clickable area is 44px (Fitts).
 */
export function Checkbox({ label, checked, onChange, id, ...rest }) {
  useStyle();
  const fid = id || React.useId();
  return (
    <label className="kf-check" htmlFor={fid}>
      <input id={fid} type="checkbox" checked={checked} onChange={onChange} {...rest} />
      <span className="kf-check__hit">
        <span className="kf-check__box">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3.2" strokeLinecap="round" strokeLinejoin="round"><polyline points="20 6 9 17 4 12"/></svg>
        </span>
      </span>
      {label && <span className="kf-check__label">{label}</span>}
    </label>
  );
}
