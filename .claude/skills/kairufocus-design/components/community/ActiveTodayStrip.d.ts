import * as React from "react";

/** One member of "Actifs aujourd'hui". */
export interface ActiveTodayMember {
  /** Profile name. */
  name: string;
  /** Avatar URL, only for a GitHub name; omit for the initial. */
  src?: string;
  /** The viewer: "(toi)" after the name, link to their own profile. */
  isMe?: boolean;
}

/** "Actifs aujourd'hui": the last line of the live strip, on the dark brand hero. */
export interface ActiveTodayStripProps {
  /** Default "Actifs aujourd'hui". */
  title?: string;
  /** Up to five members, most recent sprint end first. Empty shows `emptyText`. */
  members?: ActiveTodayMember[];
  /** All members active today; above `members.length`, "et N autres" is shown. Defaults to `members.length`. */
  total?: number;
  /** Default "toi". */
  youLabel?: string;
  /** Already pluralised; defaults to "et N autre(s)". */
  moreText?: string;
  /** Default "Personne n'a encore terminé de sprint aujourd'hui." */
  emptyText?: string;
  /** Link to a member's profile. */
  hrefFor?: (member: ActiveTodayMember) => string;
  /** Link to the viewer's own profile. */
  meHref?: string;
}

export function ActiveTodayStrip(props: ActiveTodayStripProps): JSX.Element;
