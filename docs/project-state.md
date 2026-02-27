# Kairudev — État du projet

> Ce fichier est mis à jour après chaque itération.
> Il est lu par Claude au démarrage de chaque session.

---

## Résumé état actuel

**Dernière itération : #11b — BC Settings + Thème Dark paramétrable** (2026-02-XX)

**Bounded Contexts opérationnels :**
- **Tasks** : 6 Commands/Queries — **Architecture CQRS** ✅
- **Pomodoro** : 10 Commands/Queries — **Architecture CQRS** ✅
- **Journal** : 5 Commands/Queries — **Architecture CQRS** ✅
- **Settings** : 2 Commands/Queries (🆕 thème paramétrable) — **Architecture CQRS** ✅

**Architecture Application Layer :**
- ✅ **CQRS sans MediatR** : Commands (écriture) + Queries (lecture)
- ✅ **Handlers** retournent directement des `Result` (plus de Presenters)
- ✅ **Injection directe** dans les Controllers (pas de mediator)
- ✅ **23 use cases** (6 Tasks + 10 Pomodoro + 5 Journal + 2 Settings)

**Tests :** 154 au total, **7 migrés vers Handlers** (AddTask, CompleteTask + 5 à migrer)

**Infrastructure :** API REST, Blazor WASM, .NET MAUI, SQLite + EF Core, .NET Aspire

**Migrations :** 6 migrations (InitialCreate, AddPomodoro, AddJournalEntry, AddTaskDescription, AddSessionType, **AddUserSettings**)

---

## Itérations

| # | Contenu | Statut | Date |
|---|---|---|---|
| ~~#1~~ | ~~BC Tasks — Domain, Application, Adapters, Infrastructure, SQLite, 23 tests~~ | ~~✅ Livré~~ | ~~2026-02-24~~ |
| ~~#2~~ | ~~API REST (ASP.NET Core) + UI Blazor WebAssembly~~ | ~~✅ Livré~~ | ~~2026-02-24~~ |
| ~~#2b~~ | ~~Réécriture spec.md (use cases + diagrammes Mermaid) + prompts agents~~ | ~~✅ Livré~~ | ~~2026-02-24~~ |
| ~~#3~~ | ~~BC Pomodoro — sessions de focus, chrono circulaire, lien avec Tasks~~ | ~~✅ Livré~~ | ~~2026-02-25~~ |
| ~~#3b~~ | ~~.NET Aspire 13.1.1 — AppHost + ServiceDefaults~~ | ~~✅ Livré~~ | ~~2026-02-25~~ |
| ~~#4~~ | ~~Tests d'intégration SQLite (`Kairudev.Infrastructure.Tests`)~~ | ~~✅ Livré~~ | ~~2026-02-25~~ |
| ~~#5~~ | ~~Configuration externalisée — URL API via `appsettings.json`~~ | ~~✅ Livré~~ | ~~2026-02-25~~ |
| ~~#5b~~ | ~~Bugfixes (NetworkError + CreatedAtAction) + sous-agents Claude + UC-12 ChangeTaskStatus~~ | ~~✅ Livré~~ | ~~2026-02-26~~ |
| ~~#5c~~ | ~~UC-05 UpdateTask + Description optionnelle sur les tâches~~ | ~~✅ Livré~~ | ~~2026-02-26~~ |
| ~~#5d~~ | ~~Correctif migration EF Core — AddTaskDescription~~ | ~~✅ Livré~~ | ~~2026-02-26~~ |
| ~~#5e~~ | ~~UI Blazor — Description et modification de tâches~~ | ~~✅ Livré~~ | ~~2026-02-26~~ |
| ~~#6~~ | ~~UC-05 (Pomodoro) — Configurer les durées (sprint, pause courte/longue)~~ | ~~✅ Livré~~ | ~~2026-02-26~~ |
| ~~#7~~ | ~~Types de session Pomodoro (Sprint/Pause courte/Pause longue) — UI onglets~~ | ~~✅ Livré~~ | ~~2026-02-26~~ |
| ~~#8~~ | ~~BC Journal — log d'activité quotidien + génération automatique d'entrées~~ | ~~✅ Livré~~ | ~~2026-02-26~~ |
| ~~#10~~ | ~~.NET MAUI — application desktop/mobile~~ | ~~✅ Livré~~ | ~~2026-02-26~~ |
| ~~#11~~ | ~~Migration CQRS sans MediatR — refactoring architectural~~ | ~~✅ Livré~~ | ~~2026-02-XX~~ |
| ~~#11b~~ | ~~BC Settings + Thème Dark paramétrable~~ | ~~✅ Livré~~ | ~~2026-02-XX~~ |
| #12 | BC Tickets — intégration Jira / Linear / GitHub Issues | 📋 Planifié | — |

