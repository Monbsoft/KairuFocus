namespace KairuFocus.Domain.Pomodoro;

/// <summary>
/// Projection légère d'un sprint complété : son début et sa fin (UTC).
/// Renvoyée par le repository pour l'agrégation des statistiques de focus,
/// afin de ne pas matérialiser l'agrégat complet sur une fenêtre d'un an.
/// </summary>
public sealed record CompletedSprintInterval(DateTime StartedAt, DateTime EndedAt);
