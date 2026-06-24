import * as React from "react";

/** User avatar — GitHub photo when `src` is set, otherwise initials on brand blue. */
export interface AvatarProps {
  /** Image URL (e.g. GitHub avatar). */
  src?: string;
  /** Display name — used for initials fallback and the title tooltip. */
  name?: string;
  /** Preset size or an explicit pixel number. */
  size?: "sm" | "md" | "lg" | number;
}

export function Avatar(props: AvatarProps): JSX.Element;
