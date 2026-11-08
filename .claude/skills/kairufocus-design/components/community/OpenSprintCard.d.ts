import * as React from "react";

/** One card of the live strip "En sprint maintenant": host, countdown, participants, one action at most. Renders an `<li>`. */
export interface OpenSprintCardProps {
  /** Profile name of the host (or of the oldest participant if the host left). */
  name: string;
  /** Avatar URL, only for a GitHub name; omit for the initial. */
  src?: string;
  /** Link to the member's profile. */
  href?: string;
  /** Time left, "mm:ss", shown in big. */
  timeLeft: string;
  /** Word before the clock. Default "encore". */
  remainingLabel?: string;
  /** Already pluralised, e.g. "3 participants"; for one participant, "Personne ne l'a encore rejoint" (the default). */
  participantsText?: string;
  /** The viewer's own group: shows `mineLabel` and a link to the session instead of the button. */
  mine?: boolean;
  /** Default "Ton sprint". */
  mineLabel?: string;
  /** Default "Aller à ma session". */
  mineLinkLabel?: string;
  /** Link to the viewer's session. */
  mineHref?: string;
  /** False for a visitor outside the community: no button (the strip shows one notice for all cards). Default true. */
  canJoin?: boolean;
  /** Default "Rejoindre ce sprint". */
  joinLabel?: string;
  onJoin?: () => void;
}

export function OpenSprintCard(props: OpenSprintCardProps): JSX.Element;
