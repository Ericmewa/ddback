using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace NCBA.DCL.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUser;
    private readonly string? _smtpPass;
    private readonly bool _smtpSecure;
    private readonly string? _emailFrom;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Read SMTP configuration from environment or appsettings
        _smtpHost = configuration["EmailSettings:SmtpHost"] ?? Environment.GetEnvironmentVariable("SMTP_HOST");
        _smtpPort = int.TryParse(configuration["EmailSettings:SmtpPort"] ?? Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
        _smtpUser = configuration["EmailSettings:SmtpUser"] ?? Environment.GetEnvironmentVariable("SMTP_USER");
        _smtpPass = configuration["EmailSettings:SmtpPass"] ?? Environment.GetEnvironmentVariable("SMTP_PASS");
        _smtpSecure = (configuration["EmailSettings:SmtpSecure"] ?? Environment.GetEnvironmentVariable("SMTP_SECURE") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
        _emailFrom = configuration["EmailSettings:EmailFrom"] ?? Environment.GetEnvironmentVariable("EMAIL_FROM");

        // Log configuration status
        LogEmailConfig();
    }

    private void LogEmailConfig()
    {
        _logger.LogInformation("📧 Email Service Config: " +
            $"SMTP_HOST: {(_smtpHost != null ? "✅ set" : "❌ missing")}, " +
            $"SMTP_PORT: {(_smtpPort > 0 ? "✅ set" : "❌ missing")}, " +
            $"SMTP_USER: {(_smtpUser != null ? "✅ set" : "❌ missing")}, " +
            $"SMTP_SECURE: {(_smtpSecure ? "✅ true" : "❌ false")}, " +
            $"EMAIL_FROM: {(_emailFrom != null ? $"✅ {_emailFrom}" : "❌ missing")}");
    }

    // ✅ Generic email sending method (aligns with Node.js sendEmail)
    private async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(htmlBody))
        {
            throw new ArgumentException("Email parameters: to, subject, and htmlBody are required");
        }

        // If SMTP not configured, log and return (non-blocking)
        if (string.IsNullOrWhiteSpace(_smtpHost) || string.IsNullOrWhiteSpace(_smtpUser))
        {
            _logger.LogWarning($"⚠️ SMTP not configured. Email not sent TO: {to} | Subject: {subject}");
            return;
        }

        try
        {
            using (var client = new SmtpClient(_smtpHost, _smtpPort))
            {
                client.EnableSsl = _smtpSecure;
                client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
                client.Timeout = 10000; // 10 second timeout

                using (var mailMessage = new MailMessage(_emailFrom ?? _smtpUser, to))
                {
                    mailMessage.Subject = subject;
                    mailMessage.Body = htmlBody;
                    mailMessage.IsBodyHtml = true;

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"✅ [EMAIL SENT] To: {to} | Subject: {subject}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Failed to send email to {to}. Subject: {subject}");
            // Non-blocking: don't throw, just log the error
        }
    }

    // ✅ HTML template for checker status changed
    private string GetCheckerStatusChangedHtml(string userName, string dclNo, string status)
    {
        return $@"
            <html>
                <body style=""font-family: Arial, sans-serif; color: #333;"">
                    <h2>DCL Status Update</h2>
                    <p>Hello {userName},</p>
                    <p>Your DCL <strong>{dclNo}</strong> status has been changed to <strong>{status}</strong>.</p>
                    <p>Please log in to the system to review the details.</p>
                    <hr>
                    <p>This is an automated email. Please do not reply.</p>
                </body>
            </html>";
    }

    // ✅ HTML template for extension approval request
    private string GetExtensionApprovalRequestHtml(string userName, string deferralNumber, string requesterName)
    {
        return $@"
            <html>
                <body style=""font-family: Arial, sans-serif; color: #333;"">
                    <h2>Extension Request</h2>
                    <p>Hello {userName},</p>
                    <p><strong>{requesterName}</strong> has requested an extension for Deferral <strong>{deferralNumber}</strong>.</p>
                    <p>Please review and take action in the system.</p>
                    <hr>
                    <p>This is an automated email. Please do not reply.</p>
                </body>
            </html>";
    }

    // ✅ HTML template for extension status update
    private string GetExtensionStatusUpdateHtml(string userName, string deferralNumber, string status)
    {
        return $@"
            <html>
                <body style=""font-family: Arial, sans-serif; color: #333;"">
                    <h2>Extension Request Update</h2>
                    <p>Hello {userName},</p>
                    <p>The extension request for Deferral <strong>{deferralNumber}</strong> has been <strong>{status}</strong>.</p>
                    <p>Please log in to the system to view the details.</p>
                    <hr>
                    <p>This is an automated email. Please do not reply.</p>
                </body>
            </html>";
    }

    // ✅ HTML template for checker approval
    private string GetCheckerApprovedHtml(string userName, string dclNo, string checkerName)
    {
        return $@"
            <html>
                <body style=""font-family: Arial, sans-serif; color: #333;"">
                    <h2>DCL Approved</h2>
                    <p>Hello {userName},</p>
                    <p>Your DCL <strong>{dclNo}</strong> has been <strong>approved</strong> by checker <strong>{checkerName}</strong>.</p>
                    <p>Thank you for your submission.</p>
                    <hr>
                    <p>This is an automated email. Please do not reply.</p>
                </body>
            </html>";
    }

    // ✅ HTML template for checker returned
    private string GetCheckerReturnedHtml(string userName, string dclNo, string checkerName)
    {
        return $@"
            <html>
                <body style=""font-family: Arial, sans-serif; color: #333;"">
                    <h2>DCL Returned for Revision</h2>
                    <p>Hello {userName},</p>
                    <p>Your DCL <strong>{dclNo}</strong> has been <strong>returned</strong> by checker <strong>{checkerName}</strong> for revision.</p>
                    <p>Please review the feedback and resubmit.</p>
                    <hr>
                    <p>This is an automated email. Please do not reply.</p>
                </body>
            </html>";
    }

    public async Task SendCheckerStatusChangedAsync(string toEmail, string userName, string dclNo, string status)
    {
        var subject = $"DCL {dclNo} Status Update";
        var htmlBody = GetCheckerStatusChangedHtml(userName, dclNo, status);
        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task SendExtensionApprovalRequestAsync(string toEmail, string userName, string deferralNumber, string requesterName)
    {
        var subject = $"Extension Request for {deferralNumber}";
        var htmlBody = GetExtensionApprovalRequestHtml(userName, deferralNumber, requesterName);
        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task SendExtensionStatusUpdateAsync(string toEmail, string userName, string deferralNumber, string status)
    {
        var subject = $"Extension Update for {deferralNumber}";
        var htmlBody = GetExtensionStatusUpdateHtml(userName, deferralNumber, status);
        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    // ✅ NEW: Checker approval notification (aligns with Node.js sendCheckerApproved)
    public async Task SendCheckerApprovedAsync(string toEmail, string userName, string checklistId, string dclNo, string checkerName)
    {
        var subject = $"DCL {dclNo} Approved by Checker";
        var htmlBody = GetCheckerApprovedHtml(userName, dclNo, checkerName);
        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    // ✅ NEW: Checker returned notification (aligns with Node.js sendCheckerReturned)
    public async Task SendCheckerReturnedAsync(string toEmail, string userName, string checklistId, string dclNo, string checkerName)
    {
        var subject = $"DCL {dclNo} Returned by Checker";
        var htmlBody = GetCheckerReturnedHtml(userName, dclNo, checkerName);
        await SendEmailAsync(toEmail, subject, htmlBody);
    }
}
