# ADR-004 — Thème paramétrable (Dark mode)

**Statut** : ✅ Accepté  
**Date** : 2026-02-XX  
**Contexte** : Itération #11b

---

## Contexte et problème

L'application Kairudev (Web Blazor WASM + .NET MAUI) utilise le thème Bootstrap par défaut (clair uniquement). Les utilisateurs modernes s'attendent à pouvoir choisir entre un thème clair et un thème sombre, ou suivre la préférence du système.

**Besoin fonctionnel :**
- L'utilisateur doit pouvoir basculer entre thème clair/sombre/système
- La préférence doit être **persistée** et synchronisée entre Web et MAUI
- Le changement doit être appliqué **immédiatement** sans rechargement

**Contraintes architecturales :**
- Respecter l'architecture Clean + DDD + CQRS en place
- Éviter une solution UI-only (localStorage/Preferences) non synchronisée
- Réutiliser les mêmes mécanismes que les autres settings (Pomodoro)

---

## Décision

**Option choisie : Bounded Context Settings avec persistence SQLite**

### Architecture implémentée

#### 1. **Domain Layer**
- **Aggregate Root** : `UserSettings` (singleton avec `Id = 1`)
- **Value Object** : `ThemePreference` (enum : `Light`, `Dark`, `System`)
- **Repository Interface** : `IUserSettingsRepository`

```csharp
public enum ThemePreference { Light, Dark, System }

public sealed class UserSettings : AggregateRoot<int>
{
    public const int SingletonId = 1;
    public ThemePreference ThemePreference { get; private set; }
    
    public void UpdateThemePreference(ThemePreference newPreference) { ... }
}
```

#### 2. **Application Layer (CQRS)**
- **Query** : `GetUserSettingsQuery` → `GetUserSettingsQueryHandler` → `UserSettingsViewModel`
- **Command** : `SaveThemePreferenceCommand` → `SaveThemePreferenceCommandHandler` → `SaveThemePreferenceResult`

#### 3. **Infrastructure Layer**
- **Repository** : `SqliteUserSettingsRepository` (EF Core)
- **Migration** : `AddUserSettings` (table `UserSettings` avec colonnes `Id`, `ThemePreference`)
- **Configuration** : `UserSettingsConfiguration` (mapping EF Core)

#### 4. **API Layer**
- **Controller** : `SettingsController` (`GET /api/settings`, `PUT /api/settings/theme`)
- **Injection** : Handlers enregistrés dans `Program.cs`

#### 5. **UI Layer (Web + MAUI)**
- **Service API Client** : `SettingsApiClient` (appels REST)
- **Page** : `Settings.razor` avec select `☀️ Clair / 🌙 Sombre / ⚙️ Système`
- **Application du thème** : via JavaScript `document.documentElement.setAttribute('data-bs-theme', 'dark/light')`
- **Détection préférence système** : `window.matchMedia('(prefers-color-scheme: dark)')`

### Flux utilisateur

1. L'utilisateur ouvre `/settings`
2. Le composant appelle `GET /api/settings` → charge `ThemePreference`
3. L'utilisateur change le thème dans le select
4. `@bind:after="OnThemeChanged"` → `PUT /api/settings/theme`
5. SQLite est mis à jour → synchronisation Web ↔ MAUI garantie
6. Le thème est appliqué immédiatement via JSInterop

---

## Conséquences

### ✅ Avantages

1. **Architecture cohérente** : suit exactement le même pattern que Tasks/Pomodoro/Journal (CQRS sans MediatR)
2. **Persistence robuste** : SQLite garantit la synchronisation entre Web/MAUI via l'API
3. **Extensible** : le BC Settings peut accueillir d'autres préférences (langue, fuseau horaire, etc.)
4. **Domain-driven** : règles métier encapsulées dans l'aggregate `UserSettings`
5. **Testable** : handlers, repository, domain logic tous testables unitairement

### ⚠️ Inconvénients

1. **Plus complexe** qu'une solution `localStorage` pure (Web) ou `Preferences` (MAUI)
2. **Latence réseau** : chaque changement appelle l'API (mitigation : changement immédiat en UI + async save)
3. **Singleton** : un seul utilisateur par app (acceptable dans notre contexte)

### 🔄 Alternatives considérées

#### Option A : localStorage (Web) + Preferences (MAUI) — ❌ Rejetée
- **Pour** : simple, pas de backend requis
- **Contre** : **aucune synchronisation** entre Web et MAUI, deux sources de vérité

#### Option B : Stockage cloud (Azure App Configuration / Firebase) — ❌ Rejetée
- **Pour** : synchronisation temps réel, multi-device
- **Contre** : over-engineering pour un seul utilisateur, coût, dépendance externe

#### Option C : BC Settings + SQLite (choix actuel) — ✅ Accepté
- **Pour** : architecture cohérente, synchronisation via API, extensible
- **Contre** : complexité

---

## Références

- **Bootstrap Dark Mode** : https://getbootstrap.com/docs/5.3/customize/color-modes/
- **Blazor JSInterop** : https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/
- **MAUI Preferences API** : https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/preferences
- **ADR-003** : Migration CQRS sans MediatR (pattern réutilisé ici)

---

## Implémentation

- **Fichiers Domain** : `UserSettings.cs`, `ThemePreference.cs`, `IUserSettingsRepository.cs`
- **Fichiers Application** : `GetUserSettingsQuery/Handler/Result`, `SaveThemePreferenceCommand/Handler/Result`
- **Fichiers Infrastructure** : `SqliteUserSettingsRepository.cs`, `UserSettingsConfiguration.cs`, migration `AddUserSettings`
- **Fichiers API** : `SettingsController.cs`, injection dans `Program.cs`
- **Fichiers UI** : `SettingsApiClient.cs` (Web + MAUI), `Settings.razor` (Web + MAUI)

---

## Tests à ajouter

- [ ] `UserSettingsTests` — Domain logic (UpdateThemePreference)
- [ ] `SaveThemePreferenceCommandHandlerTests` — validation parsing enum
- [ ] `SqliteUserSettingsRepositoryTests` — GetAsync crée default si absent
- [ ] Tests d'intégration API : `GET /api/settings`, `PUT /api/settings/theme`

