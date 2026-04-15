# 🚀 Kairu

> Application de gestion d'activité quotidienne pour développeurs.

Kairu centralise en un seul endroit tout ce dont un développeur a besoin pour rester focus et organisé : todo list de micro-tâches, journal de bord, sessions Pomodoro et intégration avec les outils de ticketing (Jira).

> 🔄 **Renommage à venir** — Ce projet sera renommé en **KairuFocus** lors de la prochaine itération pour s'aligner avec le domaine de production [kairufocus.com](https://kairufocus.com). Le renommage concernera le dépôt GitHub (`Kairu` → `KairuFocus`) ainsi que les namespaces et projets (`Kairu.*` → `KairuFocus.*`).

---

## ✨ Fonctionnalités

| Module | Description |
|--------|-------------|
| ☑️ **Tasks** | Micro-tâches quotidiennes — ajout, modification, statut, lien Jira |
| 🍅 **Pomodoro** | Sessions de focus (sprint / pause courte / pause longue) avec chronomètre circulaire |
| 📖 **Journal** | Timeline d'activité quotidienne générée automatiquement + commentaires personnels |
| 🎫 **Tickets** | Intégration Jira Cloud — liste des tickets assignés, liaison avec les tâches |
| ⚙️ **Settings** | Thème clair/sombre/système, durées Pomodoro, credentials Jira |
| 🔐 **Auth** | Connexion via GitHub OAuth 2.0, JWT HS256, multi-utilisateurs |

---

## 🛠️ Stack technique

- **Runtime** : .NET 10
- **API** : ASP.NET Core Web API
- **Web** : Blazor WebAssembly
- **Mobile/Desktop** : .NET MAUI (Blazor Hybrid) — Windows, Android, iOS, macOS
- **Orchestration** : .NET Aspire 13.1
- **Base de données** : SQLite + EF Core 10
- **Auth** : GitHub OAuth 2.0 + JWT HS256
- **Tests** : xUnit — 192 tests (Domain + Application)

---

## 🏛️ Architecture

Clean Architecture (Uncle Bob) + DDD, avec CQRS sans MediatR.

```
Domain          →  Entités, Value Objects, règles métier
Application     →  Commands / Queries / Handlers (CQRS)
Infrastructure  →  EF Core, SQLite, Jira API client
Api             →  Controllers ASP.NET Core, Auth JWT
Web / Maui      →  UI Blazor (WASM + Hybrid)
```

> 📐 **Règle fondamentale** : les dépendances ne pointent que vers l'intérieur. Le Domain ne connaît rien des couches externes.

```
src/
├── Kairu.Domain/          # 🧠 Bounded Contexts : Tasks, Pomodoro, Journal, Settings, Identity
├── Kairu.Application/     # ⚡ 30 use cases CQRS
├── Kairu.Infrastructure/  # 🗄️ EF Core + migrations + repositories
├── Kairu.Api/             # 🌐 REST API + GitHub OAuth + JWT
├── Kairu.Web/             # 💻 Blazor WASM
├── Kairu.Maui/            # 📱 .NET MAUI Blazor Hybrid
├── Kairu.AppHost/         # 🔭 Aspire orchestration
└── Kairu.ServiceDefaults/ # 📊 OpenTelemetry, health checks
tests/
├── Kairu.Domain.Tests/
├── Kairu.Application.Tests/
└── Kairu.Infrastructure.Tests/
docs/
├── spec.md                   # 📋 Spécification complète + ADR
└── project-state.md          # 📍 État de l'itération courante
```

---

## ⚡ Démarrage rapide

### Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Une [GitHub OAuth App](https://github.com/settings/developers) (voir ci-dessous)

### 1️⃣ Configurer GitHub OAuth

Crée une OAuth App sur [github.com/settings/developers](https://github.com/settings/developers) :

| Champ | Valeur |
|-------|--------|
| Homepage URL | `http://localhost:5010` |
| Authorization callback URL | `http://localhost:5205/signin-github` |

Puis renseigne les credentials dans `src/Kairu.Api/appsettings.Development.json` :

```json
{
  "GitHub": {
    "ClientId": "<ton-client-id>",
    "ClientSecret": "<ton-client-secret>"
  },
  "WebBaseUrl": "http://localhost:5010",
  "Jwt": {
    "SecretKey": "une-clé-secrète-de-minimum-32-caractères",
    "ExpiryHours": 24
  }
}
```

> ⚠️ Ne jamais committer ce fichier avec de vraies valeurs.

### 2️⃣ Lancer l'application

**Option A — Aspire (recommandé)** : démarre API + Web + dashboard observabilité

```bash
dotnet run --project src/Kairu.AppHost/Kairu.AppHost.csproj --launch-profile http
```

| Service | URL |
|---------|-----|
| 🔭 Dashboard Aspire | `http://localhost:15100` |
| 💻 Web app | `http://localhost:5010` |
| 🌐 API | `http://localhost:5205` |

**Option B — Séparé**

```bash
# Terminal 1 — API 🌐
dotnet run --project src/Kairu.Api/Kairu.Api.csproj --launch-profile http

# Terminal 2 — Web 💻
dotnet run --project src/Kairu.Web/Kairu.Web.csproj --launch-profile http
```

### 3️⃣ Lancer les tests

```bash
dotnet test
```

```
✅ Réussi! — 192 tests, 0 échec
```

---

## 🎫 Intégration Jira (optionnel)

Dans l'application → **⚙️ Paramètres** → section Jira :

| Champ | Exemple |
|-------|---------|
| Base URL | `https://ton-domaine.atlassian.net` |
| Email | `ton@email.com` |
| API Token | Généré sur [id.atlassian.com/manage-profile/security/api-tokens](https://id.atlassian.com/manage-profile/security/api-tokens) |

---

## 📚 Documentation

- [`docs/spec.md`](docs/spec.md) — 📋 Spécification complète, use cases, diagrammes, ADR
- [`docs/project-state.md`](docs/project-state.md) — 📍 État de l'itération courante et historique

---

## 📄 Licence

Usage privé — projet personnel.