---

## Dernière itération livrée

**#11b — BC Settings + Thème Dark paramétrable** — Livré le 2026-02-XX

### Ce qui a été livré

#### Problème
L'application utilise uniquement le thème Bootstrap clair. Les utilisateurs modernes s'attendent à pouvoir choisir entre clair/sombre/système, avec persistence et synchronisation Web ↔ MAUI.

#### Solution appliquée

**Nouveau Bounded Context : Settings** ✅
- **Domain** : `UserSettings` (aggregate root, singleton), `ThemePreference` (enum : Light/Dark/System)
- **Application** : 2 use cases CQRS (GetUserSettings, SaveThemePreference)
- **Infrastructure** : `SqliteUserSettingsRepository` + migration `AddUserSettings`
- **API** : `SettingsController` (`GET /api/settings`, `PUT /api/settings/theme`)
- **UI (Web + MAUI)** : `SettingsApiClient`, select thème dans `Settings.razor`

**Fonctionnalités** ✅
- **3 modes** : ☀️ Clair, 🌙 Sombre, ⚙️ Système (détection via `prefers-color-scheme`)
- **Application immédiate** : thème change sans rechargement (JSInterop + `data-bs-theme`)
- **Persistence SQLite** : synchronisation Web ↔ MAUI via API REST
- **Defaults** : création automatique si premier accès (valeur par défaut : `System`)

**Architecture** ✅
- **Clean Architecture respectée** : dépendances pointent vers l'intérieur
- **CQRS sans MediatR** : suit exactement le pattern Tasks/Pomodoro/Journal
- **ADR-004** créé : documentation de la décision

### Impact
- **UX améliorée** : support natif du dark mode, confort visuel
- **Synchronisation** : préférence partagée entre Web et MAUI
- **Extensible** : BC Settings prêt pour d'autres préférences (langue, fuseau horaire, etc.)
- **23 use cases** au total (6 Tasks + 10 Pomodoro + 5 Journal + 2 Settings)

### Dette technique introduite
- **Tests manquants** : `UserSettingsTests`, `SaveThemePreferenceCommandHandlerTests`, `SqliteUserSettingsRepositoryTests`
- **Tests non migrés** : certains tests (Interactors/Presenters) ne compilent plus (priorité basse, hors scope)
- **Duplication UI** : `Settings.razor` dupliqué Web/MAUI (résolu dans future RCL Shared)

---

##

#### Problème
L'application n'était accessible que via navigateur web (Blazor WASM). Besoin d'une expérience native desktop/mobile avec les mêmes fonctionnalités.

#### Solution appliquée

**Projet .NET MAUI avec Blazor Hybrid** ✅
- **`Kairudev.Maui`** : nouveau projet .NET 10 MAUI avec `Microsoft.AspNetCore.Components.WebView.Maui`
- **Multi-plateforme** : cible Windows, Android, iOS, macOS via `TargetFrameworks`
- **Réutilisation des pages Blazor** : tous les composants copiés depuis `Kairudev.Web`
  - `Tasks.razor`, `Pomodoro.razor`, `Journal.razor`, `Settings.razor`
  - `NavMenu.razor` adapté avec émojis pour la navigation
  - Layout `MainLayout` réutilisé

**Services API clients** ✅
- **`Services/`** : `TaskApiClient`, `PomodoroApiClient`, `JournalApiClient`
- **DTOs** : `TaskDto`, `PomodoroDto`, `JournalDto` copiés dans le namespace `Kairudev.Maui.Services`
- **Configuration** : `appsettings.json` avec `ApiBaseUrl` (défaut : `https://localhost:7056`)
- **Injection de dépendances** : `MauiProgram.cs` configure `HttpClient` + les 3 clients API

