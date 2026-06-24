(() => {
// Journal — activity timeline (connexion uniforme: dots + lines), "today"
// emphasised, day navigation, pedagogical empty state.
const { Tag, IconButton } = window.KairuFocusDesignSystem_0d3740;
const { ArrowLeft, ArrowRight } = window.KFIcons;

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

function JournalScreen({ entries, dayOffset, onPrev, onNext }) {
  React.useEffect(() => {
    if (document.getElementById("kf-nb-css")) return;
    const s = document.createElement("style"); s.id = "kf-nb-css"; s.textContent = NB_CSS; document.head.appendChild(s);
  }, []);
  const isToday = dayOffset === 0;
  const base = new Date(); base.setDate(base.getDate() + dayOffset);
  const day = base.toLocaleDateString("fr-FR", { weekday: "long" }).toUpperCase();
  const date = base.toLocaleDateString("fr-FR", { day: "numeric", month: "long", year: "numeric" });
  const list = isToday ? entries : [];

  return (
    <div className="kf-nb">
      <div className="kf-nb__header">
        <button className="kf-nb__navbtn" aria-label="Jour précédent" onClick={onPrev}><ArrowLeft size={16} /></button>
        <div className="kf-nb__title">
          <div className="kf-nb__day">{day}</div>
          <div className="kf-nb__date">{date}</div>
          {isToday && <span className="kf-nb__today">Aujourd'hui</span>}
        </div>
        <button className="kf-nb__navbtn" aria-label="Jour suivant" disabled={isToday} onClick={onNext}><ArrowRight size={16} /></button>
      </div>
      <div className="kf-nb__body">
        {list.length === 0 ? (
          <div className="kf-nb__empty">
            <div className="kf-nb__empty-icon" aria-hidden="true">📓</div>
            <p style={{ fontStyle: "italic", margin: "0 0 .25rem" }}>Aucune activité enregistrée ce jour.</p>
            {isToday && <p style={{ fontSize: ".82rem", margin: 0 }}>Démarrez un sprint Pomodoro pour voir apparaître vos entrées ici.</p>}
          </div>
        ) : (
          <div className="kf-nb__timeline">
            {list.map((e) => (
              <div className={`kf-nb__entry ${e.variant}`} key={e.id}>
                <div className="kf-nb__time">{e.time}</div>
                <div className="kf-nb__dotcol"><div className="kf-nb__dot" /><div className="kf-nb__line" /></div>
                <div className="kf-nb__content">
                  <div className="kf-nb__event"><span className="kf-nb__icon" aria-hidden="true">{e.icon}</span><span className="kf-nb__elabel">{e.label}</span></div>
                  {e.tasks && e.tasks.length > 0 && (
                    <div className="kf-nb__tasks">
                      <span className="kf-nb__tasks-h">Tâches travaillées</span>
                      <ul className="kf-nb__tasks-list">
                        {e.tasks.map((t, i) => (
                          <li key={i}><span>{t.title}</span>{t.tags.map((tag) => <Tag key={tag} hash>{tag}</Tag>)}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                  {e.comment && (
                    <div className="kf-nb__comment"><div className="kf-nb__comment-body"><span>✏️ {e.comment}</span></div></div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

window.JournalScreen = JournalScreen;
})();
