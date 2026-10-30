import * as React from "react";

/** One row of the "En sprint maintenant" list: host, time left, participants, join button. */
export interface OpenSprintRowProps {
  /** Profile name of the host (or of the oldest participant if the host left). */
  name: string;
  /** Avatar URL, only for a GitHub name; omit for the initial. */
  src?: string;
  /** Link to the member's profile. */
  href?: string;
  /** Time left, "mm:ss". */
  timeLeft: string;
  /** Word before the clock. Default "encore". */
  remainingLabel?: string;
  /** Already pluralised, e.g. "3 participants". */
  participantsText: string;
  /** The viewer's own group: shows `mineLabel` instead of the button. */
  mine?: boolean;
  /** Default "Ton sprint". */
  mineLabel?: string;
  /** Default "Rejoindre ce sprint". */
  joinLabel?: string;
  onJoin?: () => void;
}

export function OpenSprintRow(props: OpenSprintRowProps): JSX.Element;