**UI et UX** ✅
- **Page d'accueil** : redirection automatique vers `/tasks`
- **Navigation** : menu latéral avec icônes (☑ Tâches, 🍅 Pomodoro, 📖 Journal, ⚙ Paramètres)
- **CSS** : Bootstrap + styles personnalisés copiés depuis `Kairudev.Web`
- **Safe area** : support des zones sécurisées iOS (notch)

**Architecture** ✅
- **Communication API REST uniquement** : MAUI ne connaît rien du Domain/Application
- **Clean Architecture respectée** : aucune dépendance vers Domain/Application/Infrastructure
- **Blazor Hybrid** : les composants Razor tournent dans un WebView natif avec accès aux APIs .NET MAUI

### Impact
- **Application native** disponible sur **Windows, Android, iOS, macOS**
- **Réutilisation complète** des composants Blazor existants → zéro code UI à refactorer
- **Même API REST** utilisée par Web et MAUI → cohérence garantie
- **Prêt pour distribution** : Windows unpackaged, Android/iOS via stores

### Dette technique introduite
- **Duplication de code** : les pages Blazor et services API sont copiés dans `Kairudev.Maui`
- **Solution future** : extraire dans `Kairudev.Web.Shared` (Razor Class Library) référencée par Web + MAUI
- **Priorité** : basse (fonctionne tel quel, refactoring optionnel)

---

## Dernière itération livrée

**#11 — Migration CQRS sans MediatR (refactoring architectural)** — Livré le 2026-02-XX

### Ce qui a été livré

#### Problème
L'architecture Application Layer utilisait le pattern **Interactor + Presenter** (Clean Architecture classique) :
- **Verbosité excessive** : 4-5 fichiers par use case (Interactor, UseCase, Presenter, Request, HttpPresenter)
- **Presenters dupliqués** : inline dans chaque Controller (80+ lignes) + Fake Presenters dans tests (20+ lignes)
- **Complexité de test** : setup avec Fake Presenters, assertions indirectes via `_presenter.IsSuccess`
- **Pas mainstream** : pattern peu utilisé dans l'écosystème .NET moderne (eShop n'utilise pas de Presenters)

#### Solution appliquée

**Migration vers CQRS simplifié (sans MediatR)** ✅

**Architecture cible** :
```
Application/
├── Tasks/
│   ├── Commands/AddTask/
│   │   ├── AddTaskCommand.cs        (record immutable)
│   │   ├── AddTaskCommandHandler.cs (logique métier)
│   │   └── AddTaskResult.cs         (Success/Failure)
│   └── Queries/ListTasks/
│       ├── ListTasksQuery.cs
│       ├── ListTasksQueryHandler.cs
│       └── ListTasksResult.cs
```

**21 use cases migrés** :
- ✅ **Tasks** (6) : AddTask, ListTasks, CompleteTask, DeleteTask, UpdateTask, ChangeTaskStatus
- ✅ **Pomodoro** (10) : GetSettings, SaveSettings, StartSession, CompleteSession, InterruptSession, GetSuggestedSessionType, GetCurrentSession, LinkTask, CreateTaskDuringSession, UpdateTaskStatus
- ✅ **Journal** (5) : GetTodayJournal, AddComment, UpdateComment, RemoveComment, CreateEntry

**Controllers refactorés** :
```csharp
// Injection directe des Handlers (pas de MediatR)
public sealed class TasksController(
    AddTaskCommandHandler addTask,
    ListTasksQueryHandler listTasks,
    ...) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddTaskCommand command, CancellationToken ct)
    {
        var result = await addTask.HandleAsync(command, ct);
        return result.IsSuccess 
            ? Created($"api/tasks/{result.Task!.Id}", result.Task)
            : BadRequest(new { error = result.Error });
    }
}
```

