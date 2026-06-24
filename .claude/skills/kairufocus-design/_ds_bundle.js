/* @ds-bundle: {"format":3,"namespace":"KairuFocusDesignSystem_0d3740","components":[{"name":"Button","sourcePath":"components/buttons/Button.jsx"},{"name":"IconButton","sourcePath":"components/buttons/IconButton.jsx"},{"name":"Avatar","sourcePath":"components/data-display/Avatar.jsx"},{"name":"Badge","sourcePath":"components/data-display/Badge.jsx"},{"name":"Card","sourcePath":"components/data-display/Card.jsx"},{"name":"StatCard","sourcePath":"components/data-display/StatCard.jsx"},{"name":"Tag","sourcePath":"components/data-display/Tag.jsx"},{"name":"Alert","sourcePath":"components/feedback/Alert.jsx"},{"name":"ProgressRing","sourcePath":"components/feedback/ProgressRing.jsx"},{"name":"Skeleton","sourcePath":"components/feedback/Skeleton.jsx"},{"name":"Checkbox","sourcePath":"components/forms/Checkbox.jsx"},{"name":"Input","sourcePath":"components/forms/Input.jsx"},{"name":"Select","sourcePath":"components/forms/Select.jsx"},{"name":"Textarea","sourcePath":"components/forms/Textarea.jsx"}],"sourceHashes":{"components/buttons/Button.jsx":"b4f5e4e470df","components/buttons/IconButton.jsx":"df7a66b35d76","components/data-display/Avatar.jsx":"2af6c8aafe0e","components/data-display/Badge.jsx":"caf00e219727","components/data-display/Card.jsx":"0e3c646d40f7","components/data-display/StatCard.jsx":"cffbe67e79e3","components/data-display/Tag.jsx":"fcc98828000c","components/feedback/Alert.jsx":"ebb7f704f3c4","components/feedback/ProgressRing.jsx":"46961ebd43ce","components/feedback/Skeleton.jsx":"6bf5754d9963","components/forms/Checkbox.jsx":"caa9de1475ff","components/forms/Input.jsx":"e75ca716314f","components/forms/Select.jsx":"39e51b758470","components/forms/Textarea.jsx":"98a13023eb66","ui_kits/kairufocus-web/AppShell.jsx":"73ea142452a1","ui_kits/kairufocus-web/DashboardScreen.jsx":"8feb1004f88d","ui_kits/kairufocus-web/JournalScreen.jsx":"2321142f4cad","ui_kits/kairufocus-web/LandingScreen.jsx":"d8d263950581","ui_kits/kairufocus-web/LoginScreen.jsx":"e225649b00b6","ui_kits/kairufocus-web/PomodoroScreen.jsx":"6939f6564bf5","ui_kits/kairufocus-web/SettingsScreen.jsx":"94f5e4d48115","ui_kits/kairufocus-web/SprintLibreScreen.jsx":"f4edbabeee63","ui_kits/kairufocus-web/StatsScreen.jsx":"df097f5360aa","ui_kits/kairufocus-web/TaskEditScreen.jsx":"f6ac8ba3ab99","ui_kits/kairufocus-web/TasksScreen.jsx":"91701740f2c2","ui_kits/kairufocus-web/icons.jsx":"f7fc5c593f6d","ui_kits/kairufocus-web/mockData.jsx":"22dfce3eb564"},"inlinedExternals":[],"unexposedExports":[]} */

