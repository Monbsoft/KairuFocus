import * as React from "react";

/** Native select restyled to the token system. Pass `options` or `<option>` children. */
export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  /** Visible label rendered above the control. */
  label?: string;
  /** Convenience data — `{ value, label }[]`. Alternative to children. */
  options?: Array<{ value: string; label: string }>;
}

export function Select(props: SelectProps): JSX.Element;
