(() => {
// Tasks — conventional checkbox to complete (Jakob), big targets (Fitts),
// grouped metadata (Proximity), readable filters, optimistic add/complete.
const { Input, Textarea, Select, Button, IconButton, Checkbox, Badge, Tag, Card } = window.KairuFocusDesignSystem_0d3740;
const { Pencil, Cross, Search } = window.KFIcons;

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

function TaskRow({ task, onComplete, onDelete, onOpen, onEdit }) {
  const done = task.status === "Done";
  return (
    <div className="kf-taskrow">
      <div style={{ paddingTop: 2 }}>
        <Checkbox checked={done} onChange={() => onComplete(task.id)} aria-label={`Compléter ${task.title}`} />
      </div>
      <div className="kf-taskrow__body">
        <button className={`kf-taskrow__title ${done ? "kf-taskrow__title--done" : ""}`} onClick={() => onOpen(task)} style={{ background: "none", border: "none", padding: 0, textAlign: "left", cursor: "pointer", font: "inherit", color: "inherit" }}>{task.title}</button>
        {task.description ? <div className="kf-taskrow__desc">{task.description}</div> : null}
        <div className="kf-taskrow__meta">
          <Badge status={task.status} />
          {task.tags.map((t) => <Tag key={t} hash>{t}</Tag>)}
        </div>
      </div>
      <div className="kf-taskrow__actions">
        {!done && <IconButton label={`Modifier ${task.title}`} tone="primary" outline onClick={() => onEdit(task)}><Pencil size={16} /></IconButton>}
        <IconButton label={`Supprimer ${task.title}`} tone="danger" outline onClick={() => onDelete(task.id)}><Cross size={15} /></IconButton>
      </div>
    </div>
  );
}

function TasksScreen({ tasks, onAdd, onComplete, onDelete, onOpen, onEdit }) {
  React.useEffect(() => {
    if (document.getElementById("kf-tasks-css")) return;
    const s = document.createElement("style"); s.id = "kf-tasks-css"; s.textContent = TASKS_CSS; document.head.appendChild(s);
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
    onAdd({ title: title.trim(), description: desc.trim(), tags });
    setTitle(""); setDesc(""); setTags([]); setTagInput("");
  };

  const visible = tasks.filter((t) => {
    if (search && !t.title.toLowerCase().includes(search.toLowerCase())) return false;
    if (filter === "OpenOnly") return t.status !== "Done";
    if (filter === "Done") return t.status === "Done";
    if (filter === "InProgress") return t.status === "InProgress";
    if (filter === "Pending") return t.status === "Todo";
    return true;
  });

  return (
    <div className="kf-tasks">
      <h1>Tâches</h1>

      <Card className="kf-addcard">
        <div className="kf-addcard__row">
          <Input placeholder="Titre de la tâche…" value={title} onChange={(e) => setTitle(e.target.value)} onKeyDown={(e) => e.key === "Enter" && submit()} aria-label="Titre de la tâche" />
          <Textarea placeholder="Description (optionnelle)…" rows={2} maxLength={120} value={desc} onChange={(e) => setDesc(e.target.value)} aria-label="Description" />
          <div className="kf-addtags">
            {tags.map((t) => <Tag key={t} hash onRemove={() => setTags(tags.filter((x) => x !== t))}>{t}</Tag>)}
            <div style={{ maxWidth: 220 }}>
              <Input placeholder={tags.length >= 5 ? "Maximum 5 tags" : "Ajouter un tag… (Entrée)"} disabled={tags.length >= 5} value={tagInput} onChange={(e) => setTagInput(e.target.value)} onKeyDown={(e) => e.key === "Enter" && addTag()} aria-label="Ajouter un tag" />
            </div>
          </div>
          <div><Button variant="primary" disabled={!title.trim()} onClick={submit}>Ajouter</Button></div>
        </div>
      </Card>

      <div className="kf-filters">
        <div className="kf-filters__search">
          <Input placeholder="Rechercher par titre…" value={search} onChange={(e) => setSearch(e.target.value)} aria-label="Rechercher" />
        </div>
        <div style={{ width: 180 }}>
          <Select value={filter} onChange={(e) => setFilter(e.target.value)} aria-label="Filtre de statut"
            options={[{value:"OpenOnly",label:"Ouvertes"},{value:"All",label:"Toutes"},{value:"Pending",label:"En attente"},{value:"InProgress",label:"En cours"},{value:"Done",label:"Terminées"}]} />
        </div>
      </div>

      {visible.length === 0 ? (
        <div className="kf-empty">
          <div className="kf-empty__icon" aria-hidden="true">📋</div>
          <div className="kf-empty__title">Aucune tâche ne correspond aux filtres.</div>
          <div>Ajoutez votre première tâche ci-dessus pour commencer.</div>
        </div>
      ) : (
        <div className="kf-tasklist">
          {visible.map((t) => <TaskRow key={t.id} task={t} onComplete={onComplete} onDelete={onDelete} onOpen={onOpen} onEdit={onEdit} />)}
        </div>
      )}
    </div>
  );
}

window.TasksScreen = TasksScreen;
})();
