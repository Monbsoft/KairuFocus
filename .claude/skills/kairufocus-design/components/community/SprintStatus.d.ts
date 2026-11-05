import * as React from "react";

/** Sprint status panel of a profile, designed to sit inside a `ProfileHero`. */
export interface SprintStatusProps {
  /** `open` shows the live sprint and the join button; `closed` (default) shows "Pas de sprint ouvert pour le moment". */
  state?: "open" | "closed";
  /** Time left, "mm:ss" (monospace, tabular). Only for `open`. */
  timeLeft?: string;
  /** Text before the clock. Default "En sprint · encore ". */
  openBefore?: string;
  /** The other members of the sprint, usually `<AvatarStack members={…} />`. Only for `open`. */
  avatars?: React.ReactNode;
  /** Names next to the stack, e.g. "avec tomtom et 2 autres". Only for `open`. */
  withText?: string;
  /** Join button label. Default "Rejoindre ce sprint". */
  joinLabel?: string;
  /** Click handler of the join button; omit it to hide the button (own profile, or viewer already in the sprint). */
  onJoin?: () => void;
  /** Label when no sprint is open. */
  closedLabel?: string;
  /** Count line under the closed label, e.g. "38 sprints faits ensemble". Only for `closed`. */
  togetherText?: string;
}

export function SprintStatus(props: SprintStatusProps): JSX.Element;
