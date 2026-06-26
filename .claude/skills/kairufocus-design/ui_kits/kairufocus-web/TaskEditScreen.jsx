(() => {
// Task detail + edit — reading hierarchy, Édit/Preview tabs (Markdown),
// visible character counter, focus management. Mirrors TaskDetail/TaskEdit.razor.
const { Input, Textarea, Button, Badge, Tag, Card, Alert } = window.KairuFocusDesignSystem_0d3740;
const { ArrowLeft, Pencil } = window.KFIcons;

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
  const esc = (s) => s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
  const inline = (s) => esc(s)
    .replace(/`([^`]+)`/g, "<code>$1</code>")
    .replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>")
    .replace(/\*([^*]+)\*/g, "<em>$1</em>");
  const lines = src.split(/\n/); const out = []; let inList = false;
  for (const ln of lines) {
    const h = ln.match(/^(#{1,3})\s+(.*)$/);
    const li = ln.match(/^[-*]\s+(.*)$/);
    if (li) { if (!inList) { out.push("<ul>"); inList = true; } out.push("<li>" + inline(li[1]) + "</li>"); continue; }
    if (inList) { out.push("</ul>"); inList = false; }
    if (h) { const lvl = h[1].length; out.push(`<h${lvl}>` + inline(h[2]) + `</h${lvl}>`); }
    else if (ln.trim()) out.push("<p>" + inline(ln) + "</p>");
  }
  if (inList) out.push("</ul>");
  return out.join("");
}

const MAXLEN = 5000;

function TaskEditScreen({ task, mode, onSave, onBack, onEdit }) {
  React.useEffect(() => {
    if (document.getElementById("kf-te-css")) return;
    const s = document.createElement("style"); s.id = "kf-te-css"; s.textContent = TE_CSS; document.head.appendChild(s);
  }, []);
  const [title, setTitle] = React.useState(task.title);
  const [desc, setDesc] = React.useState(task.description || "");
  const [tags, setTags] = React.useState(task.tags || []);
  const [tagInput, setTagInput] = React.useState("");
  const [tab, setTab] = React.useState("edit");

  const addTag = () => { const v = tagInput.trim(); if (v && tags.length < 5 && !tags.includes(v)) setTags([...tags, v]); setTagInput(""); };
  const over = desc.length > MAXLEN;

  if (mode === "detail") {
    return (
      <div className="kf-te">
        <div className="kf-te__back"><Button variant="ghost" size="sm" icon={<ArrowLeft size={16} />} onClick={onBack}>Retour</Button></div>
        <div className="kf-te__head">
          <h1 className="kf-te__title">{task.title}</h1>
          <Badge status={task.status} />
        </div>
        <div className="kf-te__tags" style={{ marginBottom: "1rem" }}>{(task.tags || []).map((t) => <Tag key={t} hash>{t}</Tag>)}</div>
        <Card className="kf-detailcard" style={{ background: "var(--surface-sunken)" }}>
          <div className="kf-md" style={{ border: "none", padding: 0, background: "transparent", minHeight: 80 }}
               dangerouslySetInnerHTML={{ __html: task.description ? renderMd(task.description) : '<p class="kf-md__empty">Aucune description.</p>' }} />
        </Card>
        <div className="kf-te__actions">
          {task.status !== "Done" && <Button variant="primary" icon={<Pencil size={16} />} onClick={() => onEdit(task)}>Modifier</Button>}
          <Button variant="secondary" onClick={onBack}>Retour</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="kf-te">
      <div className="kf-te__back"><Button variant="ghost" size="sm" icon={<ArrowLeft size={16} />} onClick={onBack}>Retour</Button></div>
      <h1 className="kf-te__title" style={{ marginBottom: "1.25rem" }}>Modifier la tâche</h1>

      <div className="kf-te__field">
        <Input label="Titre" autoFocus value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Titre…" />
      </div>

      <div className="kf-te__field">
        <span className="kf-te__label">Tags</span>
        <div className="kf-te__tags">
          {tags.map((t) => <Tag key={t} hash onRemove={() => setTags(tags.filter((x) => x !== t))}>{t}</Tag>)}
        </div>
        <div style={{ maxWidth: 320 }}>
          <Input placeholder={tags.length >= 5 ? "Maximum 5 tags" : "Ajouter un tag… (Entrée)"} disabled={tags.length >= 5} value={tagInput} onChange={(e) => setTagInput(e.target.value)} onKeyDown={(e) => e.key === "Enter" && addTag()} aria-label="Ajouter un tag" />
        </div>
      </div>

      <div className="kf-te__field">
        <span className="kf-te__label">Description</span>
        <div className="kf-tabs2" role="tablist">
          <button role="tab" aria-selected={tab === "edit"} className={`kf-tab2 ${tab === "edit" ? "is-active" : ""}`} onClick={() => setTab("edit")}>Éditer</button>
          <button role="tab" aria-selected={tab === "preview"} className={`kf-tab2 ${tab === "preview" ? "is-active" : ""}`} onClick={() => setTab("preview")}>Prévisualiser</button>
        </div>
        {tab === "edit" ? (
          <>
            <textarea className="kf-md" style={{ width: "100%", fontFamily: "var(--font-mono)", fontSize: "var(--text-sm)", resize: "vertical" }} rows={10} value={desc} onChange={(e) => setDesc(e.target.value)} placeholder="Description en Markdown…" aria-label="Description Markdown" />
            <div style={{ marginTop: ".35rem", fontSize: "var(--text-xs)", fontFamily: "var(--font-mono)", fontWeight: over ? 700 : 400, color: over ? "var(--danger)" : "var(--text-muted)" }}>{desc.length} / {MAXLEN}</div>
          </>
        ) : (
          <div className="kf-md" dangerouslySetInnerHTML={{ __html: renderMd(desc) }} />
        )}
      </div>

      <div className="kf-te__actions">
        <Button variant="primary" disabled={over || !title.trim()} onClick={() => onSave({ ...task, title: title.trim(), description: desc.trim(), tags })}>Enregistrer</Button>
        <Button variant="secondary" onClick={onBack}>Annuler</Button>
      </div>
    </div>
  );
}

window.TaskEditScreen = TaskEditScreen;
})();
