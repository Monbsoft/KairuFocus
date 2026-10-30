import * as React from "react";

export interface AvatarStackMember {
  /** Display name. Its first letter is the fallback initial. */
  name: string;
  /** Image URL, only for a name that comes from GitHub. Omit for a chosen name. */
  src?: string;
}

/** Overlapping avatars of the other members of a sprint, with a "+N" bubble for the rest. Decorative. */
export interface AvatarStackProps {
  members?: AvatarStackMember[];
  /** Avatars shown before the "+N" bubble. Default 3. */
  max?: number;
  /** Side of each avatar in pixels. Default 32. */
  size?: number;
  /** CSS colour of the ring between avatars; default is the card surface. Pass the surface behind the stack. */
  ringColor?: string;
}

export function AvatarStack(props: AvatarStackProps): JSX.Element;
