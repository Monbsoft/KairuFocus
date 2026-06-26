# KairuFocus — Web UI Kit

A high-fidelity, click-through recreation of the **KairuFocus** Blazor WebAssembly web app, rebuilt with this design system's tokens and components. It is a *recreation*, not production code — interactions are faked, but the layout, color, type, spacing and motion match the real app (`Monbsoft/KairuFocus`, `src/KairuFocus.Web/`).

## Run it

Open `index.html`. It loads the compiled `_ds_bundle.js` and the screen modules, then boots an interactive app.

## Flow

1. **Landing** (`/`) — navy hero, single GitHub CTA (Von Restorff), 4 homogeneous feature blocks. Click **Se connecter avec GitHub** to enter the app.
2. **Dashboard** (`/dashboard`) — skeleton load (Doherty), then: an **À reprendre** resume block (Zeigarnik), a **day-progress** bar (Goal-Gradient), grouped secondary stats, and reduced quick links (Hick).
3. **Tâches** (`/tasks`) — add a task, complete it via the conventional checkbox (Jakob, strike-through), delete it; grouped metadata + hashed tags (Proximity); readable filters; pedagogical empty state. Click a title → **detail**, the pencil → **edit** (Éditer/Prévisualiser Markdown tabs + character counter).
4. **Pomodoro** (`/pomodoro`) — session-type tabs, a smooth state-colored ring (Goal-Gradient), big primary controls (Fitts), **Mode focus** (Flow), linked sprint tasks, celebratory end (Peak-End). The **Sprint libre** link opens the open-ended count-up timer (`/pomodoro/libre`) with note, linked task and today's history.
5. **Journal** (`/journal`) — the notebook timeline (dots + lines = uniform connectedness), "Aujourd'hui" emphasised, day navigation, empty state.
6. **Paramètres** (`/settings`) — grouped sections (Common Region + chunking), save-confirmation toast.
7. **Login** (`/login`) — focused card, single action; reached by **logout**, mirrors `Login.razor`.

Use the sidebar to move between screens, the **🌙 / ☀️** button in the top row to flip light/dark — every screen is verified in both themes.

## Files

| File | Role |
|------|------|
| `index.html` | App boot + simple state routing (the `@dsCard` / starting-point entry). |
| `AppShell.jsx` | 64px sidebar rail (Jakob) + top row + content; active-item highlight (Von Restorff). |
| `icons.jsx` | Shared Feather/Lucide-style stroke SVGs (`window.KFIcons`). |
| `mockData.jsx` | Seed tasks + journal entries (`window.KFData`). |
| `LandingScreen.jsx`, `LoginScreen.jsx`, `DashboardScreen.jsx`, `TasksScreen.jsx`, `TaskEditScreen.jsx`, `PomodoroScreen.jsx`, `SprintLibreScreen.jsx`, `JournalScreen.jsx`, `SettingsScreen.jsx` | The surfaces. |

Each screen composes design-system primitives (`Button`, `IconButton`, `Card`, `StatCard`, `Badge`, `Tag`, `Input`, `Select`, `Checkbox`, `Alert`, `ProgressRing`, `Skeleton`) — it does not re-implement them.

## Scope

Covers the nine surfaces with real source pages. New surfaces should compose the same design-system primitives rather than re-implement them.
