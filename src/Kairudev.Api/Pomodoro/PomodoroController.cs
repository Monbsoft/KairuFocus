using Kairudev.Api.Pomodoro.Presenters;
using Kairudev.Application.Journal.CreateJournalEntry;
using Kairudev.Application.Pomodoro.CompleteSession;
using Kairudev.Application.Pomodoro.CreateTaskDuringSession;
using Kairudev.Application.Pomodoro.GetCurrentSession;
using Kairudev.Application.Pomodoro.GetSettings;
using Kairudev.Application.Pomodoro.GetSuggestedSessionType;
using Kairudev.Application.Pomodoro.InterruptSession;
using Kairudev.Application.Pomodoro.LinkTask;
using Kairudev.Application.Pomodoro.SaveSettings;
using Kairudev.Application.Pomodoro.StartSession;
using Kairudev.Application.Pomodoro.UpdateTaskStatus;
using Kairudev.Domain.Pomodoro;
using Kairudev.Domain.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Pomodoro;

[ApiController]
[Route("api/pomodoro")]
public sealed class PomodoroController : ControllerBase
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IPomodoroSettingsRepository _settingsRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICreateJournalEntryUseCase _journalUseCase;

    public PomodoroController(
        IPomodoroSessionRepository sessionRepository,
        IPomodoroSettingsRepository settingsRepository,
        ITaskRepository taskRepository,
        ICreateJournalEntryUseCase journalUseCase)
    {
        _sessionRepository = sessionRepository;
        _settingsRepository = settingsRepository;
        _taskRepository = taskRepository;
        _journalUseCase = journalUseCase;
    }

    // ── Settings ───────────────────────────────────────────────────────────

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var presenter = new GetSettingsHttpPresenter();
        await new GetSettingsInteractor(_settingsRepository, presenter)
            .Execute(cancellationToken);
        return presenter.Result!;
    }

    [HttpPut("settings")]
    public async Task<IActionResult> SaveSettings(
        [FromBody] SaveSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var presenter = new SaveSettingsHttpPresenter();
        await new SaveSettingsInteractor(_settingsRepository, presenter)
            .Execute(request, cancellationToken);
        return presenter.Result!;
    }

    // ── Session ────────────────────────────────────────────────────────────

    [HttpGet("session/suggested")]
    public async Task<IActionResult> GetSuggestedSessionType(CancellationToken cancellationToken)
    {
        var presenter = new GetSuggestedSessionTypeHttpPresenter();
        await new GetSuggestedSessionTypeInteractor(_sessionRepository, _settingsRepository, presenter)
            .Execute(cancellationToken);
        return presenter.Result!;
    }

    [HttpGet("session")]
    public async Task<IActionResult> GetCurrentSession(CancellationToken cancellationToken)
    {
        var presenter = new GetCurrentSessionHttpPresenter();
        await new GetCurrentSessionInteractor(_sessionRepository, presenter)
            .Execute(cancellationToken);
        return presenter.Result!;
    }

    [HttpPost("session")]
    public async Task<IActionResult> StartSession(
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var presenter = new StartSessionHttpPresenter();
        await new StartSessionInteractor(_sessionRepository, _settingsRepository, presenter, _journalUseCase)
            .Execute(new StartSessionRequest(type), cancellationToken);
        return presenter.Result!;
    }

    [HttpPatch("session/complete")]
    public async Task<IActionResult> CompleteSession(CancellationToken cancellationToken)
    {
        var presenter = new CompleteSessionHttpPresenter();
        await new CompleteSessionInteractor(_sessionRepository, _settingsRepository, presenter, _journalUseCase)
            .Execute(new CompleteSessionRequest(), cancellationToken);
        return presenter.Result!;
    }

    [HttpPatch("session/interrupt")]
    public async Task<IActionResult> InterruptSession(CancellationToken cancellationToken)
    {
        var presenter = new InterruptSessionHttpPresenter();
        await new InterruptSessionInteractor(_sessionRepository, presenter, _journalUseCase)
            .Execute(new InterruptSessionRequest(), cancellationToken);
        return presenter.Result!;
    }

    // ── Tasks within session ───────────────────────────────────────────────

    [HttpPost("session/tasks/{id:guid}")]
    public async Task<IActionResult> LinkTask(Guid id, CancellationToken cancellationToken)
    {
        var presenter = new LinkTaskHttpPresenter();
        await new LinkTaskInteractor(_sessionRepository, _taskRepository, presenter)
            .Execute(new LinkTaskRequest(id), cancellationToken);
        return presenter.Result!;
    }

    [HttpPost("session/tasks")]
    public async Task<IActionResult> CreateTaskDuringSession(
        [FromBody] CreateTaskDuringSessionRequest request,
        CancellationToken cancellationToken)
    {
        var presenter = new CreateTaskDuringSessionHttpPresenter();
        await new CreateTaskDuringSessionInteractor(_sessionRepository, _taskRepository, presenter)
            .Execute(request, cancellationToken);
        return presenter.Result!;
    }

    [HttpPatch("session/tasks/{id:guid}/status")]
    public async Task<IActionResult> UpdateTaskStatus(
        Guid id,
        [FromBody] UpdateTaskStatusBody body,
        CancellationToken cancellationToken)
    {
        var presenter = new UpdateTaskStatusHttpPresenter();
        await new UpdateTaskStatusInteractor(_sessionRepository, _taskRepository, presenter)
            .Execute(new UpdateTaskStatusRequest(id, body.TargetStatus), cancellationToken);
        return presenter.Result!;
    }
}

/// <summary>Body DTO for PATCH /session/tasks/{id}/status</summary>
public sealed record UpdateTaskStatusBody(string TargetStatus);