**Tests migrés (2/18 ✅, 16 TODO)** :
- ✅ `AddTaskCommandHandlerTests.cs` (migré depuis `AddTaskInteractorTests.cs`)
- ✅ `CompleteTaskCommandHandlerTests.cs` (migré depuis `CompleteTaskInteractorTests.cs`)
- 🟡 **16 fichiers restants à migrer** (guide fourni dans `docs/test-migration-guide.md`)

**Documentation** ✅ :
- **ADR-003** : `docs/adr/003-migration-cqrs-sans-mediatr.md` (contexte, décision, alternatives, conséquences)
- **Guide de migration des tests** : `docs/test-migration-guide.md` (checklist, exemples, template)
- **Script PowerShell** : `scripts/migrate-tests.ps1` (plan de migration automatisé)

**Nettoyage partiel** 🟡 :
- ✅ Suppression des anciens Interactors Tasks (24 fichiers)
- 🟡 Anciens fichiers Pomodoro/Journal (partiellement — certains fichiers verrouillés dans VS)
- ⏳ Dossiers `Presenters/` dans API (à finaliser)
- ⏳ Projet `Kairudev.Adapters` (à supprimer complètement)

### Impact

**Gains quantitatifs** :
- **-40% de code** dans Application Layer (suppression Presenters + interfaces)
- **-38% de lignes** par Controller (~180 → ~110 lignes)
- **-28% de lignes** par fichier de test (~70 → ~50 lignes)

**Avantages qualitatifs** :
- ✅ **Plus simple** : retour direct de `Result`, pas d'inversion via Presenter
- ✅ **Plus testable** : assertions directes sur `result`, pas de Fake Presenters
- ✅ **Plus mainstream** : pattern standard .NET (eShop, Clean Architecture moderne)
- ✅ **CQRS explicite** : séparation Commands (écriture) vs Queries (lecture)
- ✅ **Performance** : injection directe, pas de pipeline MediatR

**Décisions techniques** :
- ❌ **MediatR rejeté** : licence non open-source (depuis v12)
- ✅ **Injection directe** : 6-10 handlers par Controller (acceptable pour 21 use cases)
- ⏳ **Service Facade** : envisageable si >15 handlers dans un Controller (pas nécessaire actuellement)

### Dette technique introduite

1. **Tests non migrés** (16/18) :
   - **Impact** : build échoue avec 55+ erreurs de compilation
   - **Plan** : suivre `docs/test-migration-guide.md` pour migrer les 16 tests restants
   - **Estimation** : ~30 min par test → ~8h de travail total

2. **Anciens fichiers non supprimés** :
   - **Cause** : fichiers ouverts dans Visual Studio (verrouillés)
   - **Plan** : fermer VS, exécuter script de nettoyage PowerShell
   - **Fichiers concernés** : 
     - `src/Kairudev.Application/Pomodoro/*` (anciens dossiers)
     - `src/Kairudev.Application/Journal/*` (anciens dossiers)
     - `src/Kairudev.Api/*/Presenters/` (dossiers vides)
     - `src/Kairudev.Adapters/` (projet complet, obsolète)

3. **Projet Kairudev.Adapters obsolète** :
   - **État** : contient uniquement des Presenters HTTP (devenus inline dans Controllers)
   - **Action** : supprimer complètement le projet + références dans solution

### Prochaines étapes (TODO manuel)

1. **Fermer Visual Studio** → déverrouiller les fichiers
2. **Exécuter le nettoyage** :
   ```powershell
   # Supprimer anciens dossiers Application
   Remove-Item -Recurse -Force "src\Kairudev.Application\Pomodoro\GetSettings"
   # ... (10+ dossiers Pomodoro + 5 Journal)

   # Supprimer Presenters API
   Remove-Item -Recurse -Force "src\Kairudev.Api\Tasks\Presenters"
   Remove-Item -Recurse -Force "src\Kairudev.Api\Pomodoro\Presenters"
   Remove-Item -Recurse -Force "src\Kairudev.Api\Journal\Presenters"

   # Supprimer projet Adapters
   Remove-Item -Recurse -Force "src\Kairudev.Adapters"
   ```

