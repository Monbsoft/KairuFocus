using KairuFocus.Domain.Common;

namespace KairuFocus.Application.Tickets;

public sealed record JiraTicketDto(
    string Key,
    string Summary,
    string Status,
    string? Priority);

public interface IJiraTicketService
{
    Task<Result<IReadOnlyList<JiraTicketDto>>> GetAssignedTicketsAsync(
        string baseUrl, string email, string apiToken,
        CancellationToken cancellationToken = default);
}
