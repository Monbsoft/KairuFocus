import React from "react";

const CSS = `
@keyframes kf-skeleton-pulse { 0%,100% { opacity: 1; } 50% { opacity: 0.45; } }
.kf-skeleton {
  display: block; background: var(--surface-sunken); border-radius: var(--radius-sm);
  animation: kf-skeleton-pulse 1.2s var(--ease-in-out) infinite;
}
@media (prefers-reduced-motion: reduce) { .kf-skeleton { animation: none; } }
`;

function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-skeleton-css")) return;
    const s = document.createElement("style");
    s.id = "kf-skeleton-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Skeleton placeholder — replaces "Chargement…" text while data loads
 * (Doherty < 400ms). Compose several to mimic the final layout.
 */
export function Skeleton({ width = "100%", height = 16, radius, circle = false, style = {}, className = "" }) {
  useStyle();
  return (
    <span
      className={`kf-skeleton ${className}`}
      aria-hidden="true"
      style={{
        width,
        height: circle ? width : height,
        borderRadius: circle ? "50%" : radius || undefined,
        ...style,
      }}
    />
  );
}
