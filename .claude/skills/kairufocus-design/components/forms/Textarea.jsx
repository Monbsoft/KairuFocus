import React from "react";

const CSS = `
.kf-textarea {
  font-family: var(--font-sans); font-size: var(--text-base); color: var(--text-primary);
  background: var(--surface-card); border: 1px solid var(--border-default);
  border-radius: var(--radius-sm); padding: 0.6rem 0.85rem; width: 100%; resize: vertical;
  line-height: var(--leading-normal); min-height: 64px;
  transition: border-color var(--duration-fast) var(--ease-standard),
              box-shadow var(--duration-fast) var(--ease-standard);
}
.kf-textarea::placeholder { color: var(--text-muted); }
.kf-textarea:hover:not(:disabled) { border-color: var(--border-strong); }
.kf-textarea:focus { outline: none; border-color: var(--primary); box-shadow: 0 0 0 3px var(--primary-subtle); }
.kf-textarea-wrap { position: relative; }
.kf-textarea__count {
  position: absolute; right: 0.6rem; bottom: 0.45rem; font-size: var(--text-xs);
  font-family: var(--font-mono); color: var(--text-muted); pointer-events: none;
}
.kf-textarea__count--over { color: var(--danger); }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-textarea-css")) return;
    const s = document.createElement("style");
    s.id = "kf-textarea-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/** Multi-line text area with an optional live character counter (Postel/limits). */
export function Textarea({ label, id, maxLength, value, className = "", ...rest }) {
  useStyle();
  const fid = id || React.useId();
  const len = typeof value === "string" ? value.length : 0;
  const over = maxLength != null && len > maxLength;
  return (
    <div className="kf-field">
      {label && <label className="kf-field__label" htmlFor={fid}>{label}</label>}
      <div className="kf-textarea-wrap">
        <textarea id={fid} className={`kf-textarea ${className}`} value={value} maxLength={maxLength} {...rest} />
        {maxLength != null && (
          <span className={`kf-textarea__count ${over ? "kf-textarea__count--over" : ""}`}>{len}/{maxLength}</span>
        )}
      </div>
    </div>
  );
}
