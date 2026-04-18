using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NextTurn.Application.Common.Interfaces;
using AppointmentNotificationAuditLog = NextTurn.Domain.Appointment.Entities.AppointmentNotificationAuditLog;

namespace NextTurn.Application.Appointment.Notifications;

public sealed class AppointmentNotificationService : IAppointmentNotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AppointmentNotificationService> _logger;

    public AppointmentNotificationService(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<AppointmentNotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public Task SendBookingConfirmationAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        return SendAsync(appointmentId, "Booked", cancellationToken);
    }

    public Task SendRescheduleUpdateAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        return SendAsync(appointmentId, "Rescheduled", cancellationToken);
    }

    public Task SendCancellationUpdateAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        return SendAsync(appointmentId, "Cancelled", cancellationToken);
    }

    private async Task SendAsync(Guid appointmentId, string notificationType, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            _logger.LogWarning("Appointment notification skipped. Appointment {AppointmentId} not found.", appointmentId);
            return;
        }

        var user = await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == appointment.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Appointment notification skipped. User {UserId} missing or inactive.", appointment.UserId);
            return;
        }

        if (!IsNotificationEnabled(user, notificationType))
            return;

        var profile = await _context.AppointmentProfiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == appointment.AppointmentProfileId, cancellationToken);

        var organisation = await _context.Organisations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == appointment.OrganisationId, cancellationToken);

        var officeName = organisation?.Name ?? "Unknown office";
        var serviceName = profile?.Name ?? "General appointment";

        try
        {
            await _emailService.SendAppointmentStatusEmailAsync(
                toEmail: user.Email.Value,
                userName: user.Name,
                notificationType: notificationType,
                slotStart: appointment.SlotStart,
                slotEnd: appointment.SlotEnd,
                officeName: officeName,
                serviceName: serviceName,
                cancellationToken: cancellationToken);

            _context.AppointmentNotificationAuditLogs.Add(
                AppointmentNotificationAuditLog.Sent(
                    organisationId: appointment.OrganisationId,
                    appointmentId: appointment.Id,
                    userId: user.Id,
                    notificationType: notificationType,
                    recipientEmail: user.Email.Value,
                    slotStart: appointment.SlotStart,
                    slotEnd: appointment.SlotEnd,
                    officeName: officeName,
                    serviceName: serviceName));

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send appointment {NotificationType} notification for appointment {AppointmentId}.",
                notificationType,
                appointment.Id);

            _context.AppointmentNotificationAuditLogs.Add(
                AppointmentNotificationAuditLog.Failed(
                    organisationId: appointment.OrganisationId,
                    appointmentId: appointment.Id,
                    userId: user.Id,
                    notificationType: notificationType,
                    recipientEmail: user.Email.Value,
                    slotStart: appointment.SlotStart,
                    slotEnd: appointment.SlotEnd,
                    officeName: officeName,
                    serviceName: serviceName,
                    errorMessage: ex.Message));

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsNotificationEnabled(Domain.Auth.Entities.User user, string notificationType)
    {
        return notificationType switch
        {
            "Booked" => user.AppointmentBookedNotificationsEnabled,
            "Rescheduled" => user.AppointmentRescheduledNotificationsEnabled,
            "Cancelled" => user.AppointmentCancelledNotificationsEnabled,
            _ => false,
        };
    }
}
