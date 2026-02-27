# Script de migration automatisée des tests
# Usage: .\migrate-tests.ps1

Write-Host "🧹 Migration des tests vers le pattern Handler" -ForegroundColor Cyan
Write-Host ""

# Fonction pour afficher le mapping
function Show-TestMapping {
    param([string]$OldPath, [string]$NewPath)
    $oldFile = Split-Path -Leaf $OldPath
    $newFile = Split-Path -Leaf $NewPath
    Write-Host "  📝 " -NoNewline -ForegroundColor Yellow
    Write-Host "$oldFile" -NoNewline -ForegroundColor Gray
    Write-Host " → " -NoNewline -ForegroundColor White
    Write-Host "$newFile" -ForegroundColor Green
}

# Mapping des tests Tasks
$tasksMigrations = @{
    "tests\Kairudev.Application.Tests\Tasks\ListTasksInteractorTests.cs" = "tests\Kairudev.Application.Tests\Tasks\ListTasksQueryHandlerTests.cs"
    "tests\Kairudev.Application.Tests\Tasks\CompleteTaskInteractorTests.cs" = "tests\Kairudev.Application.Tests\Tasks\CompleteTaskCommandHandlerTests.cs"
    "tests\Kairudev.Application.Tests\Tasks\DeleteTaskInteractorTests.cs" = "tests\Kairudev.Application.Tests\Tasks\DeleteTaskCommandHandlerTests.cs"
    "tests\Kairudev.Application.Tests\Tasks\UpdateTaskInteractorTests.cs" = "tests\Kairudev.Application.Tests\Tasks\UpdateTaskCommandHandlerTests.cs"
    "tests\Kairudev.Application.Tests\Tasks\ChangeTaskStatusInteractorTests.cs" = "tests\Kairudev.Application.Tests\Tasks\ChangeTaskStatusCommandHandlerTests.cs"
}

# Mapping des tests Journal
$journalMigrations = @{
    "tests\Kairudev.Application.Tests\Journal\GetTodayJournalInteractorTests.cs" = "tests\Kairudev.Application.Tests\Journal\GetTodayJournalQueryHandlerTests.cs"
    "tests\Kairudev.Application.Tests\Journal\AddJournalCommentInteractorTests.cs" = "tests\Kairudev.Application.Tests\Journal\AddCommentCommandHandlerTests.cs"
    "tests\Kairudev.Application.Tests\Journal\UpdateJournalCommentInteractorTests.cs" = "tests\Kairudev.Application.Tests\Journal\UpdateCommentCommandHandlerTests.cs"
    "tests\Kairudev.Application.Tests\Journal\RemoveJournalCommentInteractorTests.cs" = "tests\Kairudev.Application.Tests\Journal\RemoveCommentCommandHandlerTests.cs"
}

# Mapping des tests Pomodoro
$pomodoroMigrations = @{
    "tests\Kairudev.Application.Tests\Pomodoro\StartSessionInteractorTests.cs" = "tests\Kairudev.Application.Tests\Pomodoro\StartSessionCommandHandlerTests.cs"
    "tests\Kairudev.Application.Tests\Pomodoro\CompleteSessionInteractorTests.cs" = "tests\Kairudev.Application.Tests\Pomodoro\CompleteSessionCommandHandlerTests.cs"
    "tests\Kairudev.Application.Tests\Pomodoro\InterruptSessionInteractorTests.cs" = "tests\Kairudev.Application.Tests\Pomodoro\InterruptSessionCommandHandlerTests.cs"
    "tests\Kairudev.Application.Tests\Pomodoro\SaveSettingsInteractorTests.cs" = "tests\Kairudev.Application.Tests\Pomodoro\SaveSettingsCommandHandlerTests.cs"
}

Write-Host "📋 Plan de migration :" -ForegroundColor Cyan
Write-Host ""
Write-Host "Tasks (5 fichiers):" -ForegroundColor Yellow
foreach ($migration in $tasksMigrations.GetEnumerator()) {
    Show-TestMapping $migration.Key $migration.Value
}

Write-Host ""
Write-Host "Journal (4 fichiers):" -ForegroundColor Yellow
foreach ($migration in $journalMigrations.GetEnumerator()) {
    Show-TestMapping $migration.Key $migration.Value
}

Write-Host ""
Write-Host "Pomodoro (4 fichiers):" -ForegroundColor Yellow
foreach ($migration in $pomodoroMigrations.GetEnumerator()) {
    Show-TestMapping $migration.Key $migration.Value
}

Write-Host ""
Write-Host "⚠️  ATTENTION ⚠️" -ForegroundColor Red
Write-Host "Ce script affiche uniquement le plan de migration." -ForegroundColor Yellow
Write-Host "La migration du contenu des tests doit être faite manuellement" -ForegroundColor Yellow
Write-Host "en suivant le guide : docs/test-migration-guide.md" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ AddTaskCommandHandlerTests.cs est déjà migré (exemple de référence)" -ForegroundColor Green
Write-Host ""

# Optionnel : lister les anciens fichiers qui existent encore
Write-Host "📂 Fichiers à migrer (existants) :" -ForegroundColor Cyan
$allMigrations = $tasksMigrations + $journalMigrations + $pomodoroMigrations
$existingFiles = @()

foreach ($migration in $allMigrations.GetEnumerator()) {
    if (Test-Path $migration.Key) {
        $existingFiles += $migration.Key
        Write-Host "  ✓ $($migration.Key)" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "Total à migrer : $($existingFiles.Count) fichiers" -ForegroundColor Cyan