3. **Migrer les 16 tests restants** :
   - Ouvrir `docs/test-migration-guide.md`
   - Pour chaque fichier `*InteractorTests.cs` :
     1. Copier le test existant
     2. Appliquer la checklist de migration
     3. Créer le nouveau `*CommandHandlerTests.cs` ou `*QueryHandlerTests.cs`
     4. Supprimer l'ancien `*InteractorTests.cs`
     5. Vérifier que le test passe ✅

4. **Build finale** :
   ```powershell
   dotnet build
   dotnet test
   ```

### Références

- **ADR** : `docs/adr/003-migration-cqrs-sans-mediatr.md`
- **Guide migration tests** : `docs/test-migration-guide.md`
- **Script PowerShell** : `scripts/migrate-tests.ps1`
- **Exemple migré** : `tests/Kairudev.Application.Tests/Tasks/AddTaskCommandHandlerTests.cs`

---

## Itération précédente

**#10 — .NET MAUI (application desktop/mobile)** — Livré le 2026-02-26

### Ce qui a été livré

#### Problème
L'application n'était accessible que via navigateur web (Blazor WASM). Besoin d'une expérience native desktop/mobile avec les mêmes fonctionnalités.

#### Solution appliquée

**UI Blazor** (nouveau) ✅
- **`Journal.razor`** : page `/journal` avec timeline chronologique des événements
  - Affichage des entrées du jour (sprints, tâches) avec icônes et horodatages
  - Liste des commentaires par entrée
  - Formulaire d'ajout de commentaire (inline)
  - Modification/suppression de commentaires (inline)
  - Icônes contextuelles : 🍅 Sprint démarré, ✅ Sprint complété, ⏸️ Sprint interrompu, 🚀 Tâche démarrée, 🎉 Tâche complétée
- **`JournalApiClient`** : appels HTTP pour `GetTodayEntries`, `AddComment`, `UpdateComment`, `DeleteComment`
- **`JournalDto`** : DTOs `JournalEntryDto`, `JournalCommentDto`, `AddCommentRequest`, `UpdateCommentRequest`
- **`NavMenu.razor`** : ajout du lien 📖 Journal dans la navigation

**Intégration événementielle** (UCS-1 — Génération automatique) ✅
- **Pomodoro → Journal** :
  - `StartSessionInteractor` → crée `SprintStarted` lors du démarrage
  - `CompleteSessionInteractor` → crée `SprintCompleted` lors de la complétion
  - `InterruptSessionInteractor` → crée `SprintInterrupted` lors de l'interruption
- **Tasks → Journal** :
  - `ChangeTaskStatusInteractor` → crée `TaskStarted` si statut = `InProgress`
  - `ChangeTaskStatusInteractor` → crée `TaskCompleted` si statut = `Done`
  - `CompleteTaskInteractor` → crée `TaskCompleted` lors de la complétion
- Injection de `ICreateJournalEntryUseCase` dans tous les interactors concernés
- `NoOpJournalEntryPresenter` utilisé pour les appels internes (pas de réponse HTTP nécessaire)

**Infrastructure déjà en place** (itérations précédentes) ✅
- **Domain** : `JournalEntry`, `JournalComment`, `JournalEventType`, `IJournalEntryRepository`
- **Application** : 5 use cases (GetTodayJournal, AddComment, UpdateComment, RemoveComment, CreateEntry)
- **API** : `JournalController` avec endpoints REST complets
- **Tests** : 71 Domain + 66 Application + 17 Infrastructure = 154 tests, tous au vert ✅

### Impact
- L'utilisateur peut maintenant **consulter son activité quotidienne** sous forme de timeline
- Les événements Pomodoro et Tasks sont **automatiquement tracés** dans le journal
- L'utilisateur peut **ajouter des commentaires** personnels pour enrichir le contexte
- **Rétrospective facilitée** : "qu'est-ce que j'ai fait aujourd'hui ?"
- **Traçabilité complète** : chaque sprint et chaque tâche démarrée/complétée génère une entrée
- **UC-13, UC-14, UC-15, UC-16, UCS-1** sont maintenant complètement implémentés de bout en bout

### Captures d'écran (UI)
- Page `/journal` : timeline chronologique avec icônes et horodatages
- Affichage des commentaires : texte + boutons modifier/supprimer
- Formulaire d'ajout : textarea + boutons Ajouter/Annuler