(() => {

const __ds_ns = (window.KairuFocusDesignSystem_0d3740 = window.KairuFocusDesignSystem_0d3740 || {});

const __ds_scope = {};

(__ds_ns.__errors = __ds_ns.__errors || []);

// components/buttons/Button.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.kf-btn {
  display: inline-flex; align-items: center; justify-content: center; gap: 0.5rem;
  font-family: var(--font-sans); font-weight: var(--weight-semibold);
  border: 1px solid transparent; border-radius: var(--radius-sm);
  cursor: pointer; text-decoration: none; white-space: nowrap;
  transition: background-color var(--duration-fast) var(--ease-standard),
              border-color var(--duration-fast) var(--ease-standard),
              color var(--duration-fast) var(--ease-standard),
              box-shadow var(--duration-fast) var(--ease-standard),
              transform var(--duration-fast) var(--ease-standard);
}
.kf-btn:focus-visible { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
.kf-btn:active:not(:disabled) { transform: translateY(1px); }
.kf-btn:disabled { opacity: 0.55; cursor: not-allowed; }

/* sizes — all >= 44px tall except sm (compact rows) */
.kf-btn--sm { height: 36px; padding: 0 0.75rem; font-size: var(--text-sm); }
.kf-btn--md { height: var(--target-min); padding: 0 1.1rem; font-size: var(--text-base); }
.kf-btn--lg { height: var(--target-comfy); padding: 0 1.6rem; font-size: var(--text-md); }

/* variants */
.kf-btn--primary { background: var(--primary); color: var(--on-primary); }
.kf-btn--primary:hover:not(:disabled) { background: var(--primary-hover); }
.kf-btn--secondary { background: var(--surface-card); color: var(--text-primary); border-color: var(--border-default); }
.kf-btn--secondary:hover:not(:disabled) { background: var(--surface-hover); border-color: var(--border-strong); }
.kf-btn--ghost { background: transparent; color: var(--text-secondary); }
.kf-btn--ghost:hover:not(:disabled) { background: var(--surface-hover); color: var(--text-primary); }
.kf-btn--danger { background: var(--danger); color: #fff; }
.kf-btn--danger:hover:not(:disabled) { filter: brightness(0.94); }
.kf-btn--accent { background: var(--accent); color: #fff; box-shadow: var(--shadow-sm); }
.kf-btn--accent:hover:not(:disabled) { background: var(--accent-hover); }
`;
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-button-css")) return;
    const s = document.createElement("style");
    s.id = "kf-button-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Primary KairuFocus button. Use for any clickable action — Von Restorff
 * gives `primary`/`accent` their visual weight; keep one per view.
 */
function Button({
  variant = "primary",
  size = "md",
  icon = null,
  iconRight = null,
  as = "button",
  children,
  className = "",
  ...rest
}) {
  useStyle();
  const Tag = as;
  return /*#__PURE__*/React.createElement(Tag, _extends({
    className: `kf-btn kf-btn--${variant} kf-btn--${size} ${className}`
  }, rest), icon && /*#__PURE__*/React.createElement("span", {
    "aria-hidden": "true",
    style: {
      display: "inline-flex"
    }
  }, icon), children, iconRight && /*#__PURE__*/React.createElement("span", {
    "aria-hidden": "true",
    style: {
      display: "inline-flex"
    }
  }, iconRight));
}
Object.assign(__ds_scope, { Button });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/buttons/Button.jsx", error: String((e && e.message) || e) }); }

// components/buttons/IconButton.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.kf-iconbtn {
  display: inline-flex; align-items: center; justify-content: center;
  width: var(--target-min); height: var(--target-min);
  border: 1px solid transparent; border-radius: var(--radius-md);
  background: transparent; color: var(--text-secondary);
  cursor: pointer; flex-shrink: 0;
  transition: background-color var(--duration-fast) var(--ease-standard),
              color var(--duration-fast) var(--ease-standard),
              border-color var(--duration-fast) var(--ease-standard);
}
.kf-iconbtn:hover:not(:disabled) { background: var(--surface-hover); color: var(--text-primary); }
.kf-iconbtn:focus-visible { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
.kf-iconbtn:disabled { opacity: 0.5; cursor: not-allowed; }
.kf-iconbtn--sm { width: 36px; height: 36px; }
.kf-iconbtn--outline { border-color: var(--border-default); }
.kf-iconbtn--primary:hover:not(:disabled) { background: var(--primary-subtle); color: var(--primary); border-color: var(--primary-border); }
.kf-iconbtn--success:hover:not(:disabled) { background: var(--success-subtle); color: var(--success); border-color: var(--success); }
.kf-iconbtn--danger:hover:not(:disabled) { background: var(--danger-subtle); color: var(--danger); border-color: var(--danger); }
`;
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-iconbtn-css")) return;
    const s = document.createElement("style");
    s.id = "kf-iconbtn-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Icon-only button. ALWAYS pass `label` — it becomes the accessible name
 * (aria-label) and the native tooltip. 44px target by default (Fitts/AA).
 */
function IconButton({
  label,
  tone = "default",
  size = "md",
  outline = false,
  children,
  className = "",
  ...rest
}) {
  useStyle();
  const cls = ["kf-iconbtn", `kf-iconbtn--${tone}`, size === "sm" ? "kf-iconbtn--sm" : "", outline ? "kf-iconbtn--outline" : "", className].filter(Boolean).join(" ");
  return /*#__PURE__*/React.createElement("button", _extends({
    type: "button",
    className: cls,
    "aria-label": label,
    title: label
  }, rest), /*#__PURE__*/React.createElement("span", {
    "aria-hidden": "true",
    style: {
      display: "inline-flex",
      lineHeight: 0
    }
  }, children));
}
Object.assign(__ds_scope, { IconButton });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/buttons/IconButton.jsx", error: String((e && e.message) || e) }); }

// components/data-display/Avatar.jsx
try { (() => {
const CSS = `
.kf-avatar {
  display: inline-flex; align-items: center; justify-content: center;
  border-radius: var(--radius-pill); overflow: hidden; flex-shrink: 0;
  background: var(--primary); color: var(--on-primary);
  font-family: var(--font-sans); font-weight: var(--weight-bold);
  border: 1px solid var(--border-subtle);
}
.kf-avatar img { width: 100%; height: 100%; object-fit: cover; display: block; }
`;
const SIZES = {
  sm: 28,
  md: 36,
  lg: 48
};
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-avatar-css")) return;
    const s = document.createElement("style");
    s.id = "kf-avatar-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}
function initials(name) {
  if (!name) return "?";
  return name.trim().split(/\s+/).slice(0, 2).map(w => w[0]).join("").toUpperCase();
}

/** User avatar — GitHub photo if `src`, else initials on brand blue. */
function Avatar({
  src,
  name,
  size = "md"
}) {
  useStyle();
  const px = SIZES[size] || size;
  return /*#__PURE__*/React.createElement("span", {
    className: "kf-avatar",
    style: {
      width: px,
      height: px,
      fontSize: px * 0.42
    },
    title: name
  }, src ? /*#__PURE__*/React.createElement("img", {
    src: src,
    alt: name || ""
  }) : initials(name));
}
Object.assign(__ds_scope, { Avatar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data-display/Avatar.jsx", error: String((e && e.message) || e) }); }

// components/data-display/Badge.jsx
try { (() => {
const CSS = `
.kf-badge {
  display: inline-flex; align-items: center; gap: 0.3rem;
  font-family: var(--font-sans); font-size: var(--text-xs); font-weight: var(--weight-bold);
  line-height: 1; padding: 0.3rem 0.55rem; border-radius: var(--radius-pill);
  white-space: nowrap; border: 1px solid transparent;
}
.kf-badge__dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
.kf-badge--todo       { background: var(--surface-sunken); color: var(--text-secondary); border-color: var(--border-subtle); }
.kf-badge--inprogress { background: var(--warning-subtle); color: var(--warning-text); }
.kf-badge--done       { background: var(--success-subtle); color: var(--success-text); }
.kf-badge--info       { background: var(--info-subtle); color: var(--info-text); }
.kf-badge--danger     { background: var(--danger-subtle); color: var(--danger-text); }
.kf-badge--solid      { background: var(--primary); color: var(--on-primary); }
`;
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-badge-css")) return;
    const s = document.createElement("style");
    s.id = "kf-badge-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}
const STATUS = {
  Todo: {
    variant: "todo",
    label: "Todo"
  },
  Pending: {
    variant: "todo",
    label: "En attente"
  },
  InProgress: {
    variant: "inprogress",
    label: "En cours"
  },
  Done: {
    variant: "done",
    label: "Terminé"
  }
};

/**
 * Status / category badge. Pass `status` for the app's task states
 * (Todo / InProgress / Done) or `variant` + children for a custom label.
 */
function Badge({
  status,
  variant,
  dot = false,
  children,
  className = ""
}) {
  useStyle();
  const meta = status ? STATUS[status] : null;
  const v = variant || (meta ? meta.variant : "todo");
  const text = children != null ? children : meta ? meta.label : status;
  return /*#__PURE__*/React.createElement("span", {
    className: `kf-badge kf-badge--${v} ${className}`
  }, dot && /*#__PURE__*/React.createElement("span", {
    className: "kf-badge__dot",
    "aria-hidden": "true"
  }), text);
}
Object.assign(__ds_scope, { Badge });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data-display/Badge.jsx", error: String((e && e.message) || e) }); }

// components/data-display/Card.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.kf-card {
  background: var(--surface-card); border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg); box-shadow: var(--shadow-sm);
  padding: var(--space-6); color: var(--text-primary);
}
.kf-card--flush { padding: 0; }
.kf-card--interactive { cursor: pointer; transition: box-shadow var(--duration-base) var(--ease-standard), transform var(--duration-base) var(--ease-standard), border-color var(--duration-base) var(--ease-standard); }
.kf-card--interactive:hover { box-shadow: var(--shadow-md); transform: translateY(-2px); }
.kf-card--interactive:focus-visible { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
.kf-card--active { border-color: var(--success); background: var(--success-subtle); }
`;
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-card-css")) return;
    const s = document.createElement("style");
    s.id = "kf-card-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/** Surface container. `interactive` adds the hover-lift affordance (Fitts). */
function Card({
  interactive = false,
  active = false,
  flush = false,
  as = "div",
  children,
  className = "",
  ...rest
}) {
  useStyle();
  const Tag = as;
  const cls = ["kf-card", interactive ? "kf-card--interactive" : "", active ? "kf-card--active" : "", flush ? "kf-card--flush" : "", className].filter(Boolean).join(" ");
  return /*#__PURE__*/React.createElement(Tag, _extends({
    className: cls
  }, rest), children);
}
Object.assign(__ds_scope, { Card });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data-display/Card.jsx", error: String((e && e.message) || e) }); }

// components/data-display/StatCard.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.kf-stat {
  display: flex; align-items: center; gap: var(--space-4);
  background: var(--surface-card); border: 1px solid var(--border-subtle);
  border-radius: var(--radius-lg); padding: var(--space-5);
  cursor: pointer; text-decoration: none; color: var(--text-primary);
  transition: box-shadow var(--duration-base) var(--ease-standard), transform var(--duration-base) var(--ease-standard), border-color var(--duration-base) var(--ease-standard);
}
.kf-stat:hover { box-shadow: var(--shadow-md); transform: translateY(-2px); }
.kf-stat:focus-visible { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
.kf-stat--active { border-color: var(--success); background: var(--success-subtle); }
.kf-stat__icon { width: 44px; height: 44px; flex-shrink: 0; border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center; font-size: 1.5rem; background: var(--surface-sunken); }
.kf-stat__text { display: flex; flex-direction: column; gap: 0.25rem; min-width: 0; }
.kf-stat__value { display: block; font-size: var(--text-xl); font-weight: var(--weight-bold); line-height: 1; }
.kf-stat__value--active { color: var(--success); }
.kf-stat__label { display: block; font-size: var(--text-sm); color: var(--text-muted); }
`;
const ICON_TINT = {
  primary: "var(--primary-subtle)",
  success: "var(--success-subtle)",
  warning: "var(--warning-subtle)",
  info: "var(--info-subtle)",
  task: "var(--task-subtle)"
};
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-stat-css")) return;
    const s = document.createElement("style");
    s.id = "kf-stat-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Dashboard stat tile (Miller/chunking). Clickable, with an icon, a big
 * value and a muted label. `active` highlights a live state (e.g. running sprint).
 */
function StatCard({
  icon,
  value,
  label,
  tone = "primary",
  active = false,
  as = "a",
  ...rest
}) {
  useStyle();
  const Tag = as;
  return /*#__PURE__*/React.createElement(Tag, _extends({
    className: `kf-stat ${active ? "kf-stat--active" : ""}`
  }, rest), /*#__PURE__*/React.createElement("span", {
    className: "kf-stat__icon",
    style: {
      background: ICON_TINT[tone]
    },
    "aria-hidden": "true"
  }, icon), /*#__PURE__*/React.createElement("span", {
    className: "kf-stat__text"
  }, /*#__PURE__*/React.createElement("span", {
    className: `kf-stat__value ${active ? "kf-stat__value--active" : ""}`
  }, value), /*#__PURE__*/React.createElement("span", {
    className: "kf-stat__label"
  }, label)));
}
Object.assign(__ds_scope, { StatCard });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data-display/StatCard.jsx", error: String((e && e.message) || e) }); }

// components/data-display/Tag.jsx
try { (() => {
const CSS = `
.kf-tag {
  display: inline-flex; align-items: center; gap: 0.3rem;
  font-family: var(--font-sans); font-size: var(--text-xs); font-weight: var(--weight-semibold);
  line-height: 1; padding: 0.28rem 0.5rem; border-radius: var(--radius-xs);
  color: #fff; white-space: nowrap;
}
.kf-tag__hash { opacity: 0.7; }
.kf-tag__x {
  display: inline-flex; align-items: center; justify-content: center;
  margin-left: 0.1rem; width: 14px; height: 14px; border-radius: 50%;
  background: rgba(255,255,255,0.25); cursor: pointer; border: none; color: #fff;
  font-size: 10px; line-height: 1; padding: 0;
}
.kf-tag__x:hover { background: rgba(255,255,255,0.45); }
`;

/* Palette mirrors the app's TagColors.cs (deterministic djb2 hash). */
const PALETTE = ["var(--primary)", "var(--success)", "var(--danger)", "var(--warning)", "var(--info)", "var(--text-muted)", "var(--task)", "var(--kf-orange-500)", "var(--kf-teal-500)", "var(--kf-pink-500)"];
const DARK_TEXT = new Set([3, 4]); // warning, info read better with dark text

function hashColor(tag) {
  const n = (tag || "").toLowerCase();
  let hash = 5381 >>> 0;
  for (let i = 0; i < n.length; i++) hash = (hash * 33 >>> 0 ^ n.charCodeAt(i)) >>> 0;
  return hash % PALETTE.length;
}
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-tag-css")) return;
    const s = document.createElement("style");
    s.id = "kf-tag-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Tag chip with a deterministic, hash-derived color (same tag → same color,
 * matching the app). Optional `#` prefix and removable `x`.
 */
function Tag({
  children,
  hash = false,
  onRemove,
  color
}) {
  useStyle();
  const text = String(children ?? "");
  const idx = hashColor(text);
  const bg = color || PALETTE[idx];
  const dark = !color && DARK_TEXT.has(idx);
  return /*#__PURE__*/React.createElement("span", {
    className: "kf-tag",
    style: {
      background: bg,
      color: dark ? "#1a1a1a" : "#fff"
    }
  }, hash && /*#__PURE__*/React.createElement("span", {
    className: "kf-tag__hash"
  }, "#"), text, onRemove && /*#__PURE__*/React.createElement("button", {
    type: "button",
    className: "kf-tag__x",
    "aria-label": `Retirer ${text}`,
    onClick: onRemove
  }, "\xD7"));
}
Object.assign(__ds_scope, { Tag });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data-display/Tag.jsx", error: String((e && e.message) || e) }); }

// components/feedback/Alert.jsx
try { (() => {
const CSS = `
.kf-alert {
  display: flex; gap: 0.65rem; align-items: flex-start;
  font-family: var(--font-sans); font-size: var(--text-sm); line-height: var(--leading-normal);
  border: 1px solid transparent; border-radius: var(--radius-md);
  padding: 0.75rem 0.9rem;
}
.kf-alert__icon { flex-shrink: 0; line-height: 1.2; font-size: 1rem; }
.kf-alert__body { flex: 1; }
.kf-alert__body strong { font-weight: var(--weight-bold); }
.kf-alert--success { background: var(--success-subtle); color: var(--success-text); border-color: color-mix(in srgb, var(--success) 30%, transparent); }
.kf-alert--danger  { background: var(--danger-subtle);  color: var(--danger-text);  border-color: color-mix(in srgb, var(--danger) 30%, transparent); }
.kf-alert--warning { background: var(--warning-subtle); color: var(--warning-text); border-color: color-mix(in srgb, var(--warning) 35%, transparent); }
.kf-alert--info    { background: var(--info-subtle);    color: var(--info-text);    border-color: color-mix(in srgb, var(--info) 30%, transparent); }
`;
const ICONS = {
  success: "✓",
  danger: "⚠",
  warning: "ℹ",
  info: "ℹ"
};
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-alert-css")) return;
    const s = document.createElement("style");
    s.id = "kf-alert-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/** Inline feedback banner (success / danger / warning / info). */
function Alert({
  tone = "info",
  icon,
  children,
  className = ""
}) {
  useStyle();
  return /*#__PURE__*/React.createElement("div", {
    className: `kf-alert kf-alert--${tone} ${className}`,
    role: tone === "danger" ? "alert" : "status"
  }, /*#__PURE__*/React.createElement("span", {
    className: "kf-alert__icon",
    "aria-hidden": "true"
  }, icon ?? ICONS[tone]), /*#__PURE__*/React.createElement("div", {
    className: "kf-alert__body"
  }, children));
}
Object.assign(__ds_scope, { Alert });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/Alert.jsx", error: String((e && e.message) || e) }); }

// components/feedback/ProgressRing.jsx
try { (() => {
const STATE_COLOR = {
  sprint: "var(--pomo-sprint)",
  shortBreak: "var(--pomo-short-break)",
  longBreak: "var(--pomo-long-break)",
  interrupted: "var(--pomo-interrupted)",
  done: "var(--success)",
  idle: "var(--text-muted)"
};

/**
 * Pomodoro progress ring. `progress` is 0–1 (remaining fraction). The arc
 * uses the session-state color and animates smoothly (Goal-Gradient).
 */
function ProgressRing({
  progress = 1,
  state = "sprint",
  size = 220,
  stroke = 14,
  label,
  sublabel
}) {
  const r = (size - stroke) / 2;
  const c = 2 * Math.PI * r;
  const clamped = Math.max(0, Math.min(1, progress));
  const offset = c * (1 - clamped);
  const color = STATE_COLOR[state] || STATE_COLOR.sprint;
  return /*#__PURE__*/React.createElement("svg", {
    width: size,
    height: size,
    viewBox: `0 0 ${size} ${size}`,
    role: "img",
    "aria-label": label ? `${label} ${sublabel || ""}`.trim() : "Minuteur"
  }, /*#__PURE__*/React.createElement("circle", {
    cx: size / 2,
    cy: size / 2,
    r: r,
    fill: "none",
    stroke: "var(--pomo-track)",
    strokeWidth: stroke
  }), /*#__PURE__*/React.createElement("circle", {
    cx: size / 2,
    cy: size / 2,
    r: r,
    fill: "none",
    stroke: color,
    strokeWidth: stroke,
    strokeLinecap: "round",
    strokeDasharray: c,
    strokeDashoffset: offset,
    transform: `rotate(-90 ${size / 2} ${size / 2})`,
    style: {
      transition: "stroke-dashoffset 0.8s linear, stroke 0.4s ease"
    }
  }), label != null && /*#__PURE__*/React.createElement("text", {
    x: "50%",
    y: "48%",
    textAnchor: "middle",
    dominantBaseline: "middle",
    fontFamily: "var(--font-mono)",
    fontWeight: "700",
    fontSize: size * 0.19,
    fill: "var(--text-primary)"
  }, label), sublabel != null && /*#__PURE__*/React.createElement("text", {
    x: "50%",
    y: "66%",
    textAnchor: "middle",
    fontFamily: "var(--font-sans)",
    fontSize: size * 0.07,
    fill: "var(--text-muted)"
  }, sublabel));
}
Object.assign(__ds_scope, { ProgressRing });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/ProgressRing.jsx", error: String((e && e.message) || e) }); }

// components/feedback/Skeleton.jsx
try { (() => {
const CSS = `
@keyframes kf-skeleton-pulse { 0%,100% { opacity: 1; } 50% { opacity: 0.45; } }
.kf-skeleton {
  display: block; background: var(--surface-sunken); border-radius: var(--radius-sm);
  animation: kf-skeleton-pulse 1.2s var(--ease-in-out) infinite;
}
@media (prefers-reduced-motion: reduce) { .kf-skeleton { animation: none; } }
`;
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-skeleton-css")) return;
    const s = document.createElement("style");
    s.id = "kf-skeleton-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Skeleton placeholder — replaces "Chargement…" text while data loads
 * (Doherty < 400ms). Compose several to mimic the final layout.
 */
function Skeleton({
  width = "100%",
  height = 16,
  radius,
  circle = false,
  style = {},
  className = ""
}) {
  useStyle();
  return /*#__PURE__*/React.createElement("span", {
    className: `kf-skeleton ${className}`,
    "aria-hidden": "true",
    style: {
      width,
      height: circle ? width : height,
      borderRadius: circle ? "50%" : radius || undefined,
      ...style
    }
  });
}
Object.assign(__ds_scope, { Skeleton });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/Skeleton.jsx", error: String((e && e.message) || e) }); }

// components/forms/Checkbox.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.kf-check { display: inline-flex; align-items: flex-start; gap: 0.6rem; cursor: pointer; font-family: var(--font-sans); user-select: none; }
.kf-check input { position: absolute; opacity: 0; width: 0; height: 0; }
.kf-check__box {
  flex-shrink: 0; width: 22px; height: 22px; margin-top: 1px;
  border: 2px solid var(--border-strong); border-radius: var(--radius-xs);
  background: var(--surface-card); color: #fff;
  display: inline-flex; align-items: center; justify-content: center;
  transition: background-color var(--duration-fast) var(--ease-standard),
              border-color var(--duration-fast) var(--ease-standard);
}
/* generous hit area around the 22px box (Fitts) */
.kf-check__hit { display: inline-flex; align-items: center; justify-content: center; min-width: var(--target-min); min-height: var(--target-min); margin: calc(var(--target-min)/-2 + 11px) 0; }
.kf-check__box svg { opacity: 0; transform: scale(0.6); transition: all var(--duration-fast) var(--ease-out); }
.kf-check input:checked + .kf-check__hit .kf-check__box { background: var(--success); border-color: var(--success); }
.kf-check input:checked + .kf-check__hit .kf-check__box svg { opacity: 1; transform: scale(1); }
.kf-check input:focus-visible + .kf-check__hit .kf-check__box { outline: 2px solid var(--focus-ring); outline-offset: 2px; }
.kf-check__label { font-size: var(--text-base); color: var(--text-primary); padding-top: 1px; }
.kf-check input:checked ~ .kf-check__label { color: var(--text-muted); text-decoration: line-through; }
`;
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-check-css")) return;
    const s = document.createElement("style");
    s.id = "kf-check-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/**
 * Conventional task checkbox (Jakob's Law) — checking strikes through the
 * label. The visible box is 22px but the clickable area is 44px (Fitts).
 */
function Checkbox({
  label,
  checked,
  onChange,
  id,
  ...rest
}) {
  useStyle();
  const fid = id || React.useId();
  return /*#__PURE__*/React.createElement("label", {
    className: "kf-check",
    htmlFor: fid
  }, /*#__PURE__*/React.createElement("input", _extends({
    id: fid,
    type: "checkbox",
    checked: checked,
    onChange: onChange
  }, rest)), /*#__PURE__*/React.createElement("span", {
    className: "kf-check__hit"
  }, /*#__PURE__*/React.createElement("span", {
    className: "kf-check__box"
  }, /*#__PURE__*/React.createElement("svg", {
    width: "13",
    height: "13",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "3.2",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("polyline", {
    points: "20 6 9 17 4 12"
  })))), label && /*#__PURE__*/React.createElement("span", {
    className: "kf-check__label"
  }, label));
}
Object.assign(__ds_scope, { Checkbox });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Checkbox.jsx", error: String((e && e.message) || e) }); }

// components/forms/Input.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.kf-field { display: flex; flex-direction: column; gap: 0.35rem; font-family: var(--font-sans); }
.kf-field__label { font-size: var(--text-sm); font-weight: var(--weight-semibold); color: var(--text-secondary); }
.kf-field__hint { font-size: var(--text-xs); color: var(--text-muted); }
.kf-field__error { font-size: var(--text-xs); color: var(--danger); font-weight: var(--weight-medium); }
.kf-input {
  font-family: var(--font-sans); font-size: var(--text-base); color: var(--text-primary);
  background: var(--surface-card); border: 1px solid var(--border-default);
  border-radius: var(--radius-sm); padding: 0 0.85rem; height: var(--target-min); width: 100%;
  transition: border-color var(--duration-fast) var(--ease-standard),
              box-shadow var(--duration-fast) var(--ease-standard);
}
.kf-input::placeholder { color: var(--text-muted); }
.kf-input:hover:not(:disabled) { border-color: var(--border-strong); }
.kf-input:focus { outline: none; border-color: var(--primary); box-shadow: 0 0 0 3px var(--primary-subtle); }
.kf-input:disabled { opacity: 0.6; cursor: not-allowed; background: var(--surface-sunken); }
.kf-input--error { border-color: var(--danger); }
.kf-input--error:focus { box-shadow: 0 0 0 3px var(--danger-subtle); }
`;
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-input-css")) return;
    const s = document.createElement("style");
    s.id = "kf-input-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/** Single-line text field with optional label, hint and error. */
function Input({
  label,
  hint,
  error,
  id,
  className = "",
  ...rest
}) {
  useStyle();
  const fid = id || React.useId();
  return /*#__PURE__*/React.createElement("div", {
    className: "kf-field"
  }, label && /*#__PURE__*/React.createElement("label", {
    className: "kf-field__label",
    htmlFor: fid
  }, label), /*#__PURE__*/React.createElement("input", _extends({
    id: fid,
    className: `kf-input ${error ? "kf-input--error" : ""} ${className}`,
    "aria-invalid": error ? "true" : undefined
  }, rest)), error ? /*#__PURE__*/React.createElement("span", {
    className: "kf-field__error"
  }, error) : hint ? /*#__PURE__*/React.createElement("span", {
    className: "kf-field__hint"
  }, hint) : null);
}
Object.assign(__ds_scope, { Input });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Input.jsx", error: String((e && e.message) || e) }); }

// components/forms/Select.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.kf-select-wrap { position: relative; display: inline-flex; flex-direction: column; gap: 0.35rem; width: 100%; font-family: var(--font-sans); }
.kf-select {
  appearance: none; -webkit-appearance: none;
  font-family: var(--font-sans); font-size: var(--text-base); color: var(--text-primary);
  background: var(--surface-card); border: 1px solid var(--border-default);
  border-radius: var(--radius-sm); padding: 0 2.2rem 0 0.85rem; height: var(--target-min); width: 100%;
  cursor: pointer;
  transition: border-color var(--duration-fast) var(--ease-standard),
              box-shadow var(--duration-fast) var(--ease-standard);
}
.kf-select:hover { border-color: var(--border-strong); }
.kf-select:focus { outline: none; border-color: var(--primary); box-shadow: 0 0 0 3px var(--primary-subtle); }
.kf-select__chev { position: absolute; right: 0.8rem; bottom: 0; height: var(--target-min); display: flex; align-items: center; pointer-events: none; color: var(--text-muted); }
`;
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-select-css")) return;
    const s = document.createElement("style");
    s.id = "kf-select-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/** Native select, restyled to the token system with a custom chevron. */
function Select({
  label,
  id,
  children,
  options,
  className = "",
  ...rest
}) {
  useStyle();
  const fid = id || React.useId();
  return /*#__PURE__*/React.createElement("div", {
    className: "kf-select-wrap"
  }, label && /*#__PURE__*/React.createElement("label", {
    className: "kf-field__label",
    htmlFor: fid
  }, label), /*#__PURE__*/React.createElement("div", {
    style: {
      position: "relative"
    }
  }, /*#__PURE__*/React.createElement("select", _extends({
    id: fid,
    className: `kf-select ${className}`
  }, rest), options ? options.map(o => /*#__PURE__*/React.createElement("option", {
    key: o.value,
    value: o.value
  }, o.label)) : children), /*#__PURE__*/React.createElement("span", {
    className: "kf-select__chev",
    "aria-hidden": "true"
  }, /*#__PURE__*/React.createElement("svg", {
    width: "14",
    height: "14",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "2.4",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("polyline", {
    points: "6 9 12 15 18 9"
  })))));
}
Object.assign(__ds_scope, { Select });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Select.jsx", error: String((e && e.message) || e) }); }

// components/forms/Textarea.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const CSS = `
.kf-textarea {
  font-family: var(--font-sans); font-size: var(--text-base); color: var(--text-primary);
  background: var(--surface-card); border: 1px solid var(--border-default);
  border-radius: var(--radius-sm); padding: 0.6rem 0.85rem; width: 100%; resize: vertical;
  line-height: var(--leading-normal); min-height: 64px;
  transition: border-color var(--duration-fast) var(--ease-standard),
              box-shadow var(--duration-fast) var(--ease-standard);
}
.kf-textarea::placeholder { color: var(--text-muted); }
.kf-textarea:hover:not(:disabled) { border-color: var(--border-strong); }
.kf-textarea:focus { outline: none; border-color: var(--primary); box-shadow: 0 0 0 3px var(--primary-subtle); }
.kf-textarea-wrap { position: relative; }
.kf-textarea__count {
  position: absolute; right: 0.6rem; bottom: 0.45rem; font-size: var(--text-xs);
  font-family: var(--font-mono); color: var(--text-muted); pointer-events: none;
}
.kf-textarea__count--over { color: var(--danger); }
`;
function useStyle() {
  React.useEffect(() => {
    if (document.getElementById("kf-textarea-css")) return;
    const s = document.createElement("style");
    s.id = "kf-textarea-css";
    s.textContent = CSS;
    document.head.appendChild(s);
  }, []);
}

/** Multi-line text area with an optional live character counter (Postel/limits). */
function Textarea({
  label,
  id,
  maxLength,
  value,
  className = "",
  ...rest
}) {
  useStyle();
  const fid = id || React.useId();
  const len = typeof value === "string" ? value.length : 0;
  const over = maxLength != null && len > maxLength;
  return /*#__PURE__*/React.createElement("div", {
    className: "kf-field"
  }, label && /*#__PURE__*/React.createElement("label", {
    className: "kf-field__label",
    htmlFor: fid
  }, label), /*#__PURE__*/React.createElement("div", {
    className: "kf-textarea-wrap"
  }, /*#__PURE__*/React.createElement("textarea", _extends({
    id: fid,
    className: `kf-textarea ${className}`,
    value: value,
    maxLength: maxLength
  }, rest)), maxLength != null && /*#__PURE__*/React.createElement("span", {
    className: `kf-textarea__count ${over ? "kf-textarea__count--over" : ""}`
  }, len, "/", maxLength)));
}
Object.assign(__ds_scope, { Textarea });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Textarea.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/AppShell.jsx
try { (() => {
(() => {
  // App shell — persistent 64px sidebar rail (Jakob) + top row + content.
  const {
    Avatar
  } = window.KairuFocusDesignSystem_0d3740;
  const {
    Home,
    Logout
  } = window.KFIcons;
  const SHELL_CSS = `
.kf-app { display: flex; min-height: 100vh; background: var(--surface-base); }
.kf-side {
  width: var(--sidebar-width); flex-shrink: 0; position: sticky; top: 0; height: 100vh;
  background: var(--brand-sidebar); display: flex; flex-direction: column; align-items: center;
}
.kf-side__brand {
  height: var(--topbar-height); width: 100%; display: flex; align-items: center; justify-content: center;
  background: rgba(0,0,0,0.4); color: #fff; font-size: 1.4rem; font-weight: 700; letter-spacing: .05em;
}
.kf-side__nav { display: flex; flex-direction: column; align-items: center; gap: .35rem; padding-top: .75rem; flex: 1; width: 100%; }
.kf-navicon {
  position: relative; width: 3rem; height: 3rem; border: none; background: transparent;
  color: #d7d7d7; border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center;
  font-size: 1.4rem; cursor: pointer; transition: background-color var(--duration-fast) var(--ease-standard), color var(--duration-fast) var(--ease-standard);
}
.kf-navicon:hover { background: rgba(255,255,255,0.15); color: #fff; }
.kf-navicon.is-active { background: rgba(255,255,255,0.37); color: #fff; }
.kf-navicon:focus-visible { outline: 2px solid #fff; outline-offset: 2px; }
.kf-navicon__tip {
  position: absolute; left: calc(100% + 12px); top: 50%; transform: translateY(-50%);
  background: rgba(0,0,0,.85); color: #fff; padding: .3rem .65rem; border-radius: 6px;
  font-size: .8rem; white-space: nowrap; pointer-events: none; opacity: 0;
  transition: opacity var(--duration-fast) var(--ease-standard); z-index: 50;
}
.kf-navicon:hover .kf-navicon__tip { opacity: 1; }
.kf-side__settings { margin-top: auto; border-top: 1px solid rgba(255,255,255,.1); padding-top: .5rem; width: 100%; display: flex; justify-content: center; }
.kf-side__logout { border-top: 1px solid rgba(255,255,255,.1); padding: .5rem 0; width: 100%; display: flex; justify-content: center; }
.kf-main { flex: 1; display: flex; flex-direction: column; min-width: 0; }
.kf-top {
  height: var(--topbar-height); position: sticky; top: 0; z-index: 10;
  background: var(--surface-card); border-bottom: 1px solid var(--border-subtle);
  display: flex; align-items: center; justify-content: flex-end; gap: 1rem; padding: 0 1.5rem;
}
.kf-top__user { display: flex; align-items: center; gap: .6rem; font-size: var(--text-sm); font-weight: 600; color: var(--text-secondary); }
.kf-theme {
  width: 40px; height: 40px; border-radius: var(--radius-md); border: 1px solid var(--border-subtle);
  background: var(--surface-card); color: var(--text-secondary); cursor: pointer; font-size: 1.1rem;
  display: flex; align-items: center; justify-content: center;
}
.kf-theme:hover { background: var(--surface-hover); }
.kf-content { padding: 1.5rem 2rem; flex: 1; }
`;
  const NAV = [{
    id: "dashboard",
    icon: /*#__PURE__*/React.createElement(Home, {
      size: 20
    }),
    label: "Tableau de bord"
  }, {
    id: "tasks",
    icon: /*#__PURE__*/React.createElement("span", null, "\u2611\uFE0F"),
    label: "Tâches"
  }, {
    id: "pomodoro",
    icon: /*#__PURE__*/React.createElement("span", null, "\uD83C\uDF45"),
    label: "Pomodoro"
  }, {
    id: "journal",
    icon: /*#__PURE__*/React.createElement("span", null, "\uD83D\uDCD6"),
    label: "Journal"
  }];
  function NavIcon({
    item,
    active,
    onNavigate
  }) {
    return /*#__PURE__*/React.createElement("button", {
      className: `kf-navicon ${active === item.id ? "is-active" : ""}`,
      "aria-label": item.label,
      "aria-current": active === item.id ? "page" : undefined,
      onClick: () => onNavigate(item.id)
    }, /*#__PURE__*/React.createElement("span", {
      "aria-hidden": "true",
      style: {
        display: "inline-flex"
      }
    }, item.icon), /*#__PURE__*/React.createElement("span", {
      className: "kf-navicon__tip"
    }, item.label));
  }
  function AppShell({
    active,
    onNavigate,
    onLogout,
    theme,
    onToggleTheme,
    user,
    children
  }) {
    React.useEffect(() => {
      if (document.getElementById("kf-shell-css")) return;
      const s = document.createElement("style");
      s.id = "kf-shell-css";
      s.textContent = SHELL_CSS;
      document.head.appendChild(s);
    }, []);
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-app"
    }, /*#__PURE__*/React.createElement("aside", {
      className: "kf-side"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-side__brand"
    }, "K"), /*#__PURE__*/React.createElement("nav", {
      className: "kf-side__nav"
    }, NAV.map(it => /*#__PURE__*/React.createElement(NavIcon, {
      key: it.id,
      item: it,
      active: active,
      onNavigate: onNavigate
    })), /*#__PURE__*/React.createElement("div", {
      className: "kf-side__settings"
    }, /*#__PURE__*/React.createElement(NavIcon, {
      item: {
        id: "settings",
        icon: /*#__PURE__*/React.createElement("span", null, "\u2699\uFE0F"),
        label: "Paramètres"
      },
      active: active,
      onNavigate: onNavigate
    })), /*#__PURE__*/React.createElement("div", {
      className: "kf-side__logout"
    }, /*#__PURE__*/React.createElement("button", {
      className: "kf-navicon",
      "aria-label": "D\xE9connexion",
      onClick: onLogout
    }, /*#__PURE__*/React.createElement(Logout, {
      size: 20
    }), /*#__PURE__*/React.createElement("span", {
      className: "kf-navicon__tip"
    }, "D\xE9connexion"))))), /*#__PURE__*/React.createElement("div", {
      className: "kf-main"
    }, /*#__PURE__*/React.createElement("header", {
      className: "kf-top"
    }, /*#__PURE__*/React.createElement("button", {
      className: "kf-theme",
      "aria-label": theme === "dark" ? "Passer en clair" : "Passer en sombre",
      onClick: onToggleTheme
    }, theme === "dark" ? "☀️" : "🌙"), /*#__PURE__*/React.createElement("div", {
      className: "kf-top__user"
    }, /*#__PURE__*/React.createElement("span", null, user.name), /*#__PURE__*/React.createElement(Avatar, {
      name: user.name,
      size: "sm"
    }))), /*#__PURE__*/React.createElement("main", {
      className: "kf-content"
    }, children)));
  }
  window.AppShell = AppShell;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/AppShell.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/DashboardScreen.jsx
try { (() => {
(() => {
  // Dashboard — Zeigarnik resume block, Goal-Gradient day progress,
  // secondary grouped stats, reduced quick links (Hick), skeleton on load.
  const {
    Card,
    StatCard,
    Badge,
    Button,
    Tag,
    Skeleton
  } = window.KairuFocusDesignSystem_0d3740;
  const DASH_CSS = `
.kf-dash { max-width: var(--container-lg); }
.kf-dash__head { margin-bottom: 1.75rem; }
.kf-dash__greet { font-size: var(--text-2xl); font-weight: 700; margin: 0; color: var(--text-primary); }
.kf-dash__date { color: var(--text-muted); margin: .25rem 0 0; text-transform: capitalize; }
.kf-eyebrow { font-size: var(--text-xs); font-weight: 700; text-transform: uppercase; letter-spacing: var(--tracking-wide); color: var(--text-muted); margin: 0 0 .85rem; }
.kf-resume { padding: 0; overflow: hidden; }
.kf-resume__in { display: flex; align-items: center; gap: 1.25rem; padding: 1.25rem 1.5rem; }
.kf-resume__icon { width: 52px; height: 52px; border-radius: var(--radius-md); background: var(--warning-subtle); display: flex; align-items: center; justify-content: center; font-size: 1.7rem; flex-shrink: 0; }
.kf-resume__body { flex: 1; min-width: 0; }
.kf-resume__title { font-size: var(--text-md); font-weight: 700; color: var(--text-primary); margin: 0 0 .2rem; }
.kf-resume__meta { font-size: var(--text-sm); color: var(--text-muted); display: flex; gap: .4rem; align-items: center; flex-wrap: wrap; }
.kf-progress { display: flex; flex-direction: column; gap: .5rem; margin: 1.5rem 0 2rem; }
.kf-progress__row { display: flex; justify-content: space-between; align-items: baseline; }
.kf-progress__label { font-size: var(--text-sm); font-weight: 600; color: var(--text-secondary); }
.kf-progress__count { font-family: var(--font-mono); font-size: var(--text-sm); color: var(--text-muted); }
.kf-progress__track { height: 10px; border-radius: var(--radius-pill); background: var(--surface-sunken); overflow: hidden; }
.kf-progress__fill { height: 100%; border-radius: var(--radius-pill); background: linear-gradient(90deg, var(--primary), var(--success)); transition: width var(--duration-slow) var(--ease-out); }
.kf-focuscard { padding: 1.4rem 1.6rem; margin-bottom: 1.5rem; }
.kf-focushero__top { display: flex; align-items: baseline; justify-content: space-between; margin-bottom: .85rem; }
.kf-focushero__lbl { font-size: var(--text-sm); font-weight: 600; color: var(--text-secondary); }
.kf-focushero__count { font-family: var(--font-mono); font-size: var(--text-lg); font-weight: 700; color: var(--text-primary); font-variant-numeric: tabular-nums; }
.kf-focushero__hint { font-size: var(--text-sm); color: var(--text-muted); margin-top: .8rem; }
.kf-dots { display: flex; gap: 8px; flex-wrap: wrap; }
.kf-dot { width: 22px; height: 22px; border-radius: 50%; transition: background-color var(--duration-base) var(--ease-out), box-shadow var(--duration-base) var(--ease-out); }
.kf-dot--on { background: var(--pomo-sprint); box-shadow: 0 0 0 3px var(--primary-subtle); }
.kf-dot--off { background: var(--surface-sunken); border: 1px solid var(--border-default); }
.kf-focusmeta { display: flex; gap: 2rem; margin-top: 1.3rem; padding-top: 1.1rem; border-top: 1px solid var(--border-subtle); }
.kf-fi { display: flex; align-items: center; gap: .8rem; }
.kf-fi__icon { width: 44px; height: 44px; border-radius: var(--radius-md); display: flex; align-items: center; justify-content: center; font-size: 1.35rem; flex-shrink: 0; }
.kf-fi__val { font-family: var(--font-mono); font-size: var(--text-lg); font-weight: 700; line-height: 1; color: var(--text-primary); font-variant-numeric: tabular-nums; }
.kf-fi__lbl { font-size: var(--text-xs); color: var(--text-muted); margin-top: .3rem; text-transform: uppercase; letter-spacing: .04em; font-weight: 700; }
.kf-statlink { display: flex; align-items: center; justify-content: space-between; width: 100%; margin-top: 1.2rem; padding: .7rem 0 0; border: none; border-top: 1px solid var(--border-subtle); background: none; cursor: pointer; font-family: var(--font-sans); font-size: var(--text-sm); font-weight: 600; color: var(--text-secondary); transition: color var(--duration-fast) var(--ease-standard); }
.kf-statlink:hover { color: var(--primary); }
.kf-statlink span:first-child { display: flex; align-items: center; gap: .5rem; }
@media (max-width: 720px) { .kf-focusmeta { gap: 1.25rem; } }
.kf-stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(190px, 1fr)); gap: 1rem; margin-bottom: 2rem; }
.kf-quick { display: flex; flex-wrap: wrap; gap: .75rem; }
.kf-quick a {
  display: flex; flex-direction: column; align-items: center; gap: .5rem; text-decoration: none;
  background: var(--surface-card); border: 1px solid var(--border-subtle); border-radius: var(--radius-lg);
  padding: 1rem 1.5rem; min-width: 96px; color: var(--text-secondary); cursor: pointer;
  transition: transform var(--duration-base) var(--ease-standard), box-shadow var(--duration-base) var(--ease-standard), border-color var(--duration-base) var(--ease-standard), color var(--duration-base) var(--ease-standard);
}
.kf-quick a:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); border-color: var(--primary-border); color: var(--primary); }
.kf-quick__icon { font-size: 1.5rem; }
.kf-quick__label { font-size: var(--text-xs); font-weight: 600; }
.kf-skelrow { display: flex; gap: 1rem; }
`;
  function DashboardScreen({
    user,
    tasks,
    currentSession,
    resumeTask,
    onNavigate,
    focus
  }) {
    React.useEffect(() => {
      if (!document.getElementById("kf-dash-css")) {
        const s = document.createElement("style");
        s.id = "kf-dash-css";
        s.textContent = DASH_CSS;
        document.head.appendChild(s);
      }
    }, []);
    const [loaded, setLoaded] = React.useState(false);
    React.useEffect(() => {
      const t = setTimeout(() => setLoaded(true), 650);
      return () => clearTimeout(t);
    }, []);
    const pending = tasks.filter(t => t.status !== "Done").length;
    const done = tasks.filter(t => t.status === "Done").length;
    const total = tasks.length;
    const pct = total ? Math.round(done / total * 100) : 0;
    const date = new Date().toLocaleDateString("fr-FR", {
      weekday: "long",
      day: "numeric",
      month: "long",
      year: "numeric"
    });
    const f = focus || window.KFData && window.KFData.focus || {
      focusMinutes: 85,
      sprints: 3,
      sprintGoal: 4,
      streak: 5
    };
    const fmtMin = m => `${Math.floor(m / 60)}h ${String(m % 60).padStart(2, "0")}`;
    const sprintsLeft = Math.max(0, f.sprintGoal - f.sprints);
    const goalReached = f.sprints >= f.sprintGoal;
    if (!loaded) {
      return /*#__PURE__*/React.createElement("div", {
        className: "kf-dash"
      }, /*#__PURE__*/React.createElement("div", {
        className: "kf-dash__head"
      }, /*#__PURE__*/React.createElement(Skeleton, {
        width: 260,
        height: 30
      }), /*#__PURE__*/React.createElement("div", {
        style: {
          height: 8
        }
      }), /*#__PURE__*/React.createElement(Skeleton, {
        width: 200,
        height: 16
      })), /*#__PURE__*/React.createElement(Skeleton, {
        width: "100%",
        height: 92,
        radius: 12
      }), /*#__PURE__*/React.createElement("div", {
        style: {
          height: 24
        }
      }), /*#__PURE__*/React.createElement(Skeleton, {
        width: "100%",
        height: 140,
        radius: 12
      }), /*#__PURE__*/React.createElement("div", {
        style: {
          height: 24
        }
      }), /*#__PURE__*/React.createElement("div", {
        className: "kf-skelrow"
      }, [0, 1, 2].map(i => /*#__PURE__*/React.createElement(Skeleton, {
        key: i,
        width: "100%",
        height: 84,
        radius: 12
      }))));
    }
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-dash"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-dash__head"
    }, /*#__PURE__*/React.createElement("h1", {
      className: "kf-dash__greet"
    }, "Bonjour, ", user.name, " ", /*#__PURE__*/React.createElement("span", {
      "aria-hidden": "true"
    }, "\uD83D\uDC4B")), /*#__PURE__*/React.createElement("p", {
      className: "kf-dash__date"
    }, date)), /*#__PURE__*/React.createElement("h2", {
      className: "kf-eyebrow"
    }, "\xC0 reprendre"), /*#__PURE__*/React.createElement(Card, {
      interactive: true,
      className: "kf-resume",
      onClick: () => onNavigate(resumeTask ? "pomodoro" : "tasks")
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-resume__in"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-resume__icon",
      "aria-hidden": "true"
    }, resumeTask ? "⏸️" : "🚀"), /*#__PURE__*/React.createElement("div", {
      className: "kf-resume__body"
    }, /*#__PURE__*/React.createElement("p", {
      className: "kf-resume__title"
    }, resumeTask ? "Sprint #2 interrompu" : "Reprendre une tâche en cours"), /*#__PURE__*/React.createElement("div", {
      className: "kf-resume__meta"
    }, /*#__PURE__*/React.createElement("span", null, resumeTask ? resumeTask.title : "Refactor du handler d'authentification JWT"), /*#__PURE__*/React.createElement(Badge, {
      status: "InProgress"
    }))), /*#__PURE__*/React.createElement(Button, {
      variant: "primary"
    }, resumeTask ? "Reprendre le sprint" : "Continuer"))), /*#__PURE__*/React.createElement("h2", {
      className: "kf-eyebrow"
    }, "Focus aujourd'hui"), /*#__PURE__*/React.createElement(Card, {
      className: "kf-focuscard"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-focushero"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-focushero__top"
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-focushero__lbl"
    }, "Objectif de sprints"), /*#__PURE__*/React.createElement("span", {
      className: "kf-focushero__count"
    }, f.sprints, " / ", f.sprintGoal)), /*#__PURE__*/React.createElement("div", {
      className: "kf-dots",
      role: "img",
      "aria-label": `${f.sprints} sprints effectués sur un objectif de ${f.sprintGoal}`
    }, Array.from({
      length: f.sprintGoal
    }).map((_, i) => /*#__PURE__*/React.createElement("span", {
      key: i,
      className: `kf-dot ${i < f.sprints ? "kf-dot--on" : "kf-dot--off"}`
    }))), /*#__PURE__*/React.createElement("div", {
      className: "kf-focushero__hint"
    }, goalReached ? "🎉 Objectif atteint — belle journée de focus !" : `Plus que ${sprintsLeft} sprint${sprintsLeft > 1 ? "s" : ""} pour atteindre votre objectif.`)), /*#__PURE__*/React.createElement("div", {
      className: "kf-focusmeta"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-fi"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-fi__icon",
      style: {
        background: "var(--primary-subtle)"
      },
      "aria-hidden": "true"
    }, "\u23F1\uFE0F"), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
      className: "kf-fi__val"
    }, fmtMin(f.focusMinutes)), /*#__PURE__*/React.createElement("div", {
      className: "kf-fi__lbl"
    }, "Temps de focus"))), /*#__PURE__*/React.createElement("div", {
      className: "kf-fi"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-fi__icon",
      style: {
        background: "var(--danger-subtle)"
      },
      "aria-hidden": "true"
    }, "\uD83D\uDD25"), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
      className: "kf-fi__val"
    }, f.streak, " j"), /*#__PURE__*/React.createElement("div", {
      className: "kf-fi__lbl"
    }, "S\xE9rie de focus")))), /*#__PURE__*/React.createElement("button", {
      className: "kf-statlink",
      onClick: () => onNavigate("stats")
    }, /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement("span", {
      "aria-hidden": "true"
    }, "\uD83D\uDCCA"), " Statistiques de focus"), /*#__PURE__*/React.createElement("span", {
      "aria-hidden": "true"
    }, "\u2192"))), /*#__PURE__*/React.createElement("h2", {
      className: "kf-eyebrow"
    }, "Aper\xE7u"), /*#__PURE__*/React.createElement("div", {
      className: "kf-stats"
    }, /*#__PURE__*/React.createElement(StatCard, {
      as: "button",
      icon: "\u2611\uFE0F",
      value: pending,
      label: "T\xE2ches en cours",
      onClick: () => onNavigate("tasks")
    }), /*#__PURE__*/React.createElement(StatCard, {
      as: "button",
      icon: "\u2713",
      value: done,
      label: "T\xE2ches termin\xE9es",
      tone: "success",
      onClick: () => onNavigate("tasks")
    }), /*#__PURE__*/React.createElement(StatCard, {
      as: "button",
      icon: "\uD83C\uDF45",
      value: currentSession ? "En cours" : "—",
      label: currentSession ? "Sprint actif" : "Aucun sprint actif",
      tone: "warning",
      active: !!currentSession,
      onClick: () => onNavigate("pomodoro")
    }), /*#__PURE__*/React.createElement(StatCard, {
      as: "button",
      icon: "\uD83D\uDCD6",
      value: 6,
      label: "Entr\xE9es aujourd'hui",
      tone: "task",
      onClick: () => onNavigate("journal")
    })), /*#__PURE__*/React.createElement("h2", {
      className: "kf-eyebrow"
    }, "Acc\xE8s rapides"), /*#__PURE__*/React.createElement("div", {
      className: "kf-quick"
    }, /*#__PURE__*/React.createElement("a", {
      onClick: () => onNavigate("tasks")
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-quick__icon",
      "aria-hidden": "true"
    }, "\u2611\uFE0F"), /*#__PURE__*/React.createElement("span", {
      className: "kf-quick__label"
    }, "Mes t\xE2ches")), /*#__PURE__*/React.createElement("a", {
      onClick: () => onNavigate("pomodoro")
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-quick__icon",
      "aria-hidden": "true"
    }, "\uD83C\uDF45"), /*#__PURE__*/React.createElement("span", {
      className: "kf-quick__label"
    }, "Pomodoro")), /*#__PURE__*/React.createElement("a", {
      onClick: () => onNavigate("journal")
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-quick__icon",
      "aria-hidden": "true"
    }, "\uD83D\uDCD6"), /*#__PURE__*/React.createElement("span", {
      className: "kf-quick__label"
    }, "Journal"))));
  }
  window.DashboardScreen = DashboardScreen;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/DashboardScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/JournalScreen.jsx
try { (() => {
(() => {
  // Journal — activity timeline (connexion uniforme: dots + lines), "today"
  // emphasised, day navigation, pedagogical empty state.
  const {
    Tag,
    IconButton
  } = window.KairuFocusDesignSystem_0d3740;
  const {
    ArrowLeft,
    ArrowRight
  } = window.KFIcons;
  const NB_CSS = `
.kf-nb { max-width: var(--container-read); margin: 0 auto;
  --nb-body-bg: #fafaf7; --nb-body-border: #e4e0d8; --nb-line: #e8e4da;
  --nb-time: #a09ea8; --nb-label: #2c2c3a; --nb-tasks-label: #9896a8; --nb-tasks-item: #3a3848;
  --nb-comment-bg: #f0ede6; --nb-comment-border: #d4c89a; --nb-comment-text: #3c3a4a; --nb-dot-default: #c8c4bc;
}
[data-bs-theme="dark"] .kf-nb {
  --nb-body-bg: #13131f; --nb-body-border: #252538; --nb-line: #252538;
  --nb-time: #6272a4; --nb-label: #ccd6f6; --nb-tasks-label: #6272a4; --nb-tasks-item: #8892b0;
  --nb-comment-bg: #1c1c2e; --nb-comment-border: #3a3a5c; --nb-comment-text: #8892b0; --nb-dot-default: #3a3a5c;
}
.kf-nb__header { background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); border-radius: var(--radius-md) var(--radius-md) 0 0;
  padding: 1.25rem; display: grid; grid-template-columns: 40px 1fr 40px; align-items: center; gap: .5rem; }
.kf-nb__navbtn { width: 32px; height: 32px; justify-self: center; display: flex; align-items: center; justify-content: center;
  border: 1px solid rgba(255,255,255,.2); border-radius: 4px; background: none; color: #ccd6f6; cursor: pointer; transition: background-color var(--duration-fast); }
.kf-nb__navbtn:hover:not(:disabled) { background: rgba(255,255,255,.1); }
.kf-nb__navbtn:disabled { opacity: .3; cursor: default; }
.kf-nb__title { text-align: center; }
.kf-nb__day { font-size: .7rem; text-transform: uppercase; letter-spacing: var(--tracking-wider); color: #8892b0; font-weight: 700; }
.kf-nb__date { font-size: 1.3rem; font-weight: 700; color: #e6f1ff; line-height: 1.2; }
.kf-nb__today { display: inline-block; margin-top: .25rem; font-size: .62rem; font-weight: 700; letter-spacing: .08em; text-transform: uppercase;
  color: #7ee7c7; background: rgba(126,231,199,.12); padding: .1rem .45rem; border-radius: var(--radius-pill); }
.kf-nb__body { background: var(--nb-body-bg); border: 1px solid var(--nb-body-border); border-top: none; border-radius: 0 0 var(--radius-md) var(--radius-md); min-height: 140px; }
.kf-nb__timeline { padding: 1rem 1.25rem 1rem .75rem; }
.kf-nb__entry { display: grid; grid-template-columns: 48px 20px 1fr; gap: 0 .5rem; min-height: 40px; }
.kf-nb__time { font-size: .72rem; font-weight: 700; color: var(--nb-time); text-align: right; padding-top: 2px; font-variant-numeric: tabular-nums; }
.kf-nb__dotcol { display: flex; flex-direction: column; align-items: center; }
.kf-nb__dot { width: 10px; height: 10px; border-radius: 50%; border: 2px solid var(--nb-dot-default); background: var(--nb-body-bg); margin-top: 4px; flex-shrink: 0; }
.kf-nb__line { width: 2px; flex-grow: 1; background: var(--nb-line); margin-top: 3px; min-height: 16px; }
.kf-nb__entry:last-child .kf-nb__line { display: none; }
.kf-nb__entry.sprint .kf-nb__dot { border-color: var(--primary); background: color-mix(in srgb, var(--primary) 14%, var(--nb-body-bg)); }
.kf-nb__entry.break .kf-nb__dot { border-color: var(--success); background: color-mix(in srgb, var(--success) 14%, var(--nb-body-bg)); }
.kf-nb__entry.task .kf-nb__dot { border-color: var(--task); background: color-mix(in srgb, var(--task) 14%, var(--nb-body-bg)); }
.kf-nb__entry.warn .kf-nb__dot { border-color: var(--kf-orange-500); background: color-mix(in srgb, var(--kf-orange-500) 14%, var(--nb-body-bg)); }
.kf-nb__content { padding-bottom: 1rem; }
.kf-nb__event { display: flex; align-items: baseline; gap: .4rem; margin-bottom: .2rem; }
.kf-nb__icon { font-size: .95rem; }
.kf-nb__elabel { font-size: .88rem; font-weight: 700; color: var(--nb-label); }
.kf-nb__tasks { margin: .3rem 0 .2rem 1.4rem; }
.kf-nb__tasks-h { font-size: .68rem; text-transform: uppercase; letter-spacing: .08em; color: var(--nb-tasks-label); font-weight: 700; }
.kf-nb__tasks-list { margin: .2rem 0 0; padding: 0; list-style: none; }
.kf-nb__tasks-list li { font-size: .83rem; color: var(--nb-tasks-item); padding: .1rem 0; display: flex; align-items: center; gap: .35rem; }
.kf-nb__tasks-list li::before { content: '→'; color: var(--primary); font-weight: 700; font-size: .75rem; }
.kf-nb__comment { margin: .3rem 0 0 1.4rem; }
.kf-nb__comment-body { display: flex; align-items: flex-start; justify-content: space-between; gap: .5rem;
  background: var(--nb-comment-bg); border-left: 3px solid var(--nb-comment-border); padding: .35rem .55rem; border-radius: 0 4px 4px 0; font-size: .82rem; color: var(--nb-comment-text); }
.kf-nb__empty { padding: 3rem 2rem; text-align: center; color: var(--nb-time); }
.kf-nb__empty-icon { font-size: 2.5rem; opacity: .4; margin-bottom: .75rem; }
`;
  function JournalScreen({
    entries,
    dayOffset,
    onPrev,
    onNext
  }) {
    React.useEffect(() => {
      if (document.getElementById("kf-nb-css")) return;
      const s = document.createElement("style");
      s.id = "kf-nb-css";
      s.textContent = NB_CSS;
      document.head.appendChild(s);
    }, []);
    const isToday = dayOffset === 0;
    const base = new Date();
    base.setDate(base.getDate() + dayOffset);
    const day = base.toLocaleDateString("fr-FR", {
      weekday: "long"
    }).toUpperCase();
    const date = base.toLocaleDateString("fr-FR", {
      day: "numeric",
      month: "long",
      year: "numeric"
    });
    const list = isToday ? entries : [];
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-nb"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__header"
    }, /*#__PURE__*/React.createElement("button", {
      className: "kf-nb__navbtn",
      "aria-label": "Jour pr\xE9c\xE9dent",
      onClick: onPrev
    }, /*#__PURE__*/React.createElement(ArrowLeft, {
      size: 16
    })), /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__title"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__day"
    }, day), /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__date"
    }, date), isToday && /*#__PURE__*/React.createElement("span", {
      className: "kf-nb__today"
    }, "Aujourd'hui")), /*#__PURE__*/React.createElement("button", {
      className: "kf-nb__navbtn",
      "aria-label": "Jour suivant",
      disabled: isToday,
      onClick: onNext
    }, /*#__PURE__*/React.createElement(ArrowRight, {
      size: 16
    }))), /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__body"
    }, list.length === 0 ? /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__empty"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__empty-icon",
      "aria-hidden": "true"
    }, "\uD83D\uDCD3"), /*#__PURE__*/React.createElement("p", {
      style: {
        fontStyle: "italic",
        margin: "0 0 .25rem"
      }
    }, "Aucune activit\xE9 enregistr\xE9e ce jour."), isToday && /*#__PURE__*/React.createElement("p", {
      style: {
        fontSize: ".82rem",
        margin: 0
      }
    }, "D\xE9marrez un sprint Pomodoro pour voir appara\xEEtre vos entr\xE9es ici.")) : /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__timeline"
    }, list.map(e => /*#__PURE__*/React.createElement("div", {
      className: `kf-nb__entry ${e.variant}`,
      key: e.id
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__time"
    }, e.time), /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__dotcol"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__dot"
    }), /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__line"
    })), /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__content"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__event"
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-nb__icon",
      "aria-hidden": "true"
    }, e.icon), /*#__PURE__*/React.createElement("span", {
      className: "kf-nb__elabel"
    }, e.label)), e.tasks && e.tasks.length > 0 && /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__tasks"
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-nb__tasks-h"
    }, "T\xE2ches travaill\xE9es"), /*#__PURE__*/React.createElement("ul", {
      className: "kf-nb__tasks-list"
    }, e.tasks.map((t, i) => /*#__PURE__*/React.createElement("li", {
      key: i
    }, /*#__PURE__*/React.createElement("span", null, t.title), t.tags.map(tag => /*#__PURE__*/React.createElement(Tag, {
      key: tag,
      hash: true
    }, tag)))))), e.comment && /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__comment"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-nb__comment-body"
    }, /*#__PURE__*/React.createElement("span", null, "\u270F\uFE0F ", e.comment)))))))));
  }
  window.JournalScreen = JournalScreen;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/JournalScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/LandingScreen.jsx
try { (() => {
(() => {
  // Landing / Home screen — hero + 4 feature blocks (the public "/" page).
  const {
    Button
  } = window.KairuFocusDesignSystem_0d3740;
  const {
    Github
  } = window.KFIcons;
  const LANDING_CSS = `
.kf-landing { min-height: 100%; display: flex; flex-direction: column; }
.kf-hero {
  background: var(--brand-hero); color: #fff; text-align: center;
  padding: 5rem 1.5rem; display: flex; align-items: center; justify-content: center;
}
.kf-hero__in { max-width: 620px; }
.kf-hero__logo {
  width: 72px; height: 72px; border-radius: var(--radius-xl);
  background: linear-gradient(135deg, var(--kf-coral-500), var(--kf-ink-700));
  box-shadow: var(--shadow-brand); display: inline-flex; align-items: center; justify-content: center;
  font-size: 2rem; font-weight: 800; color: #fff; margin-bottom: 1.5rem;
}
.kf-hero__title { font-size: var(--text-3xl); font-weight: 800; letter-spacing: var(--tracking-tight); margin: 0 0 .5rem; }
.kf-hero__tag { font-size: var(--text-md); opacity: .82; font-weight: 300; margin: 0 0 1rem; }
.kf-hero__desc { font-size: var(--text-base); opacity: .68; line-height: var(--leading-relaxed); margin: 0 auto 2rem; max-width: 520px; }
.kf-features { padding: 4rem 1.5rem; background: var(--surface-base); }
.kf-features__grid { max-width: 960px; margin: 0 auto; display: grid; grid-template-columns: repeat(4, 1fr); gap: 1.25rem; }
.kf-feature {
  background: var(--surface-card); border: 1px solid var(--border-subtle); border-radius: var(--radius-lg);
  padding: 2rem 1.5rem; text-align: center; box-shadow: var(--shadow-sm);
  transition: transform var(--duration-base) var(--ease-standard), box-shadow var(--duration-base) var(--ease-standard);
}
.kf-feature:hover { transform: translateY(-4px); box-shadow: var(--shadow-lg); }
.kf-feature__icon { font-size: 2.5rem; display: block; margin-bottom: 1rem; }
.kf-feature__title { font-size: var(--text-md); font-weight: 700; margin: 0 0 .5rem; color: var(--text-primary); }
.kf-feature__desc { font-size: var(--text-sm); color: var(--text-muted); line-height: var(--leading-relaxed); margin: 0; }
@media (max-width: 820px) { .kf-features__grid { grid-template-columns: repeat(2, 1fr); } }
`;
  const FEATURES = [{
    icon: "☑️",
    title: "Tâches",
    desc: "Micro-tâches quotidiennes avec statuts, descriptions et édition Markdown."
  }, {
    icon: "🍅",
    title: "Pomodoro",
    desc: "Sprints et pauses minutés, suggestions et liens vers vos tâches."
  }, {
    icon: "📖",
    title: "Journal",
    desc: "Timeline d'activité générée automatiquement. Annotez, parcourez l'historique."
  }, {
    icon: "🤖",
    title: "IA & MCP",
    desc: "Serveur MCP intégré : votre IA crée, liste et complète vos tâches."
  }];
  function LandingScreen({
    onLogin
  }) {
    React.useEffect(() => {
      if (document.getElementById("kf-landing-css")) return;
      const s = document.createElement("style");
      s.id = "kf-landing-css";
      s.textContent = LANDING_CSS;
      document.head.appendChild(s);
    }, []);
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-landing"
    }, /*#__PURE__*/React.createElement("section", {
      className: "kf-hero"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-hero__in"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-hero__logo"
    }, "K"), /*#__PURE__*/React.createElement("h1", {
      className: "kf-hero__title"
    }, "KairuFocus"), /*#__PURE__*/React.createElement("p", {
      className: "kf-hero__tag"
    }, "L'espace de travail pour rester focus"), /*#__PURE__*/React.createElement("p", {
      className: "kf-hero__desc"
    }, "T\xE2ches, sprints Pomodoro, journal d'activit\xE9 \u2014 tout votre workflow en un seul endroit. Connectez votre IA favorite via le protocole MCP et laissez-la g\xE9rer vos t\xE2ches."), /*#__PURE__*/React.createElement(Button, {
      variant: "accent",
      size: "lg",
      icon: /*#__PURE__*/React.createElement(Github, {
        size: 18
      }),
      onClick: onLogin
    }, "Se connecter avec GitHub"))), /*#__PURE__*/React.createElement("section", {
      className: "kf-features"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-features__grid"
    }, FEATURES.map(f => /*#__PURE__*/React.createElement("div", {
      className: "kf-feature",
      key: f.title
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-feature__icon",
      "aria-hidden": "true"
    }, f.icon), /*#__PURE__*/React.createElement("h3", {
      className: "kf-feature__title"
    }, f.title), /*#__PURE__*/React.createElement("p", {
      className: "kf-feature__desc"
    }, f.desc))))));
  }
  window.LandingScreen = LandingScreen;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/LandingScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/LoginScreen.jsx
try { (() => {
(() => {
  // Login screen (/login) — focused card, single action, reassuring. Mirrors Login.razor.
  const {
    Button
  } = window.KairuFocusDesignSystem_0d3740;
  const {
    Github
  } = window.KFIcons;
  const LOGIN_CSS = `
.kf-login { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: var(--surface-base); padding: 1.5rem; }
.kf-login__card { display: flex; flex-direction: column; align-items: center; gap: 1rem; padding: 2.5rem 2rem; background: var(--surface-card);
  border: 1px solid var(--border-subtle); border-radius: var(--radius-lg); box-shadow: var(--shadow-sm); max-width: 380px; width: 100%; text-align: center; }
.kf-login__logo { width: 64px; height: 64px; border-radius: var(--radius-md); background: var(--brand-mark-bg); display: flex; align-items: center; justify-content: center; font-size: 2rem; font-weight: 800; color: #fff; }
.kf-login__title { font-size: 1.75rem; font-weight: 700; margin: .25rem 0 0; color: var(--text-primary); }
.kf-login__sub { color: var(--text-muted); margin: 0 0 .5rem; font-size: .9rem; }
`;
  function LoginScreen({
    onLogin
  }) {
    React.useEffect(() => {
      if (document.getElementById("kf-login-css")) return;
      const s = document.createElement("style");
      s.id = "kf-login-css";
      s.textContent = LOGIN_CSS;
      document.head.appendChild(s);
    }, []);
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-login"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-login__card"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-login__logo"
    }, "K"), /*#__PURE__*/React.createElement("h1", {
      className: "kf-login__title"
    }, "KairuFocus"), /*#__PURE__*/React.createElement("p", {
      className: "kf-login__sub"
    }, "Gestion d'activit\xE9 pour d\xE9veloppeurs"), /*#__PURE__*/React.createElement(Button, {
      variant: "accent",
      icon: /*#__PURE__*/React.createElement(Github, {
        size: 18
      }),
      onClick: onLogin
    }, "Se connecter avec GitHub")));
  }
  window.LoginScreen = LoginScreen;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/LoginScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/PomodoroScreen.jsx
try { (() => {
(() => {
  // Pomodoro — focus mode (Flow), smooth ring (Goal-Gradient), big primary
  // controls (Fitts + Von Restorff), sprint tasks, celebratory end (Peak-End).
  const {
    ProgressRing,
    Button,
    Badge,
    Alert,
    Card,
    IconButton
  } = window.KairuFocusDesignSystem_0d3740;
  const {
    Play,
    Stop,
    Check,
    Cross
  } = window.KFIcons;
  const POMO_CSS = `
.kf-pomo { max-width: 560px; margin: 0 auto; display: flex; flex-direction: column; align-items: center; gap: 1.25rem; }
.kf-pomo__head { width: 100%; display: flex; align-items: center; justify-content: space-between; }
.kf-pomo__head h1 { font-size: var(--text-xl); font-weight: 700; margin: 0; color: var(--text-primary); }
.kf-tabs { display: inline-flex; background: var(--surface-sunken); border-radius: var(--radius-md); padding: 4px; gap: 4px; }
.kf-tab {
  border: none; background: transparent; color: var(--text-secondary); cursor: pointer;
  padding: .5rem .9rem; border-radius: var(--radius-sm); font-size: var(--text-sm); font-weight: 600;
  font-family: var(--font-sans); transition: background-color var(--duration-fast) var(--ease-standard), color var(--duration-fast) var(--ease-standard);
}
.kf-tab:hover { color: var(--text-primary); }
.kf-tab.is-active { background: var(--surface-card); color: var(--text-primary); box-shadow: var(--shadow-xs); }
.kf-pomo__ring { padding: .5rem 0; }
.kf-sprinttasks { width: 100%; }
.kf-sprinttasks h2 { font-size: var(--text-md); font-weight: 700; margin: 0 0 .75rem; color: var(--text-primary); }
.kf-linked { display: flex; flex-direction: column; gap: .5rem; }
.kf-linkedrow {
  display: flex; align-items: center; justify-content: space-between; gap: .75rem;
  padding: .65rem .9rem; background: var(--surface-card); border: 1px solid var(--border-subtle); border-radius: var(--radius-md);
}
.kf-linkedrow__title { font-size: var(--text-sm); color: var(--text-primary); }
.kf-linkedrow__actions { display: flex; gap: .35rem; align-items: center; }
/* Focus mode (Flow) — strip the chrome */
.kf-focus { position: fixed; inset: 0; z-index: 100; background: var(--brand-hero); display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 2rem; }
.kf-focus__task { color: rgba(255,255,255,.85); font-size: var(--text-md); font-weight: 600; }
.kf-focus__exit { position: absolute; top: 1.5rem; right: 1.5rem; }
`;
  const TYPES = [{
    id: "Sprint",
    label: "🍅 Sprint",
    min: 25,
    state: "sprint",
    status: "Sprint en cours"
  }, {
    id: "ShortBreak",
    label: "☕ Pause courte",
    min: 5,
    state: "shortBreak",
    status: "Pause courte"
  }, {
    id: "LongBreak",
    label: "🌙 Pause longue",
    min: 15,
    state: "longBreak",
    status: "Pause longue"
  }];
  function fmt(s) {
    return `${String(Math.floor(s / 60)).padStart(2, "0")}:${String(s % 60).padStart(2, "0")}`;
  }
  function PomodoroScreen({
    linkedTasks,
    onCompleteLinked,
    onUnlink,
    running,
    onRunningChange,
    onSprintLibre
  }) {
    React.useEffect(() => {
      if (document.getElementById("kf-pomo-css")) return;
      const s = document.createElement("style");
      s.id = "kf-pomo-css";
      s.textContent = POMO_CSS;
      document.head.appendChild(s);
    }, []);
    const [typeId, setTypeId] = React.useState("Sprint");
    const type = TYPES.find(t => t.id === typeId);
    const total = type.min * 60;
    const [remaining, setRemaining] = React.useState(total);
    const [active, setActive] = React.useState(false);
    const [done, setDone] = React.useState(false);
    const [focus, setFocus] = React.useState(false);
    React.useEffect(() => {
      if (!active) {
        setRemaining(total);
        setDone(false);
      }
    }, [typeId]);
    React.useEffect(() => {
      if (!active) return;
      const id = setInterval(() => setRemaining(r => {
        if (r <= 1) {
          clearInterval(id);
          setActive(false);
          setDone(true);
          onRunningChange && onRunningChange(false);
          return 0;
        }
        return r - 1;
      }), 1000);
      return () => clearInterval(id);
    }, [active]);
    const start = () => {
      setRemaining(total);
      setActive(true);
      setDone(false);
      onRunningChange && onRunningChange(true);
    };
    const interrupt = () => {
      setActive(false);
      setRemaining(total);
      onRunningChange && onRunningChange(false);
    };
    const progress = total ? remaining / total : 0;
    const ringState = done ? "done" : active ? type.state : "idle";
    const centerLabel = done ? "✓" : fmt(remaining);
    const centerSub = done ? "Terminé !" : active ? type.status : "Prêt";
    const ring = /*#__PURE__*/React.createElement(ProgressRing, {
      progress: done ? 1 : progress,
      state: ringState,
      size: 236,
      label: centerLabel,
      sublabel: centerSub
    });
    if (focus) {
      return /*#__PURE__*/React.createElement("div", {
        className: "kf-focus"
      }, /*#__PURE__*/React.createElement("div", {
        className: "kf-focus__exit"
      }, /*#__PURE__*/React.createElement(Button, {
        variant: "ghost",
        onClick: () => setFocus(false),
        style: {
          color: "#fff"
        }
      }, "Quitter le focus")), /*#__PURE__*/React.createElement("div", {
        style: {
          filter: "saturate(1.1)"
        }
      }, ring), linkedTasks[0] && /*#__PURE__*/React.createElement("div", {
        className: "kf-focus__task"
      }, "\u25B8 ", linkedTasks[0].title), /*#__PURE__*/React.createElement(Button, {
        variant: "accent",
        size: "lg",
        icon: /*#__PURE__*/React.createElement(Stop, {
          size: 18
        }),
        onClick: () => {
          interrupt();
          setFocus(false);
        }
      }, "Interrompre"));
    }
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-pomo"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-pomo__head"
    }, /*#__PURE__*/React.createElement("h1", null, "Pomodoro"), active ? /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      onClick: () => setFocus(true)
    }, "Mode focus") : /*#__PURE__*/React.createElement(Button, {
      variant: "ghost",
      onClick: onSprintLibre
    }, "Sprint libre")), !active && !done && /*#__PURE__*/React.createElement("div", {
      className: "kf-tabs",
      role: "tablist",
      "aria-label": "Type de session"
    }, TYPES.map(t => /*#__PURE__*/React.createElement("button", {
      key: t.id,
      role: "tab",
      "aria-selected": t.id === typeId,
      className: `kf-tab ${t.id === typeId ? "is-active" : ""}`,
      onClick: () => setTypeId(t.id)
    }, t.label, " (", t.min, " min)"))), /*#__PURE__*/React.createElement("div", {
      className: "kf-pomo__ring"
    }, ring), /*#__PURE__*/React.createElement("div", null, active ? /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      size: "lg",
      icon: /*#__PURE__*/React.createElement(Stop, {
        size: 18
      }),
      onClick: interrupt
    }, "Interrompre") : /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "lg",
      icon: /*#__PURE__*/React.createElement(Play, {
        size: 16
      }),
      onClick: start
    }, "D\xE9marrer")), done && /*#__PURE__*/React.createElement(Alert, {
      tone: "success"
    }, "\uD83C\uDF45 Sprint termin\xE9 ! ", /*#__PURE__*/React.createElement("strong", null, "Pause recommand\xE9e : courte")), active && typeId === "Sprint" && linkedTasks.length > 0 && /*#__PURE__*/React.createElement("div", {
      className: "kf-sprinttasks"
    }, /*#__PURE__*/React.createElement("h2", null, "T\xE2ches du sprint"), /*#__PURE__*/React.createElement("div", {
      className: "kf-linked"
    }, linkedTasks.map(t => /*#__PURE__*/React.createElement("div", {
      className: "kf-linkedrow",
      key: t.id
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-linkedrow__title"
    }, t.title), /*#__PURE__*/React.createElement("span", {
      className: "kf-linkedrow__actions"
    }, /*#__PURE__*/React.createElement(Badge, {
      status: t.status
    }), t.status !== "Done" && /*#__PURE__*/React.createElement(IconButton, {
      label: `Terminer ${t.title}`,
      tone: "success",
      outline: true,
      size: "sm",
      onClick: () => onCompleteLinked(t.id)
    }, /*#__PURE__*/React.createElement(Check, {
      size: 14
    })), /*#__PURE__*/React.createElement(IconButton, {
      label: `Retirer ${t.title}`,
      tone: "danger",
      outline: true,
      size: "sm",
      onClick: () => onUnlink(t.id)
    }, /*#__PURE__*/React.createElement(Cross, {
      size: 13
    }))))))));
  }
  window.PomodoroScreen = PomodoroScreen;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/PomodoroScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/SettingsScreen.jsx
try { (() => {
(() => {
  // Settings — grouped sections (Common Region + chunking), clear labels,
  // save confirmation toast, sensible defaults.
  const {
    Card,
    Select,
    Input,
    Button,
    Alert,
    Badge
  } = window.KairuFocusDesignSystem_0d3740;
  const SET_CSS = `
.kf-settings { max-width: var(--container-md); }
.kf-settings h1 { font-size: var(--text-xl); font-weight: 700; margin: 0 0 1.5rem; color: var(--text-primary); }
.kf-section { margin-bottom: 1.25rem; }
.kf-section h2 { font-size: var(--text-md); font-weight: 700; margin: 0 0 1rem; color: var(--text-primary); display: flex; align-items: center; gap: .5rem; }
.kf-field-row { margin-bottom: 1rem; }
.kf-hint { font-size: var(--text-xs); color: var(--text-muted); margin-top: .35rem; }
.kf-inline { display: flex; gap: .6rem; align-items: flex-end; }
.kf-grid3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: .75rem; }
.kf-actions { display: flex; gap: .6rem; margin-top: 1rem; }
`;
  function SettingsScreen({
    sprintGoal,
    onSprintGoal
  }) {
    React.useEffect(() => {
      if (document.getElementById("kf-set-css")) return;
      const s = document.createElement("style");
      s.id = "kf-set-css";
      s.textContent = SET_CSS;
      document.head.appendChild(s);
    }, []);
    const [theme, setTheme] = React.useState(document.documentElement.getAttribute("data-bs-theme") === "dark" ? "Dark" : "Light");
    const [ringtone, setRingtone] = React.useState("AlarmClock");
    const [sprint, setSprint] = React.useState(25);
    const [shortB, setShortB] = React.useState(5);
    const [longB, setLongB] = React.useState(15);
    const storeGoal = window.KFData && window.KFData.focus || {};
    const [sGoal, setSGoal] = React.useState(sprintGoal != null ? sprintGoal : storeGoal.sprintGoal || 4);
    const [saved, setSaved] = React.useState(false);
    const applyTheme = v => {
      setTheme(v);
      document.documentElement.setAttribute("data-bs-theme", v.toLowerCase());
    };
    const changeSprintGoal = v => {
      const n = Number(v);
      setSGoal(n);
      onSprintGoal && onSprintGoal(n);
    };
    const save = () => {
      setSaved(true);
      setTimeout(() => setSaved(false), 2600);
    };
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-settings"
    }, /*#__PURE__*/React.createElement("h1", null, "\u2699 Param\xE8tres"), /*#__PURE__*/React.createElement(Card, {
      className: "kf-section"
    }, /*#__PURE__*/React.createElement("h2", null, "\uD83C\uDFA8 Th\xE8me"), /*#__PURE__*/React.createElement("div", {
      className: "kf-field-row"
    }, /*#__PURE__*/React.createElement(Select, {
      label: "Pr\xE9f\xE9rence de th\xE8me",
      value: theme,
      onChange: e => applyTheme(e.target.value),
      options: [{
        value: "Light",
        label: "☀️ Clair"
      }, {
        value: "Dark",
        label: "🌙 Sombre"
      }, {
        value: "System",
        label: "⚙️ Système"
      }]
    }), /*#__PURE__*/React.createElement("div", {
      className: "kf-hint"
    }, "Le th\xE8me est appliqu\xE9 imm\xE9diatement et sauvegard\xE9."))), /*#__PURE__*/React.createElement(Card, {
      className: "kf-section"
    }, /*#__PURE__*/React.createElement("h2", null, "\uD83D\uDD14 Sonnerie Pomodoro"), /*#__PURE__*/React.createElement("div", {
      className: "kf-inline"
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        flex: 1
      }
    }, /*#__PURE__*/React.createElement(Select, {
      label: "Son de fin de session",
      value: ringtone,
      onChange: e => setRingtone(e.target.value),
      options: [{
        value: "None",
        label: "🔇 Aucun son"
      }, {
        value: "AlarmClock",
        label: "⏰ Réveil"
      }, {
        value: "Bird",
        label: "🐦 Oiseau"
      }]
    })), /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      disabled: ringtone === "None"
    }, "\u25B6 Tester")), /*#__PURE__*/React.createElement("div", {
      className: "kf-hint"
    }, "Jou\xE9 automatiquement \xE0 la fin de chaque sprint ou pause.")), /*#__PURE__*/React.createElement(Card, {
      className: "kf-section"
    }, /*#__PURE__*/React.createElement("h2", null, "Token MCP"), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: ".6rem",
        marginBottom: ".75rem",
        fontSize: "var(--text-sm)",
        color: "var(--text-secondary)"
      }
    }, /*#__PURE__*/React.createElement(Badge, {
      status: "Done"
    }, "Actif"), " Expire le ", /*#__PURE__*/React.createElement("strong", null, "03/03/2026")), /*#__PURE__*/React.createElement("div", {
      className: "kf-actions",
      style: {
        marginTop: 0
      }
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "secondary"
    }, "R\xE9g\xE9n\xE9rer"), /*#__PURE__*/React.createElement(Button, {
      variant: "danger"
    }, "R\xE9voquer"))), /*#__PURE__*/React.createElement(Card, {
      className: "kf-section"
    }, /*#__PURE__*/React.createElement("h2", null, "\uD83C\uDFAF Objectif de focus"), /*#__PURE__*/React.createElement("div", {
      className: "kf-field-row"
    }, /*#__PURE__*/React.createElement(Select, {
      label: "Sprints vis\xE9s par jour",
      value: String(sGoal),
      onChange: e => changeSprintGoal(e.target.value),
      options: [{
        value: "2",
        label: "2 sprints"
      }, {
        value: "4",
        label: "4 sprints"
      }, {
        value: "6",
        label: "6 sprints"
      }, {
        value: "8",
        label: "8 sprints"
      }, {
        value: "10",
        label: "10 sprints"
      }]
    }), /*#__PURE__*/React.createElement("div", {
      className: "kf-hint"
    }, "Votre objectif quotidien, affich\xE9 sur le Dashboard (\xAB Focus aujourd'hui \xBB). Un sprint = une session Pomodoro."))), /*#__PURE__*/React.createElement(Card, {
      className: "kf-section"
    }, /*#__PURE__*/React.createElement("h2", null, "\uD83C\uDF45 Pomodoro"), /*#__PURE__*/React.createElement("div", {
      className: "kf-grid3"
    }, /*#__PURE__*/React.createElement(Input, {
      label: "Sprint (min)",
      type: "number",
      value: sprint,
      onChange: e => setSprint(e.target.value)
    }), /*#__PURE__*/React.createElement(Input, {
      label: "Pause courte",
      type: "number",
      value: shortB,
      onChange: e => setShortB(e.target.value)
    }), /*#__PURE__*/React.createElement(Input, {
      label: "Pause longue",
      type: "number",
      value: longB,
      onChange: e => setLongB(e.target.value)
    })), /*#__PURE__*/React.createElement("div", {
      className: "kf-hint"
    }, "Entre 1 et 120 minutes. Une pause longue est recommand\xE9e tous les 4 sprints."), saved && /*#__PURE__*/React.createElement("div", {
      style: {
        marginTop: "1rem"
      }
    }, /*#__PURE__*/React.createElement(Alert, {
      tone: "success"
    }, "Param\xE8tres enregistr\xE9s avec succ\xE8s !")), /*#__PURE__*/React.createElement("div", {
      className: "kf-actions"
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      onClick: save
    }, "Enregistrer"), /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      onClick: () => {
        setSprint(25);
        setShortB(5);
        setLongB(15);
      }
    }, "R\xE9initialiser"))));
  }
  window.SettingsScreen = SettingsScreen;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/SettingsScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/SprintLibreScreen.jsx
try { (() => {
(() => {
  // Sprint libre (/pomodoro/libre) — open-ended count-up timer with optional
  // note + linked task, finish/interrupt, and today's sprint history. Mirrors SprintLibre.razor.
  const {
    Input,
    Select,
    Button,
    Badge,
    Card,
    Alert
  } = window.KairuFocusDesignSystem_0d3740;
  const {
    ArrowLeft,
    Play,
    Check,
    Stop
  } = window.KFIcons;
  const SL_CSS = `
.kf-sl { max-width: 560px; margin: 0 auto; display: flex; flex-direction: column; align-items: center; gap: 1.25rem; }
.kf-sl__head { width: 100%; }
.kf-sl__head h1 { font-size: var(--text-xl); font-weight: 700; margin: .75rem 0 0; color: var(--text-primary); }
.kf-sl__config { width: 100%; max-width: 480px; display: flex; flex-direction: column; gap: 1rem; }
.kf-sl__clock { font-family: var(--font-mono); font-size: 4rem; font-weight: 700; letter-spacing: .04em; font-variant-numeric: tabular-nums; line-height: 1; }
.kf-sl__status { color: var(--text-muted); font-size: var(--text-sm); }
.kf-sl__history { width: 100%; max-width: 560px; }
.kf-sl__history h2 { font-size: var(--text-md); font-weight: 700; margin: 0 0 .75rem; color: var(--text-primary); }
.kf-sl__row { display: flex; align-items: center; justify-content: space-between; gap: .75rem; padding: .65rem .9rem; background: var(--surface-card); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); }
.kf-sl__row + .kf-sl__row { margin-top: .5rem; }
.kf-sl__rowtask { font-size: var(--text-sm); color: var(--text-secondary); }
.kf-sl__rowdur { font-family: var(--font-mono); font-size: var(--text-sm); color: var(--text-muted); }
`;
  function fmt(s) {
    return `${String(Math.floor(s / 3600)).padStart(2, "0")}:${String(Math.floor(s % 3600 / 60)).padStart(2, "0")}:${String(s % 60).padStart(2, "0")}`;
  }
  function SprintLibreScreen({
    tasks,
    onBack
  }) {
    React.useEffect(() => {
      if (document.getElementById("kf-sl-css")) return;
      const s = document.createElement("style");
      s.id = "kf-sl-css";
      s.textContent = SL_CSS;
      document.head.appendChild(s);
    }, []);
    const [note, setNote] = React.useState("");
    const [taskId, setTaskId] = React.useState("");
    const [elapsed, setElapsed] = React.useState(0);
    const [running, setRunning] = React.useState(false);
    const [last, setLast] = React.useState(null);
    const [history, setHistory] = React.useState([{
      id: "h1",
      title: "Refactor du handler d'authentification JWT",
      dur: 1845,
      status: "Completed"
    }, {
      id: "h2",
      title: null,
      dur: 612,
      status: "Interrupted"
    }]);
    React.useEffect(() => {
      if (!running) return;
      const id = setInterval(() => setElapsed(e => e + 1), 1000);
      return () => clearInterval(id);
    }, [running]);
    const available = tasks.filter(t => t.status !== "Done");
    const start = () => {
      setElapsed(0);
      setRunning(true);
      setLast(null);
    };
    const stop = status => {
      setRunning(false);
      const t = available.find(x => x.id === taskId);
      const entry = {
        id: "h" + Date.now(),
        title: t ? t.title : null,
        dur: elapsed,
        status
      };
      setHistory(h => [...h, entry]);
      setLast(entry);
      setNote("");
      setTaskId("");
    };
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-sl"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-sl__head"
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "ghost",
      size: "sm",
      icon: /*#__PURE__*/React.createElement(ArrowLeft, {
        size: 16
      }),
      onClick: onBack
    }, "Retour"), /*#__PURE__*/React.createElement("h1", null, "Sprint libre")), !running && /*#__PURE__*/React.createElement("div", {
      className: "kf-sl__config"
    }, /*#__PURE__*/React.createElement(Input, {
      label: "Note (optionnelle)",
      value: note,
      onChange: e => setNote(e.target.value),
      placeholder: "Ex. daily un peu long, bloqu\xE9 sur X\u2026"
    }), /*#__PURE__*/React.createElement(Select, {
      label: "T\xE2che li\xE9e (optionnel)",
      value: taskId,
      onChange: e => setTaskId(e.target.value),
      options: [{
        value: "",
        label: "— Aucune tâche —"
      }, ...available.map(t => ({
        value: t.id,
        label: t.title
      }))]
    })), /*#__PURE__*/React.createElement("div", {
      className: "kf-sl__clock",
      style: {
        color: running ? "var(--primary)" : "var(--text-muted)"
      }
    }, fmt(elapsed)), running && /*#__PURE__*/React.createElement("div", {
      className: "kf-sl__status"
    }, "Sprint #", history.length + 1, " en cours"), /*#__PURE__*/React.createElement("div", {
      style: {
        display: "flex",
        gap: ".6rem"
      }
    }, !running ? /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "lg",
      icon: /*#__PURE__*/React.createElement(Play, {
        size: 16
      }),
      onClick: start
    }, "D\xE9marrer") : /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "lg",
      icon: /*#__PURE__*/React.createElement(Check, {
        size: 18
      }),
      onClick: () => stop("Completed")
    }, "Terminer"), /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      size: "lg",
      icon: /*#__PURE__*/React.createElement(Stop, {
        size: 16
      }),
      onClick: () => stop("Interrupted")
    }, "Interrompre"))), last && /*#__PURE__*/React.createElement(Alert, {
      tone: "success"
    }, "Sprint enregistr\xE9 \u2014 ", fmt(last.dur), " \u2014 ", last.status === "Completed" ? "Terminé" : "Interrompu"), history.length > 0 && /*#__PURE__*/React.createElement("div", {
      className: "kf-sl__history"
    }, /*#__PURE__*/React.createElement("h2", null, "Sprints du jour (", history.length, ")"), history.map(s => /*#__PURE__*/React.createElement("div", {
      className: "kf-sl__row",
      key: s.id
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-sl__rowtask"
    }, s.title ? `🔗 ${s.title}` : /*#__PURE__*/React.createElement("span", {
      style: {
        color: "var(--text-muted)"
      }
    }, "Sans t\xE2che")), /*#__PURE__*/React.createElement("span", {
      style: {
        display: "flex",
        alignItems: "center",
        gap: ".5rem"
      }
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-sl__rowdur"
    }, fmt(s.dur)), /*#__PURE__*/React.createElement(Badge, {
      status: s.status === "Completed" ? "Done" : undefined,
      variant: s.status === "Completed" ? undefined : "inprogress"
    }, s.status === "Completed" ? "Terminé" : "Interrompu"))))));
  }
  window.SprintLibreScreen = SprintLibreScreen;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/SprintLibreScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/StatsScreen.jsx
try { (() => {
(() => {
  // Statistiques de focus — dedicated page (keeps the Dashboard scannable, Hick).
  // GitHub-style 30-day activity heatmap (brand blue), summary tiles.
  const {
    Card,
    Button
  } = window.KairuFocusDesignSystem_0d3740;
  const {
    ArrowLeft
  } = window.KFIcons;
  const STATS_CSS = `
.kf-stats2 { max-width: var(--container-lg); }
.kf-stats2 h1 { font-size: var(--text-xl); font-weight: 700; margin: .75rem 0 1.25rem; color: var(--text-primary); }
.kf-stats2__tiles { display: grid; grid-template-columns: repeat(4, 1fr); gap: 1rem; margin-bottom: 1.5rem; }
.kf-st { padding: 1.1rem 1.25rem; }
.kf-st__val { font-family: var(--font-mono); font-size: var(--text-2xl); font-weight: 700; line-height: 1; color: var(--text-primary); font-variant-numeric: tabular-nums; }
.kf-st__lbl { font-size: var(--text-xs); color: var(--text-muted); margin-top: .4rem; text-transform: uppercase; letter-spacing: .04em; font-weight: 700; }
.kf-heatcard { padding: 1.5rem 1.6rem; }
.kf-heatcard__head { display: flex; align-items: baseline; justify-content: space-between; margin-bottom: 1.2rem; gap: 1rem; flex-wrap: wrap; }
.kf-heatcard__title { font-size: var(--text-md); font-weight: 700; color: var(--text-primary); }
.kf-heatcard__sub { font-size: var(--text-xs); color: var(--text-muted); font-family: var(--font-mono); }
.kf-heat { display: grid; grid-template-rows: repeat(7, 17px); grid-auto-flow: column; grid-auto-columns: 17px; gap: 5px; }
.kf-cell { width: 17px; height: 17px; border-radius: 4px; }
.kf-cell--0 { background: var(--surface-sunken); border: 1px solid var(--border-subtle); }
.kf-cell--1 { background: color-mix(in srgb, var(--primary) 28%, var(--surface-sunken)); }
.kf-cell--2 { background: color-mix(in srgb, var(--primary) 52%, var(--surface-sunken)); }
.kf-cell--3 { background: color-mix(in srgb, var(--primary) 76%, var(--surface-sunken)); }
.kf-cell--4 { background: var(--primary); }
.kf-cell--today { box-shadow: 0 0 0 2px var(--surface-card), 0 0 0 3px var(--accent); }
.kf-legend { display: flex; align-items: center; gap: 6px; margin-top: 1.2rem; font-size: var(--text-xs); color: var(--text-muted); }
.kf-legend .kf-cell { width: 14px; height: 14px; }
@media (max-width: 720px) { .kf-stats2__tiles { grid-template-columns: 1fr 1fr; } .kf-heat { overflow-x: auto; } }
`;
  function level(n, goal) {
    if (n <= 0) return 0;
    if (n < Math.ceil(goal / 2)) return 1;
    if (n < goal) return 2;
    if (n === goal) return 3;
    return 4;
  }
  function StatsScreen({
    onBack
  }) {
    React.useEffect(() => {
      if (document.getElementById("kf-stats2-css")) return;
      const s = document.createElement("style");
      s.id = "kf-stats2-css";
      s.textContent = STATS_CSS;
      document.head.appendChild(s);
    }, []);
    const f = window.KFData && window.KFData.focus || {
      sprintGoal: 4,
      streak: 5
    };
    const month = window.KFData && window.KFData.month || [];
    const goal = f.sprintGoal;
    const total = month.reduce((s, n) => s + n, 0);
    const avg = month.length ? Math.round(total / month.length * 10) / 10 : 0;
    const activeDays = month.filter(n => n > 0).length;
    const goalHit = month.filter(n => n >= goal).length;

    // date label for each cell (J-29 … aujourd'hui)
    const label = i => {
      const dt = new Date();
      dt.setDate(dt.getDate() - (month.length - 1 - i));
      return dt.toLocaleDateString("fr-FR", {
        weekday: "short",
        day: "numeric",
        month: "short"
      });
    };
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-stats2"
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "ghost",
      size: "sm",
      icon: /*#__PURE__*/React.createElement(ArrowLeft, {
        size: 16
      }),
      onClick: onBack
    }, "Retour"), /*#__PURE__*/React.createElement("h1", null, "\uD83D\uDCCA Statistiques de focus"), /*#__PURE__*/React.createElement("div", {
      className: "kf-stats2__tiles"
    }, /*#__PURE__*/React.createElement(Card, {
      className: "kf-st"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-st__val"
    }, total), /*#__PURE__*/React.createElement("div", {
      className: "kf-st__lbl"
    }, "Sprints \xB7 30 j")), /*#__PURE__*/React.createElement(Card, {
      className: "kf-st"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-st__val"
    }, avg), /*#__PURE__*/React.createElement("div", {
      className: "kf-st__lbl"
    }, "Moyenne / jour")), /*#__PURE__*/React.createElement(Card, {
      className: "kf-st"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-st__val"
    }, activeDays, "/30"), /*#__PURE__*/React.createElement("div", {
      className: "kf-st__lbl"
    }, "Jours actifs")), /*#__PURE__*/React.createElement(Card, {
      className: "kf-st"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-st__val"
    }, f.streak, " j"), /*#__PURE__*/React.createElement("div", {
      className: "kf-st__lbl"
    }, "S\xE9rie en cours"))), /*#__PURE__*/React.createElement(Card, {
      className: "kf-heatcard"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-heatcard__head"
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-heatcard__title"
    }, "Activit\xE9 \u2014 30 derniers jours"), /*#__PURE__*/React.createElement("span", {
      className: "kf-heatcard__sub"
    }, goalHit, " jours d'objectif atteint \xB7 1 carr\xE9 = 1 jour")), /*#__PURE__*/React.createElement("div", {
      className: "kf-heat"
    }, month.map((n, i) => /*#__PURE__*/React.createElement("div", {
      key: i,
      className: `kf-cell kf-cell--${level(n, goal)} ${i === month.length - 1 ? "kf-cell--today" : ""}`,
      title: `${label(i)} : ${n} sprint${n > 1 ? "s" : ""}`
    }))), /*#__PURE__*/React.createElement("div", {
      className: "kf-legend"
    }, /*#__PURE__*/React.createElement("span", null, "Moins"), /*#__PURE__*/React.createElement("span", {
      className: "kf-cell kf-cell--0"
    }), /*#__PURE__*/React.createElement("span", {
      className: "kf-cell kf-cell--1"
    }), /*#__PURE__*/React.createElement("span", {
      className: "kf-cell kf-cell--2"
    }), /*#__PURE__*/React.createElement("span", {
      className: "kf-cell kf-cell--3"
    }), /*#__PURE__*/React.createElement("span", {
      className: "kf-cell kf-cell--4"
    }), /*#__PURE__*/React.createElement("span", null, "Plus"))));
  }
  window.StatsScreen = StatsScreen;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/StatsScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/TaskEditScreen.jsx
try { (() => {
(() => {
  // Task detail + edit — reading hierarchy, Édit/Preview tabs (Markdown),
  // visible character counter, focus management. Mirrors TaskDetail/TaskEdit.razor.
  const {
    Input,
    Textarea,
    Button,
    Badge,
    Tag,
    Card,
    Alert
  } = window.KairuFocusDesignSystem_0d3740;
  const {
    ArrowLeft,
    Pencil
  } = window.KFIcons;
  const TE_CSS = `
.kf-te { max-width: var(--container-md); }
.kf-te__back { margin-bottom: 1rem; }
.kf-te__head { display: flex; align-items: center; gap: .75rem; margin-bottom: 1rem; flex-wrap: wrap; }
.kf-te__title { font-size: var(--text-xl); font-weight: 700; margin: 0; color: var(--text-primary); }
.kf-te__field { margin-bottom: 1.25rem; }
.kf-te__label { font-size: var(--text-sm); font-weight: 700; color: var(--text-secondary); display: block; margin-bottom: .4rem; }
.kf-te__tags { display: flex; gap: .4rem; flex-wrap: wrap; align-items: center; margin-bottom: .5rem; }
.kf-tabs2 { display: inline-flex; gap: 2px; border-bottom: 1px solid var(--border-subtle); margin-bottom: .75rem; }
.kf-tab2 { border: none; background: none; padding: .5rem .9rem; font-size: var(--text-sm); font-weight: 600; color: var(--text-muted); cursor: pointer; border-bottom: 2px solid transparent; margin-bottom: -1px; font-family: var(--font-sans); }
.kf-tab2:hover { color: var(--text-primary); }
.kf-tab2.is-active { color: var(--primary); border-bottom-color: var(--primary); }
.kf-md { border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 1rem 1.25rem; background: var(--surface-sunken); min-height: 200px; color: var(--text-primary); line-height: var(--leading-relaxed); }
.kf-md h1,.kf-md h2,.kf-md h3 { margin: .4rem 0; font-weight: 700; }
.kf-md h1 { font-size: var(--text-lg); } .kf-md h2 { font-size: var(--text-md); } .kf-md h3 { font-size: var(--text-base); }
.kf-md code { font-family: var(--font-mono); font-size: .85em; background: var(--surface-hover); padding: .1em .35em; border-radius: 4px; }
.kf-md ul { margin: .4rem 0; padding-left: 1.25rem; } .kf-md li { margin: .15rem 0; }
.kf-md p { margin: .4rem 0; } .kf-md__empty { color: var(--text-muted); font-style: italic; }
.kf-te__actions { display: flex; gap: .6rem; margin-top: 1.5rem; }
.kf-detailcard { margin-bottom: 1.25rem; }
`;

  // Minimal Markdown → HTML (headings, bold, italic, inline code, lists).
  function renderMd(src) {
    if (!src || !src.trim()) return '<p class="kf-md__empty">Aucun contenu à prévisualiser.</p>';
    const esc = s => s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    const inline = s => esc(s).replace(/`([^`]+)`/g, "<code>$1</code>").replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>").replace(/\*([^*]+)\*/g, "<em>$1</em>");
    const lines = src.split(/\n/);
    const out = [];
    let inList = false;
    for (const ln of lines) {
      const h = ln.match(/^(#{1,3})\s+(.*)$/);
      const li = ln.match(/^[-*]\s+(.*)$/);
      if (li) {
        if (!inList) {
          out.push("<ul>");
          inList = true;
        }
        out.push("<li>" + inline(li[1]) + "</li>");
        continue;
      }
      if (inList) {
        out.push("</ul>");
        inList = false;
      }
      if (h) {
        const lvl = h[1].length;
        out.push(`<h${lvl}>` + inline(h[2]) + `</h${lvl}>`);
      } else if (ln.trim()) out.push("<p>" + inline(ln) + "</p>");
    }
    if (inList) out.push("</ul>");
    return out.join("");
  }
  const MAXLEN = 5000;
  function TaskEditScreen({
    task,
    mode,
    onSave,
    onBack,
    onEdit
  }) {
    React.useEffect(() => {
      if (document.getElementById("kf-te-css")) return;
      const s = document.createElement("style");
      s.id = "kf-te-css";
      s.textContent = TE_CSS;
      document.head.appendChild(s);
    }, []);
    const [title, setTitle] = React.useState(task.title);
    const [desc, setDesc] = React.useState(task.description || "");
    const [tags, setTags] = React.useState(task.tags || []);
    const [tagInput, setTagInput] = React.useState("");
    const [tab, setTab] = React.useState("edit");
    const addTag = () => {
      const v = tagInput.trim();
      if (v && tags.length < 5 && !tags.includes(v)) setTags([...tags, v]);
      setTagInput("");
    };
    const over = desc.length > MAXLEN;
    if (mode === "detail") {
      return /*#__PURE__*/React.createElement("div", {
        className: "kf-te"
      }, /*#__PURE__*/React.createElement("div", {
        className: "kf-te__back"
      }, /*#__PURE__*/React.createElement(Button, {
        variant: "ghost",
        size: "sm",
        icon: /*#__PURE__*/React.createElement(ArrowLeft, {
          size: 16
        }),
        onClick: onBack
      }, "Retour")), /*#__PURE__*/React.createElement("div", {
        className: "kf-te__head"
      }, /*#__PURE__*/React.createElement("h1", {
        className: "kf-te__title"
      }, task.title), /*#__PURE__*/React.createElement(Badge, {
        status: task.status
      })), /*#__PURE__*/React.createElement("div", {
        className: "kf-te__tags",
        style: {
          marginBottom: "1rem"
        }
      }, (task.tags || []).map(t => /*#__PURE__*/React.createElement(Tag, {
        key: t,
        hash: true
      }, t))), /*#__PURE__*/React.createElement(Card, {
        className: "kf-detailcard",
        style: {
          background: "var(--surface-sunken)"
        }
      }, /*#__PURE__*/React.createElement("div", {
        className: "kf-md",
        style: {
          border: "none",
          padding: 0,
          background: "transparent",
          minHeight: 80
        },
        dangerouslySetInnerHTML: {
          __html: task.description ? renderMd(task.description) : '<p class="kf-md__empty">Aucune description.</p>'
        }
      })), /*#__PURE__*/React.createElement("div", {
        className: "kf-te__actions"
      }, task.status !== "Done" && /*#__PURE__*/React.createElement(Button, {
        variant: "primary",
        icon: /*#__PURE__*/React.createElement(Pencil, {
          size: 16
        }),
        onClick: () => onEdit(task)
      }, "Modifier"), /*#__PURE__*/React.createElement(Button, {
        variant: "secondary",
        onClick: onBack
      }, "Retour")));
    }
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-te"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-te__back"
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "ghost",
      size: "sm",
      icon: /*#__PURE__*/React.createElement(ArrowLeft, {
        size: 16
      }),
      onClick: onBack
    }, "Retour")), /*#__PURE__*/React.createElement("h1", {
      className: "kf-te__title",
      style: {
        marginBottom: "1.25rem"
      }
    }, "Modifier la t\xE2che"), /*#__PURE__*/React.createElement("div", {
      className: "kf-te__field"
    }, /*#__PURE__*/React.createElement(Input, {
      label: "Titre",
      autoFocus: true,
      value: title,
      onChange: e => setTitle(e.target.value),
      placeholder: "Titre\u2026"
    })), /*#__PURE__*/React.createElement("div", {
      className: "kf-te__field"
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-te__label"
    }, "Tags"), /*#__PURE__*/React.createElement("div", {
      className: "kf-te__tags"
    }, tags.map(t => /*#__PURE__*/React.createElement(Tag, {
      key: t,
      hash: true,
      onRemove: () => setTags(tags.filter(x => x !== t))
    }, t))), /*#__PURE__*/React.createElement("div", {
      style: {
        maxWidth: 320
      }
    }, /*#__PURE__*/React.createElement(Input, {
      placeholder: tags.length >= 5 ? "Maximum 5 tags" : "Ajouter un tag… (Entrée)",
      disabled: tags.length >= 5,
      value: tagInput,
      onChange: e => setTagInput(e.target.value),
      onKeyDown: e => e.key === "Enter" && addTag(),
      "aria-label": "Ajouter un tag"
    }))), /*#__PURE__*/React.createElement("div", {
      className: "kf-te__field"
    }, /*#__PURE__*/React.createElement("span", {
      className: "kf-te__label"
    }, "Description"), /*#__PURE__*/React.createElement("div", {
      className: "kf-tabs2",
      role: "tablist"
    }, /*#__PURE__*/React.createElement("button", {
      role: "tab",
      "aria-selected": tab === "edit",
      className: `kf-tab2 ${tab === "edit" ? "is-active" : ""}`,
      onClick: () => setTab("edit")
    }, "\xC9diter"), /*#__PURE__*/React.createElement("button", {
      role: "tab",
      "aria-selected": tab === "preview",
      className: `kf-tab2 ${tab === "preview" ? "is-active" : ""}`,
      onClick: () => setTab("preview")
    }, "Pr\xE9visualiser")), tab === "edit" ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("textarea", {
      className: "kf-md",
      style: {
        width: "100%",
        fontFamily: "var(--font-mono)",
        fontSize: "var(--text-sm)",
        resize: "vertical"
      },
      rows: 10,
      value: desc,
      onChange: e => setDesc(e.target.value),
      placeholder: "Description en Markdown\u2026",
      "aria-label": "Description Markdown"
    }), /*#__PURE__*/React.createElement("div", {
      style: {
        marginTop: ".35rem",
        fontSize: "var(--text-xs)",
        fontFamily: "var(--font-mono)",
        fontWeight: over ? 700 : 400,
        color: over ? "var(--danger)" : "var(--text-muted)"
      }
    }, desc.length, " / ", MAXLEN)) : /*#__PURE__*/React.createElement("div", {
      className: "kf-md",
      dangerouslySetInnerHTML: {
        __html: renderMd(desc)
      }
    })), /*#__PURE__*/React.createElement("div", {
      className: "kf-te__actions"
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      disabled: over || !title.trim(),
      onClick: () => onSave({
        ...task,
        title: title.trim(),
        description: desc.trim(),
        tags
      })
    }, "Enregistrer"), /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      onClick: onBack
    }, "Annuler")));
  }
  window.TaskEditScreen = TaskEditScreen;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/TaskEditScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/TasksScreen.jsx
try { (() => {
(() => {
  // Tasks — conventional checkbox to complete (Jakob), big targets (Fitts),
  // grouped metadata (Proximity), readable filters, optimistic add/complete.
  const {
    Input,
    Textarea,
    Select,
    Button,
    IconButton,
    Checkbox,
    Badge,
    Tag,
    Card
  } = window.KairuFocusDesignSystem_0d3740;
  const {
    Pencil,
    Cross,
    Search
  } = window.KFIcons;
  const TASKS_CSS = `
.kf-tasks { max-width: var(--container-lg); }
.kf-tasks h1 { font-size: var(--text-xl); font-weight: 700; margin: 0 0 1.25rem; color: var(--text-primary); }
.kf-addcard { margin-bottom: 1.5rem; }
.kf-addcard__row { display: grid; grid-template-columns: 1fr; gap: .75rem; }
.kf-addtags { display: flex; gap: .4rem; flex-wrap: wrap; align-items: center; }
.kf-filters { display: flex; gap: .75rem; margin-bottom: 1.25rem; }
.kf-filters__search { flex: 1; }
.kf-tasklist { display: flex; flex-direction: column; gap: .6rem; }
.kf-taskrow {
  display: flex; align-items: flex-start; gap: .85rem; padding: 1rem 1.25rem;
  background: var(--surface-card); border: 1px solid var(--border-subtle); border-radius: var(--radius-lg);
  transition: box-shadow var(--duration-base) var(--ease-standard), border-color var(--duration-base) var(--ease-standard);
}
.kf-taskrow:hover { box-shadow: var(--shadow-sm); border-color: var(--border-default); }
.kf-taskrow__body { flex: 1; min-width: 0; }
.kf-taskrow__title { font-size: var(--text-base); font-weight: 600; color: var(--text-primary); }
.kf-taskrow__title--done { text-decoration: line-through; color: var(--text-muted); font-weight: 400; }
.kf-taskrow__meta { display: flex; gap: .4rem; flex-wrap: wrap; align-items: center; margin-top: .5rem; }
.kf-taskrow__desc { font-size: var(--text-sm); color: var(--text-muted); margin-top: .35rem; }
.kf-taskrow__actions { display: flex; gap: .4rem; align-items: center; flex-shrink: 0; }
.kf-empty { text-align: center; padding: 3rem 1rem; color: var(--text-muted); }
.kf-empty__icon { font-size: 2.5rem; opacity: .5; }
.kf-empty__title { font-size: var(--text-md); font-weight: 600; color: var(--text-secondary); margin: .75rem 0 .35rem; }
`;
  function TaskRow({
    task,
    onComplete,
    onDelete,
    onOpen,
    onEdit
  }) {
    const done = task.status === "Done";
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-taskrow"
    }, /*#__PURE__*/React.createElement("div", {
      style: {
        paddingTop: 2
      }
    }, /*#__PURE__*/React.createElement(Checkbox, {
      checked: done,
      onChange: () => onComplete(task.id),
      "aria-label": `Compléter ${task.title}`
    })), /*#__PURE__*/React.createElement("div", {
      className: "kf-taskrow__body"
    }, /*#__PURE__*/React.createElement("button", {
      className: `kf-taskrow__title ${done ? "kf-taskrow__title--done" : ""}`,
      onClick: () => onOpen(task),
      style: {
        background: "none",
        border: "none",
        padding: 0,
        textAlign: "left",
        cursor: "pointer",
        font: "inherit",
        color: "inherit"
      }
    }, task.title), task.description ? /*#__PURE__*/React.createElement("div", {
      className: "kf-taskrow__desc"
    }, task.description) : null, /*#__PURE__*/React.createElement("div", {
      className: "kf-taskrow__meta"
    }, /*#__PURE__*/React.createElement(Badge, {
      status: task.status
    }), task.tags.map(t => /*#__PURE__*/React.createElement(Tag, {
      key: t,
      hash: true
    }, t)))), /*#__PURE__*/React.createElement("div", {
      className: "kf-taskrow__actions"
    }, !done && /*#__PURE__*/React.createElement(IconButton, {
      label: `Modifier ${task.title}`,
      tone: "primary",
      outline: true,
      onClick: () => onEdit(task)
    }, /*#__PURE__*/React.createElement(Pencil, {
      size: 16
    })), /*#__PURE__*/React.createElement(IconButton, {
      label: `Supprimer ${task.title}`,
      tone: "danger",
      outline: true,
      onClick: () => onDelete(task.id)
    }, /*#__PURE__*/React.createElement(Cross, {
      size: 15
    }))));
  }
  function TasksScreen({
    tasks,
    onAdd,
    onComplete,
    onDelete,
    onOpen,
    onEdit
  }) {
    React.useEffect(() => {
      if (document.getElementById("kf-tasks-css")) return;
      const s = document.createElement("style");
      s.id = "kf-tasks-css";
      s.textContent = TASKS_CSS;
      document.head.appendChild(s);
    }, []);
    const [title, setTitle] = React.useState("");
    const [desc, setDesc] = React.useState("");
    const [tags, setTags] = React.useState([]);
    const [tagInput, setTagInput] = React.useState("");
    const [search, setSearch] = React.useState("");
    const [filter, setFilter] = React.useState("OpenOnly");
    const addTag = () => {
      const v = tagInput.trim();
      if (v && tags.length < 5 && !tags.includes(v)) setTags([...tags, v]);
      setTagInput("");
    };
    const submit = () => {
      if (!title.trim()) return;
      onAdd({
        title: title.trim(),
        description: desc.trim(),
        tags
      });
      setTitle("");
      setDesc("");
      setTags([]);
      setTagInput("");
    };
    const visible = tasks.filter(t => {
      if (search && !t.title.toLowerCase().includes(search.toLowerCase())) return false;
      if (filter === "OpenOnly") return t.status !== "Done";
      if (filter === "Done") return t.status === "Done";
      if (filter === "InProgress") return t.status === "InProgress";
      if (filter === "Pending") return t.status === "Todo";
      return true;
    });
    return /*#__PURE__*/React.createElement("div", {
      className: "kf-tasks"
    }, /*#__PURE__*/React.createElement("h1", null, "T\xE2ches"), /*#__PURE__*/React.createElement(Card, {
      className: "kf-addcard"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-addcard__row"
    }, /*#__PURE__*/React.createElement(Input, {
      placeholder: "Titre de la t\xE2che\u2026",
      value: title,
      onChange: e => setTitle(e.target.value),
      onKeyDown: e => e.key === "Enter" && submit(),
      "aria-label": "Titre de la t\xE2che"
    }), /*#__PURE__*/React.createElement(Textarea, {
      placeholder: "Description (optionnelle)\u2026",
      rows: 2,
      maxLength: 120,
      value: desc,
      onChange: e => setDesc(e.target.value),
      "aria-label": "Description"
    }), /*#__PURE__*/React.createElement("div", {
      className: "kf-addtags"
    }, tags.map(t => /*#__PURE__*/React.createElement(Tag, {
      key: t,
      hash: true,
      onRemove: () => setTags(tags.filter(x => x !== t))
    }, t)), /*#__PURE__*/React.createElement("div", {
      style: {
        maxWidth: 220
      }
    }, /*#__PURE__*/React.createElement(Input, {
      placeholder: tags.length >= 5 ? "Maximum 5 tags" : "Ajouter un tag… (Entrée)",
      disabled: tags.length >= 5,
      value: tagInput,
      onChange: e => setTagInput(e.target.value),
      onKeyDown: e => e.key === "Enter" && addTag(),
      "aria-label": "Ajouter un tag"
    }))), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      disabled: !title.trim(),
      onClick: submit
    }, "Ajouter")))), /*#__PURE__*/React.createElement("div", {
      className: "kf-filters"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-filters__search"
    }, /*#__PURE__*/React.createElement(Input, {
      placeholder: "Rechercher par titre\u2026",
      value: search,
      onChange: e => setSearch(e.target.value),
      "aria-label": "Rechercher"
    })), /*#__PURE__*/React.createElement("div", {
      style: {
        width: 180
      }
    }, /*#__PURE__*/React.createElement(Select, {
      value: filter,
      onChange: e => setFilter(e.target.value),
      "aria-label": "Filtre de statut",
      options: [{
        value: "OpenOnly",
        label: "Ouvertes"
      }, {
        value: "All",
        label: "Toutes"
      }, {
        value: "Pending",
        label: "En attente"
      }, {
        value: "InProgress",
        label: "En cours"
      }, {
        value: "Done",
        label: "Terminées"
      }]
    }))), visible.length === 0 ? /*#__PURE__*/React.createElement("div", {
      className: "kf-empty"
    }, /*#__PURE__*/React.createElement("div", {
      className: "kf-empty__icon",
      "aria-hidden": "true"
    }, "\uD83D\uDCCB"), /*#__PURE__*/React.createElement("div", {
      className: "kf-empty__title"
    }, "Aucune t\xE2che ne correspond aux filtres."), /*#__PURE__*/React.createElement("div", null, "Ajoutez votre premi\xE8re t\xE2che ci-dessus pour commencer.")) : /*#__PURE__*/React.createElement("div", {
      className: "kf-tasklist"
    }, visible.map(t => /*#__PURE__*/React.createElement(TaskRow, {
      key: t.id,
      task: t,
      onComplete: onComplete,
      onDelete: onDelete,
      onOpen: onOpen,
      onEdit: onEdit
    }))));
  }
  window.TasksScreen = TasksScreen;
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/TasksScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/icons.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
// Shared icons for the KairuFocus UI kit.
// Feather/Lucide-style stroke SVG (stroke-width 2.5) — matches the app's
// hand-rolled nav/logout/github icons. Emoji are used elsewhere per brand.
const S = p => ({
  width: p.size || 20,
  height: p.size || 20,
  viewBox: "0 0 24 24",
  fill: "none",
  stroke: "currentColor",
  strokeWidth: p.sw || 2.5,
  strokeLinecap: "round",
  strokeLinejoin: "round",
  "aria-hidden": "true"
});
function Home(p = {}) {
  return /*#__PURE__*/React.createElement("svg", S(p), /*#__PURE__*/React.createElement("path", {
    d: "M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"
  }), /*#__PURE__*/React.createElement("polyline", {
    points: "9 22 9 12 15 12 15 22"
  }));
}
function Logout(p = {}) {
  return /*#__PURE__*/React.createElement("svg", S(p), /*#__PURE__*/React.createElement("path", {
    d: "M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"
  }), /*#__PURE__*/React.createElement("polyline", {
    points: "16 17 21 12 16 7"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "21",
    y1: "12",
    x2: "9",
    y2: "12"
  }));
}
function Pencil(p = {}) {
  return /*#__PURE__*/React.createElement("svg", S({
    ...p,
    sw: p.sw || 2.2
  }), /*#__PURE__*/React.createElement("path", {
    d: "M12 20h9"
  }), /*#__PURE__*/React.createElement("path", {
    d: "M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z"
  }));
}
function Check(p = {}) {
  return /*#__PURE__*/React.createElement("svg", S({
    ...p,
    sw: p.sw || 2.6
  }), /*#__PURE__*/React.createElement("polyline", {
    points: "20 6 9 17 4 12"
  }));
}
function Cross(p = {}) {
  return /*#__PURE__*/React.createElement("svg", S({
    ...p,
    sw: p.sw || 2.4
  }), /*#__PURE__*/React.createElement("line", {
    x1: "18",
    y1: "6",
    x2: "6",
    y2: "18"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "6",
    y1: "6",
    x2: "18",
    y2: "18"
  }));
}
function Plus(p = {}) {
  return /*#__PURE__*/React.createElement("svg", S(p), /*#__PURE__*/React.createElement("line", {
    x1: "12",
    y1: "5",
    x2: "12",
    y2: "19"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "5",
    y1: "12",
    x2: "19",
    y2: "12"
  }));
}
function ArrowLeft(p = {}) {
  return /*#__PURE__*/React.createElement("svg", S({
    ...p,
    sw: p.sw || 2
  }), /*#__PURE__*/React.createElement("line", {
    x1: "19",
    y1: "12",
    x2: "5",
    y2: "12"
  }), /*#__PURE__*/React.createElement("polyline", {
    points: "12 19 5 12 12 5"
  }));
}
function ArrowRight(p = {}) {
  return /*#__PURE__*/React.createElement("svg", S({
    ...p,
    sw: p.sw || 2
  }), /*#__PURE__*/React.createElement("line", {
    x1: "5",
    y1: "12",
    x2: "19",
    y2: "12"
  }), /*#__PURE__*/React.createElement("polyline", {
    points: "12 5 19 12 12 19"
  }));
}
function Search(p = {}) {
  return /*#__PURE__*/React.createElement("svg", S({
    ...p,
    sw: p.sw || 2
  }), /*#__PURE__*/React.createElement("circle", {
    cx: "11",
    cy: "11",
    r: "8"
  }), /*#__PURE__*/React.createElement("line", {
    x1: "21",
    y1: "21",
    x2: "16.65",
    y2: "16.65"
  }));
}
function Play(p = {}) {
  return /*#__PURE__*/React.createElement("svg", _extends({}, S(p), {
    fill: "currentColor",
    stroke: "none"
  }), /*#__PURE__*/React.createElement("polygon", {
    points: "6 4 20 12 6 20 6 4"
  }));
}
function Stop(p = {}) {
  return /*#__PURE__*/React.createElement("svg", _extends({}, S(p), {
    fill: "currentColor",
    stroke: "none"
  }), /*#__PURE__*/React.createElement("rect", {
    x: "6",
    y: "6",
    width: "12",
    height: "12",
    rx: "1.5"
  }));
}
function Github(p = {}) {
  return /*#__PURE__*/React.createElement("svg", {
    width: p.size || 20,
    height: p.size || 20,
    viewBox: "0 0 24 24",
    fill: "currentColor",
    "aria-hidden": "true"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0 0 24 12c0-6.63-5.37-12-12-12z"
  }));
}
window.KFIcons = {
  Home,
  Logout,
  Pencil,
  Check,
  Cross,
  Plus,
  ArrowLeft,
  ArrowRight,
  Search,
  Play,
  Stop,
  Github
};
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/icons.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kairufocus-web/mockData.jsx
try { (() => {
// Shared mock data for the UI kit.
window.KFData = {
  user: {
    name: "dev",
    login: "kairu-dev"
  },
  focus: {
    focusMinutes: 85,
    focusGoalMinutes: 120,
    sprints: 3,
    sprintGoal: 4,
    streak: 5
  },
  week: [{
    d: "Jeu",
    n: 4
  }, {
    d: "Ven",
    n: 6
  }, {
    d: "Sam",
    n: 2
  }, {
    d: "Dim",
    n: 0
  }, {
    d: "Lun",
    n: 5
  }, {
    d: "Mar",
    n: 7
  }, {
    d: "Auj",
    n: 3
  }],
  // 30 derniers jours (du plus ancien au plus récent ; dernier = aujourd'hui)
  month: [2, 4, 3, 0, 0, 5, 6, 3, 4, 2, 1, 0, 4, 5, 6, 3, 0, 0, 2, 4, 5, 7, 3, 4, 2, 0, 1, 5, 6, 3],
  initialTasks: [{
    id: "t1",
    title: "Refactor du handler d'authentification JWT",
    status: "InProgress",
    tags: ["backend", "auth"],
    description: "Extraire la logique JWT dans son propre service."
  }, {
    id: "t2",
    title: "Écrire les tests d'intégration du JournalApiClient",
    status: "Todo",
    tags: ["tests"],
    description: ""
  }, {
    id: "t3",
    title: "Corriger le focus clavier sur la NavMenu",
    status: "Todo",
    tags: ["a11y", "ui"],
    description: "Ajouter un aria-label et un focus visible."
  }, {
    id: "t4",
    title: "Centraliser les design tokens (light/dark)",
    status: "InProgress",
    tags: ["design-system"],
    description: "Couleurs, typo, spacing, radius, shadow, motion."
  }, {
    id: "t5",
    title: "Mettre à jour la doc OAuth GitHub",
    status: "Done",
    tags: ["docs"],
    description: ""
  }, {
    id: "t6",
    title: "Skeleton loaders sur le Dashboard",
    status: "Done",
    tags: ["ui", "perf"],
    description: ""
  }],
  journal: [{
    id: "j1",
    time: "09:12",
    type: "SprintStarted",
    icon: "🍅",
    variant: "sprint",
    label: "Sprint #1 démarré",
    tasks: [{
      title: "Refactor du handler d'authentification JWT",
      tags: ["backend"]
    }]
  }, {
    id: "j2",
    time: "09:37",
    type: "SprintCompleted",
    icon: "✅",
    variant: "sprint",
    label: "Sprint #1 complété",
    tasks: []
  }, {
    id: "j3",
    time: "09:38",
    type: "BreakStarted",
    icon: "☕",
    variant: "break",
    label: "Pause #1 démarrée",
    tasks: []
  }, {
    id: "j4",
    time: "09:43",
    type: "TaskCompleted",
    icon: "🎉",
    variant: "task",
    label: "Tâche complétée",
    tasks: [{
      title: "Skeleton loaders sur le Dashboard",
      tags: ["ui"]
    }],
    comment: "Enfin ! Beaucoup plus fluide au chargement."
  }, {
    id: "j5",
    time: "10:05",
    type: "SprintStarted",
    icon: "🍅",
    variant: "sprint",
    label: "Sprint #2 démarré",
    tasks: [{
      title: "Centraliser les design tokens",
      tags: ["design-system"]
    }]
  }, {
    id: "j6",
    time: "10:30",
    type: "SprintInterrupted",
    icon: "⏸️",
    variant: "warn",
    label: "Sprint #2 interrompu",
    tasks: []
  }]
};
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kairufocus-web/mockData.jsx", error: String((e && e.message) || e) }); }

__ds_ns.Button = __ds_scope.Button;

__ds_ns.IconButton = __ds_scope.IconButton;

__ds_ns.Avatar = __ds_scope.Avatar;

__ds_ns.Badge = __ds_scope.Badge;

__ds_ns.Card = __ds_scope.Card;

__ds_ns.StatCard = __ds_scope.StatCard;

__ds_ns.Tag = __ds_scope.Tag;

__ds_ns.Alert = __ds_scope.Alert;

__ds_ns.ProgressRing = __ds_scope.ProgressRing;

__ds_ns.Skeleton = __ds_scope.Skeleton;

__ds_ns.Checkbox = __ds_scope.Checkbox;

__ds_ns.Input = __ds_scope.Input;

__ds_ns.Select = __ds_scope.Select;

__ds_ns.Textarea = __ds_scope.Textarea;

})();
