namespace NextTurn.Application.Queue.Reports;

public sealed record QueuePerformanceFilter(
    Guid OrganisationId,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? ServiceId,
    Guid? OfficeId);
