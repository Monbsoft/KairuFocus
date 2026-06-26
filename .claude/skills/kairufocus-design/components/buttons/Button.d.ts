import * as React from "react";

/**
 * Primary KairuFocus button for any clickable action.
 */
export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  /** Visual weight. `primary`/`accent` are isolation CTAs — one per view. */
  variant?: "primary" | "secondary" | "ghost" | "danger" | "accent";
  /** Height preset. `sm` is the only sub-44px size; reserve for dense rows. */
  size?: "sm" | "md" | "lg";
  /** Leading icon node (decorative, aria-hidden). */
  icon?: React.ReactNode;
  /** Trailing icon node. */
  iconRight?: React.ReactNode;
  /** Render as another element/component, e.g. "a" for a link button. */
  as?: any;
  children?: React.ReactNode;
}

export function Button(props: ButtonProps): JSX.Element;
