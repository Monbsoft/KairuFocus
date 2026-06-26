// Shared icons for the KairuFocus UI kit.
// Feather/Lucide-style stroke SVG (stroke-width 2.5) — matches the app's
// hand-rolled nav/logout/github icons. Emoji are used elsewhere per brand.
const S = (p) => ({ width: p.size || 20, height: p.size || 20, viewBox: "0 0 24 24", fill: "none", stroke: "currentColor", strokeWidth: p.sw || 2.5, strokeLinecap: "round", strokeLinejoin: "round", "aria-hidden": "true" });

function Home(p={})   { return <svg {...S(p)}><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>; }
function Logout(p={}) { return <svg {...S(p)}><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>; }
function Pencil(p={}) { return <svg {...S({...p, sw: p.sw||2.2})}><path d="M12 20h9"/><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z"/></svg>; }
function Check(p={})  { return <svg {...S({...p, sw: p.sw||2.6})}><polyline points="20 6 9 17 4 12"/></svg>; }
function Cross(p={})  { return <svg {...S({...p, sw: p.sw||2.4})}><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>; }
function Plus(p={})   { return <svg {...S(p)}><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>; }
function ArrowLeft(p={})  { return <svg {...S({...p, sw: p.sw||2})}><line x1="19" y1="12" x2="5" y2="12"/><polyline points="12 19 5 12 12 5"/></svg>; }
function ArrowRight(p={}) { return <svg {...S({...p, sw: p.sw||2})}><line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/></svg>; }
function Search(p={}) { return <svg {...S({...p, sw: p.sw||2})}><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>; }
function Play(p={})   { return <svg {...S(p)} fill="currentColor" stroke="none"><polygon points="6 4 20 12 6 20 6 4"/></svg>; }
function Stop(p={})   { return <svg {...S(p)} fill="currentColor" stroke="none"><rect x="6" y="6" width="12" height="12" rx="1.5"/></svg>; }
function Github(p={}) { return <svg width={p.size||20} height={p.size||20} viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0 0 24 12c0-6.63-5.37-12-12-12z"/></svg>; }

window.KFIcons = { Home, Logout, Pencil, Check, Cross, Plus, ArrowLeft, ArrowRight, Search, Play, Stop, Github };
