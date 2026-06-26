import * as React from "react";

/** Skeleton loading placeholder (use instead of "Chargement…" text). */
export interface SkeletonProps {
  /** CSS width (number → px). */
  width?: number | string;
  /** CSS height (number → px). Ignored when `circle`. */
  height?: number | string;
  /** Border radius override. */
  radius?: number | string;
  /** Render a circle (uses `width` as diameter). */
  circle?: boolean;
  style?: React.CSSProperties;
  className?: string;
}

export function Skeleton(props: SkeletonProps): JSX.Element;
