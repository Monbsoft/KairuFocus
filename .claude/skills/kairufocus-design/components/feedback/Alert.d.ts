import * as React from "react";

/** Inline feedback banner. */
export interface AlertProps {
  /** Severity → color + default icon. */
  tone?: "success" | "danger" | "warning" | "info";
  /** Override the leading icon. */
  icon?: React.ReactNode;
  children?: React.ReactNode;
  className?: string;
}

export function Alert(props: AlertProps): JSX.Element;
