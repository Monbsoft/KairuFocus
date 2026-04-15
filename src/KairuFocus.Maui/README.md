# KairuFocus.Maui

Application native **desktop/mobile** pour KairuFocus, utilisant **.NET MAUI avec Blazor Hybrid**.

## Plateformes supportées

- **Windows** (10.0.17763.0+)
- **Android** (API 24+)
- **iOS** (15.0+)
- **macOS** (Catalyst 15.0+)

## Architecture

- **Blazor Hybrid** : les pages Blazor (Tasks, Pomodoro, Journal, Settings) tournent dans un `BlazorWebView` natif
- **Communication API REST** : l'app communique avec `KairuFocus.Api` via `HttpClient`
- **Clean Architecture** : aucune référence aux projets Domain/Application/Infrastructure

## Configuration

### URL de l'API

L'URL de l'API est configurée dans `appsettings.json` :

```json
{
  "ApiBaseUrl": "https://localhost:7056"
}
```

Pour Android, remplacez `localhost` par l'IP de votre machine ou utilisez l'émulateur Android avec `10.0.2.2:7056`.

## Lancement

### Windows

```bash
dotnet build -t:Run -f net10.0-windows10.0.19041.0
```

Ou via Visual Studio : sélectionnez `KairuFocus.Maui (net10.0-windows)` comme projet de démarrage et appuyez sur F5.

### Android

```bash
dotnet build -t:Run -f net10.0-android
```

Ou via Visual Studio : sélectionnez un émulateur Android et lancez.

### iOS/macOS

Nécessite un Mac avec Xcode installé.

```bash
dotnet build -t:Run -f net10.0-ios        # iPhone/iPad
dotnet build -t:Run -f net10.0-maccatalyst # macOS
```

## Fonctionnalités

Toutes les fonctionnalités de Blazor WASM sont disponibles :

- ✅ **Tâches** : ajout, modification, complétion, suppression
- ✅ **Pomodoro** : chrono circulaire, types de session (sprint/pause courte/pause longue)
- ✅ **Journal** : timeline des événements, ajout de commentaires
- ✅ **Paramètres** : configuration des durées Pomodoro

## Structure du projet

```
KairuFocus.Maui/
├── Components/
│   ├── Layout/          # MainLayout, NavMenu
│   └── Pages/           # Tasks, Pomodoro, Journal, Settings, Home
├── Services/            # TaskApiClient, PomodoroApiClient, JournalApiClient
├── wwwroot/             # CSS, Bootstrap, index.html
├── appsettings.json     # Configuration (URL API)
└── MauiProgram.cs       # Point d'entrée, injection de dépendances
```

## Dette technique

**Duplication de code** : les pages Blazor et services API sont copiés depuis `KairuFocus.Web`.

**Solution future** : créer une **Razor Class Library** `KairuFocus.Web.Shared` contenant :
- Pages Blazor réutilisables
- Services API clients
- DTOs

Référencée par `KairuFocus.Web` et `KairuFocus.Maui`.

**Priorité** : basse (fonctionne tel quel, refactoring optionnel).