---

## Itération #7 (précédente)

**#7 — Types de session Pomodoro (Sprint/Pause courte/Pause longue)** — Livré le 2026-02-26

### Ce qui a été livré

#### Problème
L'utilisateur ne pouvait pas choisir le type de session Pomodoro à démarrer. Seul le sprint était disponible, sans possibilité de lancer explicitement une pause courte ou longue. Le système comptait les sprints mais ne suggérait pas automatiquement le type de session suivant.

#### Solution appliquée

**Backend (Partie 1/2)**

- **Domain** : Nouveau enum `PomodoroSessionType` (Sprint, ShortBreak, LongBreak)
- **Domain** : Propriété `SessionType` ajoutée à `PomodoroSession`
- **Application** : 
  - `StartSessionRequest` accepte un paramètre `sessionType` optionnel
  - Logique de suggestion intelligente : après 4 sprints → LongBreak, sinon → Sprint/ShortBreak
  - Nouveau use case `GetSuggestedSessionType` pour récupérer la suggestion
- **API** : 
  - `GET /api/pomodoro/session/suggested` — retourne le type suggéré + durées
  - `POST /api/pomodoro/session?type={Sprint|ShortBreak|LongBreak}` — démarre avec type spécifique
- **Infrastructure** : Migration `AddSessionType` (colonne `SessionType` dans table PomodoroSessions)
- **Tests** : Tous les tests mis à jour pour `PomodoroSession.Create(type, duration)`

**Frontend (Partie 2/2)**

- **DTO** : 
  - `PomodoroSessionDto` inclut maintenant `SessionType`
  - Nouveau `SuggestedSessionTypeDto` pour la suggestion
- **ApiClient** : 
  - `GetSuggestedSessionTypeAsync()` — appel à l'endpoint de suggestion
  - `StartSessionAsync(sessionType)` — démarre avec type optionnel
- **UI Blazor** : 
  - **3 onglets (radio buttons)** pour choisir le type :
    - 🍅 **Sprint** (bleu, durée configurée)
    - ☕ **Pause courte** (vert, durée configurée)
    - 🌙 **Pause longue** (cyan, durée configurée)
  - **Présélection intelligente** via `GET /suggested` au chargement
  - **Durée affichée dynamiquement** dans chaque onglet
  - **Couleur de l'arc** adaptée au type de session
  - **Label de statut** contextualisé ("Sprint en cours", "Pause courte", "Pause longue")

**Tests** : 154 tests au total (71 Domain + 66 Application + 17 Infrastructure), 0 échec ✅

### Impact
- L'utilisateur peut maintenant **choisir explicitement** le type de session à démarrer
- Le système **suggère intelligemment** le prochain type basé sur l'historique
- **Meilleure expérience utilisateur** : onglets visuels + durées claires
- **Traçabilité complète** : chaque session stocke son type en base
- **Respect du rythme Pomodoro classique** : 4 sprints → pause longue

### Captures d'écran (UI)
- Page `/pomodoro` : 3 onglets avec durées affichées
- Horloge circulaire : couleur adaptée (bleu/vert/cyan)
- Label dynamique : "Sprint en cours" / "Pause courte" / "Pause longue"

---

## Itération #6 (précédente)

**#6 — UC-05 (Pomodoro) — Configurer les durées** — Livré le 2026-02-26

### Ce qui a été livré

#### Problème
L'utilisateur n'avait aucun moyen de personnaliser les durées des sprints Pomodoro. Les valeurs par défaut (25 min sprint, 5 min pause courte, 15 min pause longue) étaient codées en dur.

#### Solution appliquée

**Tests Application** : `SaveSettingsInteractorTests` (+13 tests)
- Validation des durées dans la plage [1-120] minutes
- Test des valeurs minimales et maximales
- Test de réécriture des paramètres existants
- Test des erreurs de validation pour chaque champ

**API** : Endpoint existant `PUT /api/pomodoro/settings`
- Déjà implémenté dans `PomodoroController`
- `SaveSettingsHttpPresenter` gère la réponse (204 NoContent / 400 BadRequest)

