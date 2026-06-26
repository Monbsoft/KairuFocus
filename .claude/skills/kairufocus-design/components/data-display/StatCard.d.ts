import * as React from "react";

/** Dashboard stat tile — icon + big value + muted label, clickable. */
export interface StatCardProps extends React.HTMLAttributes<HTMLElement> {
  /** Leading icon (emoji or SVG). */
  icon?: React.ReactNode;
  /** The headline value (number or short string). */
  value?: React.ReactNode;
  /** Muted caption under the value. */
  label?: React.ReactNode;
  /** Icon-tint accent. */
  tone?: "primary" | "success" | "warning" | "info" | "task";
  /** Live-state highlight (green). */
  active?: boolean;
  /** Element to render, default "a". */
  as?: any;
}

export function StatCard(props: StatCardProps): JSX.Element;
