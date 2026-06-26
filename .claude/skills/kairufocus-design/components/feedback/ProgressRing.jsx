import React from "react";

const STATE_COLOR = {
  sprint: "var(--pomo-sprint)",
  shortBreak: "var(--pomo-short-break)",
  longBreak: "var(--pomo-long-break)",
  interrupted: "var(--pomo-interrupted)",
  done: "var(--success)",
  idle: "var(--text-muted)",
};

/**
 * Pomodoro progress ring. `progress` is 0–1 (remaining fraction). The arc
 * uses the session-state color and animates smoothly (Goal-Gradient).
 */
export function ProgressRing({
  progress = 1,
  state = "sprint",
  size = 220,
  stroke = 14,
  label,
  sublabel,
}) {
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const clamped = Math.max(0, Math.min(1, progress));
  const offset = c * (1 - clamped);
  const color = STATE_COLOR[state] || STATE_COLOR.sprint;
  return (
    <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} role="img"
         aria-label={label ? `${label} ${sublabel || ""}`.trim() : "Minuteur"}>
      <circle cx={size / 2} cy={size / 2} r={r} fill="none"
              stroke="var(--pomo-track)" strokeWidth={stroke} />
      <circle cx={size / 2} cy={size / 2} r={r} fill="none"
              stroke={color} strokeWidth={stroke} strokeLinecap="round"
              strokeDasharray={c} strokeDashoffset={offset}
              transform={`rotate(-90 ${size / 2} ${size / 2})`}
              style={{ transition: "stroke-dashoffset 0.8s linear, stroke 0.4s ease" }} />
      {label != null && (
        <text x="50%" y="48%" textAnchor="middle" dominantBaseline="middle"
              fontFamily="var(--font-mono)" fontWeight="700"
              fontSize={size * 0.19} fill="var(--text-primary)">{label}</text>
      )}
      {sublabel != null && (
        <text x="50%" y="66%" textAnchor="middle"
              fontFamily="var(--font-sans)" fontSize={size * 0.07}
              fill="var(--text-muted)">{sublabel}</text>
      )}
    </svg>
  );
}