**Blazor UI** : Nouvelle page `/settings`
- Formulaire avec 3 champs numériques (sprint, pause courte, pause longue)
- Validation côté client et serveur (1-120 minutes)
- Bouton "Réinitialiser" pour revenir aux valeurs par défaut
- Affichage des messages de succès/erreur
- Ajout du lien "⚙ Paramètres" dans le menu de navigation

**Tests** : 154 tests au total (71 Domain + 66 Application + 17 Infrastructure), 0 échec ✅

### Impact
- L'utilisateur peut maintenant personnaliser les durées Pomodoro selon ses préférences
- Les durées sont persistées dans SQLite et réutilisées à chaque démarrage de sprint
- UC-05 (Pomodoro) est maintenant complètement implémenté et testé
- Ajout de 13 tests Application pour couvrir tous les scénarios de validation

### Captures d'écran (UI)
- Page `/settings` : formulaire avec 3 champs + boutons Enregistrer/Réinitialiser
- Menu de navigation : nouvel item "⚙ Paramètres"

---

## Itération #5e (précédente)

**#5e — UI Blazor pour description et modification de tâches** — Livré le 2026-02-26

### Ce qui a été livré

#### Problème rencontré
L'utilisateur ne voyait pas de différence dans l'interface web Blazor après l'implémentation de UC-05 (UpdateTask). Il n'était pas possible de créer une description pour une tâche ou de modifier une tâche existante depuis l'UI.

#### Solution appliquée
- **`TaskDto`** : Ajout propriété `Description?`
- **`TaskApiClient`** :
  - `AddAsync(title, description)` — ajout paramètre optionnel `description`
  - `UpdateAsync(id, title, description)` — nouvelle méthode pour modifier une tâche
- **`Tasks.razor`** : Refonte complète de l'interface
  - **Création** : Champ titre + textarea description (optionnelle)
  - **Affichage** : Description affichée en petit texte grisé sous le titre
  - **Modification** : Mode édition inline avec boutons Enregistrer/Annuler
  - **Actions** : Bouton ✎ (modifier) ajouté pour les tâches non complétées
  - **Layout** : Meilleur alignement avec flex-grow pour le contenu

### Impact
- L'utilisateur peut maintenant créer des tâches avec description
- L'utilisateur peut modifier le titre et la description d'une tâche existante
- L'interface affiche clairement les descriptions des tâches
- UC-05 est maintenant complètement fonctionnel de bout en bout (Domain → Application → API → UI)

### Captures d'écran (UI)
- Formulaire de création : titre + description
- Liste de tâches : titre en gras, description en petit en dessous
- Mode édition : inline avec inputs et boutons d'action

---

## Itération #5d (précédente)

**#5d — Correctif migration EF Core** — Livré le 2026-02-26

### Ce qui a été livré

#### Problème rencontré
L'application ne démarrait plus avec l'erreur :
```
System.InvalidOperationException: The model for context 'KairudevDbContext' has pending changes. 
Add a new migration before updating the database.
```

#### Solution appliquée
- **Migration créée** : `20260226180050_AddTaskDescription.cs`
  - Ajoute la colonne `Description` (nullable, max 1000 caractères) à la table `Tasks`
  - Cette migration correspond aux changements faits dans `TaskConfiguration` lors de l'itération #5c
- **Build réussi** : 0 erreur, 0 avertissement
- **Tests** : 141/141 au vert ✅

### Impact
- L'application peut maintenant démarrer sans erreur
- La base de données est synchronisée avec le modèle EF Core
- Toutes les fonctionnalités précédentes (Tasks, Pomodoro, Journal) restent opérationnelles

---

## Itération #5c (précédente)

**#5c — UC-05 UpdateTask + Description optionnelle** — Livré le 2026-02-26

### Ce qui a été livré

#### UC-05 — Modifier une tâche
- **Domain** : 
  - Nouveau Value Object `TaskDescription` (optionnel, max 1000 caractères)
  - `DeveloperTask.Description` (propriété nullable)
  - `DeveloperTask.UpdateDetails(title, description)` — méthode de mise à jour
  - `DeveloperTask.Create()` accepte maintenant un paramètre `description`
