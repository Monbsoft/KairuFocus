import * as React from "react";

/** Single-line text field with optional label, hint and error message. */
export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  /** Visible label rendered above the field (wired via htmlFor). */
  label?: string;
  /** Helper text under the field. */
  hint?: string;
  /** Error message — turns the border red and sets aria-invalid. */
  error?: string;
}

export function Input(props: InputProps): JSX.Element;
