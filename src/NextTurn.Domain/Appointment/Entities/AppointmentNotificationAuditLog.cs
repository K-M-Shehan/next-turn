namespace NextTurn.Domain.Appointment.Entities;

/// <summary>
/// Immutable audit row for appointment notification delivery attempts.
/// </summary>
public sealed class AppointmentNotificationAuditLog
{
    public Guid Id { get; }
    public Guid OrganisationId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid UserId { get; private set; }
    public string NotificationType { get; private set; }
    public string DeliveryStatus { get; private set; }
    public string RecipientEmail { get; private set; }
    public DateTimeOffset SlotStart { get; private set; }
    public DateTimeOffset SlotEnd { get; private set; }
    public string OfficeName { get; private set; }
    public string ServiceName { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private AppointmentNotificationAuditLog()
    {
        NotificationType = default!;
        DeliveryStatus = default!;
        RecipientEmail = default!;
        OfficeName = default!;
        ServiceName = default!;
    }

    private AppointmentNotificationAuditLog(
        Guid id,
        Guid organisationId,
        Guid appointmentId,
        Guid userId,
        string notificationType,
        string deliveryStatus,
        string recipientEmail,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        string officeName,
        string serviceName,
        string? errorMessage,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganisationId = organisationId;
        AppointmentId = appointmentId;
        UserId = userId;
        NotificationType = notificationType;
        DeliveryStatus = deliveryStatus;
        RecipientEmail = recipientEmail;
        SlotStart = slotStart;
        SlotEnd = slotEnd;
        OfficeName = officeName;
        ServiceName = serviceName;
        ErrorMessage = errorMessage;
        CreatedAt = createdAt;
    }

    public static AppointmentNotificationAuditLog Sent(
        Guid organisationId,
        Guid appointmentId,
        Guid userId,
        string notificationType,
        string recipientEmail,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        string officeName,
        string serviceName)
    {
        return new AppointmentNotificationAuditLog(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            appointmentId: appointmentId,
            userId: userId,
            notificationType: notificationType,
            deliveryStatus: "Sent",
            recipientEmail: recipientEmail,
            slotStart: slotStart,
            slotEnd: slotEnd,
            officeName: officeName,
            serviceName: serviceName,
            errorMessage: null,
            createdAt: DateTimeOffset.UtcNow);
    }

    public static AppointmentNotificationAuditLog Failed(
        Guid organisationId,
        Guid appointmentId,
        Guid userId,
        string notificationType,
        string recipientEmail,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        string officeName,
        string serviceName,
        string errorMessage)
    {
        var normalizedError = string.IsNullOrWhiteSpace(errorMessage)
            ? "Appointment notification delivery failed."
            : errorMessage.Trim();

        return new AppointmentNotificationAuditLog(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            appointmentId: appointmentId,
            userId: userId,
            notificationType: notificationType,
            deliveryStatus: "Failed",
            recipientEmail: recipientEmail,
            slotStart: slotStart,
            slotEnd: slotEnd,
            officeName: officeName,
            serviceName: serviceName,
            errorMessage: normalizedError.Length > 500 ? normalizedError[..500] : normalizedError,
            createdAt: DateTimeOffset.UtcNow);
    }
}
