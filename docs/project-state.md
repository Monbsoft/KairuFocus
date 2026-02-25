# Kairudev — État du projet

> Ce fichier est mis à jour après chaque itération.
> Il est lu par Claude au démarrage de chaque session.

---

## Itérations

| # | Contenu | Statut | Date |
|---|---|---|---|
| ~~#1~~ | ~~BC Tasks — Domain, Application, Adapters, Infrastructure, SQLite, 23 tests~~ | ~~✅ Livré~~ | ~~2026-02-24~~ |
| ~~#2~~ | ~~API REST (ASP.NET Core) + UI Blazor WebAssembly~~ | ~~✅ Livré~~ | ~~2026-02-24~~ |
| ~~#2b~~ | ~~Réécriture spec.md (use cases + diagrammes Mermaid) + prompts agents~~ | ~~✅ Livré~~ | ~~2026-02-24~~ |
| ~~#3~~ | ~~BC Pomodoro — sessions de focus, chrono circulaire, lien avec Tasks~~ | ~~✅ Livré~~ | ~~2026-02-25~~ |
| #4 | Tests d'intégration SQLite (`Kairudev.Infrastructure.Tests`) | 📋 Planifié | — |
| #5 | Configuration externalisée — URL API via `appsettings.json` | 📋 Planifié | — |
| #6 | BC Journal — log d'activité quotidien alimenté par les sprints | 📋 Planifié | — |
| #7 | BC Tickets — intégration Jira / Linear / GitHub Issues | 📋 Planifié | — |
| #8 | .NET MAUI — application desktop/mobile | 📋 Planifié | — |

---

## Dernière itération livrée

**#3 — BC Pomodoro** — Livré le 2026-02-25

### Ce qui a été livré
- **Domain** : `PomodoroSession` (agrégat), `PomodoroSettings` (value object), `PomodoroSessionId`, `PomodoroSessionStatus`, `IPomodoroSessionRepository`, `IPomodoroSettingsRepository`, `DomainErrors.Pomodoro`
- **Application** : 9 use cases — UC-05 (SaveSettings), UC-06 (StartSession), UC-07 (LinkTask), UC-08 (UpdateTaskStatus), UC-09 (CreateTaskDuringSession), UC-10 (InterruptSession), UC-11 (CompleteSession) + GetSettings + GetCurrentSession
- **Infrastructure** : EF config (`PomodoroSessionConfiguration`, `PomodoroSettingsConfiguration`), singleton row (`PomodoroSettingsRow`), 2 repositories SQLite, migration `AddPomodoro`
- **API** : `PomodoroController` (9 endpoints REST) + 9 Presenters HTTP
- **Web** : `PomodoroApiClient`, `PomodoroDto`, `Pomodoro.razor` (horloge SVG circulaire + `PeriodicTimer` côté client, gestion tâches liées)
- **Tests** : 35 Domain + 20 Application = **55 tests, 0 échec**
- **ADR-006** : timer côté client (PeriodicTimer Blazor WASM → PATCH /complete à zéro)
- **ADR-007** : `Kairudev.Adapters` éliminé — ViewModels et presenters non-HTTP dans Application

### Dette technique héritée et courante
- URL API hardcodée dans `Program.cs` du Web (`https://localhost:7056`) → itération #5
- Pas de tests d'intégration SQLite (`Kairudev.Infrastructure.Tests` vide) → itération #4
- `TaskStatus` : alias `DomainTaskStatus` nécessaire dans les tests (conflit namespace)
- `DomainErrors` : alias `PomodoroErrors` nécessaire quand Tasks et Pomodoro sont tous deux importés
- `DeveloperTask.StartProgress()` codé dans le domaine, pas exposé en UC ni en endpoint
- `Kairudev.Adapters` : projet toujours présent dans la solution mais vide de sens (suppression à planifier)

---

## Stack technique
- .NET 10 (preview)
- SQLite + EF Core 10 (fichier local `kairudev.db`, hors git)
- ASP.NET Core Web API (`Kairudev.Api`) — `https://localhost:7056`
- Blazor WebAssembly (`Kairudev.Web`) — `https://localhost:7204`
- .NET MAUI — itération future
- xUnit pour les tests

## Structure du projet
```
src/
├── Kairudev.Domain/
│   ├── Tasks/
│   └── Pomodoro/
├── Kairudev.Application/
│   ├── Tasks/
│   └── Pomodoro/
├── Kairudev.Adapters/          ← à supprimer (ADR-007)
├── Kairudev.Infrastructure/    ← migrations dans Persistence/Migrations/
├── Kairudev.Api/               ← Tasks/ + Pomodoro/
└── Kairudev.Web/               ← Services/ + Pages/
tests/
├── Kairudev.Domain.Tests/      ← Tasks/ + Pomodoro/
├── Kairudev.Application.Tests/ ← Tasks/ + Pomodoro/
└── Kairudev.Infrastructure.Tests/  ← vide, à compléter (#4)
docs/
├── spec.md
└── project-state.md
```
