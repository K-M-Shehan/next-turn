namespace NextTurn.Infrastructure.Email;

public sealed class SmtpEmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string Provider { get; set; } = "Stub";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "NextTurn";
    public string FrontendBaseUrl { get; set; } = string.Empty;
}
