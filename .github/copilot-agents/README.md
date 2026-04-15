# Agents GitHub Copilot pour KairuFocus

Ce dossier contient les définitions des agents GitHub Copilot spécialisés pour le projet KairuFocus.

## Agents disponibles

### 1. 🎯 Product Manager (`product-manager.json`)
**Rôle :** Clarification des besoins, définition des user stories et critères d'acceptance

**Utilisation :**
- Démarrer une nouvelle feature
- Prioriser le backlog
- Affiner un use case existant
- Définir les critères d'acceptance

**Commandes suggérées :**
- `@product-manager Je veux ajouter la fonctionnalité X`
- `@product-manager Aide-moi à prioriser les features en attente`
- `@product-manager Affine le use case UC-05`

---

### 2. 🏛️ Architecte (`architecte.json`)
**Rôle :** Clean Architecture + DDD, modélisation domaine et décisions architecturales

**Utilisation :**
- Modéliser un Bounded Context
- Créer un ADR (Architecture Decision Record)
- Valider le respect de Clean Architecture
- Définir les Boundaries d'un Use Case

**Commandes suggérées :**
- `@architecte Modélise le Bounded Context Pomodoro`
- `@architecte Crée un ADR pour le choix de la base de données`
- `@architecte Valide l'architecture actuelle`
- `@architecte Définis les Boundaries pour le use case AddTask`

---

### 3. 💻 Développeur Senior .NET (`developpeur.json`)
**Rôle :** Implémentation Clean Architecture, code C# idiomatique, tests xUnit

**Utilisation :**
- Implémenter un Use Case complet
- Écrire les tests unitaires
- Refactorer vers Clean Architecture
- Review de code

**Commandes suggérées :**
- `@developpeur Implémente le use case UC-08 CompleteTask`
- `@developpeur Écris les tests pour TaskTitle`
- `@developpeur Refactore TasksController vers Clean Architecture`
- `@developpeur Review le code de AddTaskInteractor`

---

## Workflow type

```
1. @product-manager     → Démarrer itération (créer branche Git)
2. @product-manager     → Définir le besoin et le use case
3. @architecte          → Modéliser le domaine et les boundaries
4. @developpeur         → Implémenter + tester (commits fréquents)
5. @architecte          → Créer l'ADR si décision majeure
6. @product-manager     → Finaliser itération (commit final + PR)
```

---

## Workflow Git automatique

### Début d'itération
```
@product-manager Démarrer l'itération #12
→ Crée branche feature/12-bc-tickets
→ Questionne pour définir le use case
```

### Pendant l'itération
```
@developpeur Implémente le use case UC-XX
→ Commits fréquents après chaque étape
→ Format : feat(scope): description
→ Validation : dotnet build && dotnet test
```

### Fin d'itération
```
@product-manager Finaliser l'itération #12
→ Vérifie docs/project-state.md
→ Commit final
→ Push vers GitHub
→ Génère description PR
```

---

## Principes communs

- **Langue de communication :** Français
- **Langue du code :** Anglais
- **Architecture :** Clean Architecture (Uncle Bob) + DDD
- **Tests :** xUnit avec nomenclature `Should_[résultat]_When_[contexte]`
- **Documentation :** `docs/spec.md` (use cases, ADR) + `docs/project-state.md` (état)

---

## Activation

Pour utiliser ces agents dans GitHub Copilot Chat :
1. Assurez-vous que les fichiers JSON sont dans `.github/copilot-agents/`
2. Mentionnez l'agent avec `@nom-agent` dans le chat
3. Utilisez les prompts suggérés ou posez vos questions

---

## Contribution

Ces agents suivent les règles définies dans `AGENTS.md` et `.github/copilot-instructions.md`.
Toute modification doit rester alignée avec ces documents de référence.
