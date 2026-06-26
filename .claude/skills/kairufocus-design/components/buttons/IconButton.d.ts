import * as React from "react";

/**
 * Icon-only button with a required accessible label and a 44px target.
 */
export interface IconButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  /** REQUIRED accessible name — sets aria-label and the tooltip. */
  label: string;
  /** Hover accent. */
  tone?: "default" | "primary" | "success" | "danger";
  /** 44px (md) or compact 36px (sm). */
  size?: "sm" | "md";
  /** Show a resting border. */
  outline?: boolean;
  /** The icon glyph/SVG. */
  children?: React.ReactNode;
}

export function IconButton(props: IconButtonProps): JSX.Element;
