import React from "react";

const CSS = `
.kf-select-wrap { position: relative; display: inline-flex; flex-direction: column; gap: 0.35rem; width: 100%; font-family: var(--font-sans); }
.kf-select {
  appearance: none; -webkit-appearance: none;
  font-family: var(--font-sans); font-size: var(--text-base); color: var(--text-primary);
  background: var(--surface-card); border: 1px solid var(--border-default);
  border-radius: var(--radius-sm); padding: 0 2.2rem 0 0.85rem; height: var(--target-min); width: 100%;
  cursor: pointer;
  transition: border-color var(--duration-fast) var(--ease-standard),
              box-shadow var(--duration-fast) var(--ease-standard);
}
.kf-select:hover { border-color: var(--border-strong); }
.kf-select:focus { outline: none; border-color: var(--primary); box-shadow: 0 0 0 3px var(--primary-subtle); }
.kf-select__chev { position: absolute; right: 0.8rem; bottom: 0; height: var(--target-min); display: flex; align-items: center; pointer-events: none; color: var(--text-muted); }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-select-css")) return;
    const s = document.createElement("style");
    s.id = "kf-select-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/** Native select, restyled to the token system with a custom chevron. */
export function Select({ label, id, children, options, className = "", ...rest }) {
  useStyle();
  const fid = id || React.useId();
  return (
    <div className="kf-select-wrap">
      {label && <label className="kf-field__label" htmlFor={fid}>{label}</label>}
      <div style={{ position: "relative" }}>
        <select id={fid} className={`kf-select ${className}`} {...rest}>
          {options ? options.map((o) => (
            <option key={o.value} value={o.value}>{o.label}</option>
          )) : children}
        </select>
        <span className="kf-select__chev" aria-hidden="true">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
        </span>
      </div>
    </div>
  );
}
