import * as React from "react";

/**
 * Surface container — the base panel/card primitive.
 */
export interface CardProps extends React.HTMLAttributes<HTMLElement> {
  /** Adds hover-lift + focus ring; use for clickable cards. */
  interactive?: boolean;
  /** Active/selected treatment (green border + tint). */
  active?: boolean;
  /** Remove inner padding (for media or custom layouts). */
  flush?: boolean;
  /** Render as another element, e.g. "a" or "button". */
  as?: any;
  children?: React.ReactNode;
}

export function Card(props: CardProps): JSX.Element;
