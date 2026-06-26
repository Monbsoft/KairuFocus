import * as React from "react";

/** Status / category badge. Use `status` for task states or `variant` for custom labels. */
export interface BadgeProps {
  /** App task status — maps to a localized French label + color. */
  status?: "Todo" | "Pending" | "InProgress" | "Done";
  /** Explicit visual variant (overrides status color). */
  variant?: "todo" | "inprogress" | "done" | "info" | "danger" | "solid";
  /** Show a leading status dot. */
  dot?: boolean;
  /** Custom label (overrides the status default). */
  children?: React.ReactNode;
  className?: string;
}

export function Badge(props: BadgeProps): JSX.Element;
