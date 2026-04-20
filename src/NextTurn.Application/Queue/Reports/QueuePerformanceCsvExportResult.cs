namespace NextTurn.Application.Queue.Reports;

public sealed record QueuePerformanceCsvExportResult(
    string FileName,
    string ContentType,
    byte[] Content);
