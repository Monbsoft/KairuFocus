import * as React from "react";

/**
 * Pomodoro circular progress ring with a center time label.
 */
export interface ProgressRingProps {
  /** Remaining fraction, 0–1. */
  progress?: number;
  /** Session state → arc color. */
  state?: "sprint" | "shortBreak" | "longBreak" | "interrupted" | "done" | "idle";
  /** Diameter in px. */
  size?: number;
  /** Stroke width in px. */
  stroke?: number;
  /** Center label (e.g. "24:59"). */
  label?: React.ReactNode;
  /** Smaller caption under the label (e.g. "Sprint en cours"). */
  sublabel?: React.ReactNode;
}

export function ProgressRing(props: ProgressRingProps): JSX.Element;
