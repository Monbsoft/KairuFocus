import React from "react";

const CSS = `
.kf-tag {
  display: inline-flex; align-items: center; gap: 0.3rem;
  font-family: var(--font-sans); font-size: var(--text-xs); font-weight: var(--weight-semibold);
  line-height: 1; padding: 0.28rem 0.5rem; border-radius: var(--radius-xs);
  color: #fff; white-space: nowrap;
}
.kf-tag__hash { opacity: 0.7; }
.kf-tag__x {
  display: inline-flex; align-items: center; justify-content: center;
  margin-left: 0.1rem; width: 14px; height: 14px; border-radius: 50%;
  background: rgba(255,255,255,0.25); cursor: pointer; border: none; color: #fff;
  font-size: 10px; line-height: 1; padding: 0;
}
.kf-tag__x:hover { background: rgba(255,255,255,0.45); }
`;

/* Palette mirrors the app's TagColors.cs (deterministic djb2 hash). */
const PALETTE = [
  "var(--primary)", "var(--success)", "var(--danger)", "var(--warning)",
  "var(--info)", "var(--text-muted)", "var(--task)", "var(--kf-orange-500)",
  "var(--kf-teal-500)", "var(--kf-pink-500)",
];
const DARK_TEXT = new Set([3, 4]); // warning, info read better with dark text

function hashColor(tag) {
  const n = (tag || "").toLowerCase();
  let hash = 5381 >>> 0;
  for (let i = 0; i < n.length; i++) hash = (((hash * 33) >>> 0) ^ n.charCodeAt(i)) >>> 0;
  return hash % PALETTE.length;
}

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-tag-css")) return;
    const s = document.createElement("style");
    s.id = "kf-tag-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Tag chip with a deterministic, hash-derived color (same tag → same color,
 * matching the app). Optional `#` prefix and removable `x`.
 */
export function Tag({ children, hash = false, onRemove, color }) {
  useStyle();
  const text = String(children ?? "");
  const idx = hashColor(text);
  const bg = color || PALETTE[idx];
  const dark = !color && DARK_TEXT.has(idx);
  return (
    <span className="kf-tag" style={{ background: bg, color: dark ? "#1a1a1a" : "#fff" }}>
      {hash && <span className="kf-tag__hash">#</span>}
      {text}
      {onRemove && (
        <button type="button" className="kf-tag__x" aria-label={`Retirer ${text}`} onClick={onRemove}>×</button>
      )}
    </span>
  );
}
