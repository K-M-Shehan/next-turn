using NextTurn.Domain.Queue.Enums;

namespace NextTurn.Application.Queue.DailySummary;

public sealed record DailyQueueSummaryAggregationPoint(
    DateOnly Date,
    Guid OfficeId,
    string OfficeName,
    Guid ServiceId,
    string ServiceName,
    QueueActionType ActionType,
    int Count);
