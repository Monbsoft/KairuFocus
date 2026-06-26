import * as React from "react";

/** Multi-line text area with optional live character counter. */
export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  /** Visible label rendered above the field. */
  label?: string;
  /** When set, shows a live `len/maxLength` counter that reddens past the limit. */
  maxLength?: number;
}

export function Textarea(props: TextareaProps): JSX.Element;
