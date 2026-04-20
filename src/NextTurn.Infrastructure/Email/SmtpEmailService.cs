using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextTurn.Application.Common.Interfaces;

namespace NextTurn.Infrastructure.Email;

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<SmtpEmailOptions> options,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendWelcomeEmailAsync(
        string toEmail,
        string orgName,
        string temporaryPassword,
        CancellationToken cancellationToken)
    {
        var subject = "Welcome to NextTurn";
        var body =
            $"Your organisation '{orgName}' is ready. " +
            $"Use this temporary password to sign in and rotate it immediately: {temporaryPassword}";

        return SendAsync(toEmail, subject, body, cancellationToken);
    }

    public Task SendStaffInviteEmailAsync(
        string toEmail,
        string staffName,
        string invitePath,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var subject = "You are invited to NextTurn";
        var inviteUrl = ToAbsoluteFrontendUrl(invitePath);
        var body =
            $"Hi {staffName}, you were invited to join NextTurn staff access. " +
            $"Open this link to set your password: {inviteUrl}. " +
            $"This invite expires at {expiresAt:O}.";

        return SendAsync(toEmail, subject, body, cancellationToken);
    }

    public Task SendQueueJoinedEmailAsync(
        string toEmail,
        string queueName,
        int ticketNumber,
        int positionInQueue,
        int estimatedWaitSeconds,
        CancellationToken cancellationToken)
    {
        var etaMinutes = Math.Max(1, estimatedWaitSeconds / 60);
        var subject = "Queue joined successfully";
        var body =
            $"You joined '{queueName}'. " +
            $"Ticket: {ticketNumber}, current position: {positionInQueue}, " +
            $"estimated wait: about {etaMinutes} minute(s).";

        return SendAsync(toEmail, subject, body, cancellationToken);
    }

    public Task SendQueueTurnApproachingEmailAsync(
        string toEmail,
        string queueName,
        int ticketNumber,
        int positionInQueue,
        CancellationToken cancellationToken)
    {
        var subject = "Your turn is approaching";
        var body =
            $"Queue '{queueName}' update: ticket {ticketNumber} is now at position {positionInQueue}. " +
            "Please be ready.";

        return SendAsync(toEmail, subject, body, cancellationToken);
    }

    public Task SendAppointmentStatusEmailAsync(
        string toEmail,
        string userName,
        string notificationType,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        string officeName,
        string serviceName,
        CancellationToken cancellationToken)
    {
        var subject = $"Appointment {notificationType.ToLowerInvariant()}";
        var body =
            $"Hi {userName}, your appointment was {notificationType.ToLowerInvariant()}. " +
            $"Service: {serviceName}, Office: {officeName}, " +
            $"Slot: {slotStart:O} to {slotEnd:O}.";

        return SendAsync(toEmail, subject, body, cancellationToken);
    }

    private async Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(toEmail);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        _logger.LogInformation("Sending transactional email '{Subject}' to {Email} via SMTP host {Host}.", subject, toEmail, _options.Host);
        await client.SendMailAsync(message, cancellationToken);
    }

    private string ToAbsoluteFrontendUrl(string pathOrUrl)
    {
        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        if (string.IsNullOrWhiteSpace(_options.FrontendBaseUrl))
            return pathOrUrl;

        return $"{_options.FrontendBaseUrl.TrimEnd('/')}/{pathOrUrl.TrimStart('/')}";
    }
}
