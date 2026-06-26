import * as React from "react";

/** Tag chip with a deterministic hash-derived color (same tag → same color). */
export interface TagProps {
  /** The tag text; its hash picks a stable palette color. */
  children: React.ReactNode;
  /** Prefix the label with `#`. */
  hash?: boolean;
  /** Show a removable `×`; called when clicked. */
  onRemove?: () => void;
  /** Force a specific color instead of the hashed one. */
  color?: string;
}

export function Tag(props: TagProps): JSX.Element;
