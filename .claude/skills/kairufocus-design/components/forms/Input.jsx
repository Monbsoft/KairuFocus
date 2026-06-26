import React from "react";

const CSS = `
.kf-field { display: flex; flex-direction: column; gap: 0.35rem; font-family: var(--font-sans); }
.kf-field__label { font-size: var(--text-sm); font-weight: var(--weight-semibold); color: var(--text-secondary); }
.kf-field__hint { font-size: var(--text-xs); color: var(--text-muted); }
.kf-field__error { font-size: var(--text-xs); color: var(--danger); font-weight: var(--weight-medium); }
.kf-input {
  font-family: var(--font-sans); font-size: var(--text-base); color: var(--text-primary);
  background: var(--surface-card); border: 1px solid var(--border-default);
  border-radius: var(--radius-sm); padding: 0 0.85rem; height: var(--target-min); width: 100%;
  transition: border-color var(--duration-fast) var(--ease-standard),
              box-shadow var(--duration-fast) var(--ease-standard);
}
.kf-input::placeholder { color: var(--text-muted); }
.kf-input:hover:not(:disabled) { border-color: var(--border-strong); }
.kf-input:focus { outline: none; border-color: var(--primary); box-shadow: 0 0 0 3px var(--primary-subtle); }
.kf-input:disabled { opacity: 0.6; cursor: not-allowed; background: var(--surface-sunken); }
.kf-input--error { border-color: var(--danger); }
.kf-input--error:focus { box-shadow: 0 0 0 3px var(--danger-subtle); }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-input-css")) return;
    const s = document.createElement("style");
    s.id = "kf-input-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/** Single-line text field with optional label, hint and error. */
export function Input({ label, hint, error, id, className = "", ...rest }) {
  useStyle();
  const fid = id || React.useId();
  return (
    <div className="kf-field">
      {label && <label className="kf-field__label" htmlFor={fid}>{label}</label>}
      <input
        id={fid}
        className={`kf-input ${error ? "kf-input--error" : ""} ${className}`}
        aria-invalid={error ? "true" : undefined}
        {...rest}
      />
      {error ? <span className="kf-field__error">{error}</span>
             : hint ? <span className="kf-field__hint">{hint}</span> : null}
    </div>
  );
}
