import * as React from "react";

/** Conventional task checkbox; checking strikes through the label. 44px hit area. */
export interface CheckboxProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, "type"> {
  /** Text shown beside the box; struck through when checked. */
  label?: string;
  checked?: boolean;
}

export function Checkbox(props: CheckboxProps): JSX.Element;
