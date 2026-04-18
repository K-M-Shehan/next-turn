namespace NextTurn.Application.Common.Interfaces;

/// <summary>
/// Contract for sending transactional emails from the application layer.
/// The real SMTP/SendGrid implementation is deferred to a later sprint.
/// In Sprint 1, a no-op stub is registered that logs instead of sending.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a welcome email to a newly registered organisation's admin account,
    /// containing their temporary password and next-steps instructions.
    /// </summary>
    Task SendWelcomeEmailAsync(
        string toEmail,
        string orgName,
        string temporaryPassword,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends a staff invite email containing a secure invite acceptance link.
    /// </summary>
    Task SendStaffInviteEmailAsync(
        string toEmail,
        string staffName,
        string invitePath,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends a queue turn-approaching notification when a user is close to being served.
    /// </summary>
    Task SendQueueTurnApproachingEmailAsync(
        string toEmail,
        string queueName,
        int ticketNumber,
        int positionInQueue,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends appointment status updates (booked, rescheduled, cancelled) with slot and context details.
    /// </summary>
    Task SendAppointmentStatusEmailAsync(
        string toEmail,
        string userName,
        string notificationType,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        string officeName,
        string serviceName,
        CancellationToken cancellationToken);
}
