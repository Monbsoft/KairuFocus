import * as React from "react";

/** Profile banner on the brand hero gradient: identity on the left, sprint status (`children`) on the right. */
export interface ProfileHeroProps {
  /** Profile name (a GitHub login or a chosen name). Never a real name. */
  name: string;
  /** Avatar URL. Pass it only when the name comes from GitHub; a chosen name shows its initial. */
  src?: string;
  /** GitHub login: shows the "Compte GitHub de …" link. Pass it only when the name comes from GitHub. */
  githubLogin?: string;
  /** Join line, already formatted, e.g. "Dans la communauté depuis octobre 2026". */
  since?: string;
  /** The status panel, usually a `<SprintStatus>`. */
  children?: React.ReactNode;
}

export function ProfileHero(props: ProfileHeroProps): JSX.Element;
