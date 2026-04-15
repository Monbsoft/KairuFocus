# KairuFocus — Instructions GitHub Copilot

## Démarrage de chaque session

1. Lis `docs/project-state.md` — état courant du projet.
2. Lis `docs/spec.md` — use cases, modèle du domaine, ADR.
3. Résume en 3 lignes : où on en est, ce qui était prévu.
4. Demande : *"On continue avec ce qui était prévu, ou tu as une nouvelle priorité ?"*
5. Attends la réponse. Aucune action sans confirmation.

---

## Workflow Git pour chaque itération

### Au début d'une itération
1. **Créer une branche** : `git checkout -b feature/{numero}-{nom-court}`
   - Exemple : `git checkout -b feature/12-bc-tickets`
2. **Vérifier la branche active** : `git branch`

### Pendant l'itération
- **Commits fréquents** avec messages clairs :
  - Format : `feat({scope}): {description courte}`
  - Exemple : `feat(tickets): ajout domain Ticket + value objects`
- **Build et tests avant chaque commit** : `dotnet build && dotnet test`

### À la fin d'une itération
1. **Build et tests finaux** : validation complète
2. **Mise à jour `docs/project-state.md`** : documenter l'itération
3. **Commit final** : tous les changements incluant la documentation
4. **Push de la branche** : `git push -u origin feature/{numero}-{nom-court}`
5. **Créer une Pull Request** :
   - Titre : `feat({numero}): {nom de l'itération}`
   - Description : résumé technique + checklist (voir template ci-dessous)
6. **Attendre validation** avant merge

### Template Pull Request
```markdown
## Itération #{numero} — {Nom de l'itération}

### 🎯 Objectif
{Description courte du problème résolu}

### ✅ Ce qui a été fait
- {Changement 1}
- {Changement 2}
- Mise à jour `docs/project-state.md`

### 📊 Impact
- {Impact utilisateur/technique}

### 🧪 Tests
```
dotnet build   # ✅ Génération réussie
dotnet test    # ✅ {nombre} tests passent
```

### 📝 Checklist
- [x] Build réussit
- [x] Tests passent
- [x] Documentation mise à jour
- [x] Aucune régression
```

---

## Contexte produit

KairuFocus est une application de gestion d'activité quotidienne pour développeurs :
- Todo list de micro-tâches
- Journal de bord
- Intégration tickets (Jira, Linear, GitHub Issues)
- Sessions Pomodoro

## Stack technique
- .NET 10, C#
- Clean Architecture + DDD
- SQLite + EF Core (fichier local `KairuFocus.db`, hors git)
- ASP.NET Core (API REST)
- Blazor WebAssembly (UI web)
- .NET MAUI (UI mobile/desktop, itération future)
- xUnit (tests)

## Règles fondamentales
- Les dépendances pointent uniquement vers l'intérieur
- Jamais le Domain ne connaît l'Infrastructure
- Chaque Use Case = une classe Interactor avec Input/Output Boundary
- Tests nommés `Should_[résultat]_When_[contexte]`
- Langue du code : anglais. Langue des échanges : français.

## Use Case — template standard
```
UC-XX — Nom
Acteur principal / Préconditions / Postconditions (succès)
Scénario nominal (étapes numérotées)
Scénarios alternatifs / Scénarios d'exception
Critères d'acceptance (cases à cocher)
```
Les diagrammes Mermaid s'insèrent dans la spec là où ils apportent de la clarté.

## Séparation des responsabilités documentaires
- `docs/spec.md` — fonctionnel et architectural : use cases, modèle du domaine, ADR, diagrammes
- `docs/project-state.md` — état d'avancement : itération courante, statut, dette technique

## Fichiers clés
- `docs/project-state.md` — état courant
- `docs/spec.md` — spécification
- `CLAUDE.md` / `AGENTS.md` — instructions complètes

---

## Rôle : Relecteur (relecture de commit)

Quand l'utilisateur demande une relecture de commit ou de code, endosse le rôle **Relecteur**.

### Étapes

1. `git show --stat HEAD` — fichiers touchés + message
2. `git diff HEAD~1 HEAD` — diff complet
3. Lire chaque fichier modifié pour le contexte
4. Produire le rapport structuré

### Critères de relecture

**Bloquant**
- Violation règle de dépendance : une couche intérieure (Domain/Application) importe une couche extérieure
- EF Core / DbContext présent dans `KairuFocus.Domain`
- Exception pour flux normal au lieu de `Result.Failure<T>`
- Interface Application avec plusieurs responsabilités

**Avertissement**
- Test non nommé `Should_[résultat]_When_[contexte]`
- Message de commit hors format `feat({scope}): description`
- Value Object avec setter public ou sans `Create()` retournant `Result<T>`
- Code en français (doit être en anglais)
- `TaskStatus` sans alias `DomainTaskStatus` / `DomainErrors` sans alias `PomodoroErrors`

**Suggestion**
- Tests manquants sur scénarios nominaux ou d'exception
- Dead code ou TODO sans référence
- Migration EF Core absente après modification de modèle

### Format du rapport

```
## Rapport de relecture — {hash} "{titre}"
### Fichiers analysés
### Bloquants ({n})      [fichier:ligne] description
### Avertissements ({n}) [fichier:ligne] description
### Suggestions ({n})    [fichier:ligne] description
### Points positifs
### Verdict : [ BLOQUÉ | À CORRIGER | APPROUVÉ ]
```

Toujours citer le fichier et la ligne. Ne pas inventer de problèmes. Rester factuel.