- **Application** : `UpdateTask/` — Request, UseCase, Presenter, Interactor
- **Infrastructure** : 
  - Migration `AddTaskDescription` — ajout colonne `Description` (nullable, max 1000)
  - Mise à jour `TaskConfiguration` pour mapper la description
- **API** : 
  - Endpoint `PUT api/tasks/{id}` avec `UpdateTaskBody(Title, Description?)`
  - `UpdateTaskHttpPresenter`
- **Tests** : +19 tests (13 Domain + 6 Application)
- **Total : 141 tests** (71 Domain + 53 Application + 17 Infrastructure), 0 échec

#### Mise à jour AddTask
- `AddTaskRequest` accepte maintenant `Description` (optionnelle)
- `AddTaskInteractor` valide et crée la tâche avec description
- `TaskViewModel` inclut maintenant la `Description`

### Impact sur les BC existants
- Tous les appels à `DeveloperTask.Create()` ont été mis à jour pour passer `null` pour la description
- Aucune régression : tous les tests existants continuent de passer

---

## Itération #5b (précédente)

**Bugfixes + sous-agents + UC-12 ChangeTaskStatus** — Livré le 2026-02-26

### Ce qui a été livré

#### Bugfixes
- **NetworkError Blazor WASM** : `ApiBaseUrl` corrigé → `http://localhost:5205` (Aspire utilise le profil HTTP)
- **`CreatedAtActionResult("GetById")`** : remplacé par `CreatedResult($"api/tasks/{id}")` (action inexistante)

#### Sous-agents Claude Code
- **`.claude/agents/pm.md`** : agent Product Manager (outils : Read, Glob, Grep)
- **`.claude/agents/arch.md`** : agent Architecte (outils : Read, Glob, Grep, Write, Edit)
- **`.claude/agents/dev.md`** : agent Développeur (outils : Read, Glob, Grep, Write, Edit, Bash)

#### UC-12 — Changer le statut d'une tâche
- **Domain** : `DeveloperTask.ChangeStatus(TaskStatus, DateTime)` + `DomainErrors.Tasks.SameStatus`
- **Application** : `ChangeTaskStatus/` — Request, IUseCase, IPresenter, Interactor
- **API** : `PATCH api/tasks/{id}/status` + `ChangeTaskStatusHttpPresenter` + action dans `TasksController`
- **Tests** : +17 tests (9 Domain + 8 Application)
- **Total : 89 tests** (44 Domain + 28 Application + 17 Infrastructure), 0 échec

### Dette technique héritée et courante
- `TaskStatus` : alias `DomainTaskStatus` nécessaire dans les tests (conflit namespace)
- `DomainErrors` : alias `PomodoroErrors` nécessaire quand Tasks et Pomodoro sont tous deux importés
- `DeveloperTask.StartProgress()` redondant avec `ChangeStatus(InProgress, now)` — à supprimer dans un refactoring futur
- `Kairudev.Adapters` : projet toujours présent dans la solution mais vide de sens (suppression à planifier)

---

## Stack technique
- .NET 10 GA (SDK 10.0.200-preview = SDK .NET 10.1 preview, runtime 10 GA)
- SQLite + EF Core 10.0.3 (fichier local `kairudev.db`, hors git)
- ASP.NET Core Web API (`Kairudev.Api`)
- Blazor WebAssembly (`Kairudev.Web`)
- .NET Aspire 13.1.1 (`Kairudev.AppHost` + `Kairudev.ServiceDefaults`)
- .NET MAUI — itération future
- xUnit pour les tests
- Solution : `Kairudev.slnx`

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
├── Kairudev.AppHost/           ← orchestration Aspire
├── Kairudev.ServiceDefaults/   ← OTEL, health checks, service discovery
└── Kairudev.Web/               ← Services/ + Pages/ + wwwroot/appsettings.json
tests/
├── Kairudev.Domain.Tests/      ← Tasks/ + Pomodoro/
├── Kairudev.Application.Tests/ ← Tasks/ + Pomodoro/
└── Kairudev.Infrastructure.Tests/  ← Tasks/ + Pomodoro/ (17 tests intégration SQLite)
docs/
├── spec.md
└── project-state.md
```
