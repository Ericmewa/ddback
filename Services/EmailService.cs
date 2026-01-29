using Microsoft.Extensions.Logging;

namespace NCBA.DCL.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendCheckerStatusChangedAsync(string toEmail, string userName, string dclNo, string status)
    {
        _logger.LogInformation($"[MOCK EMAIL] To: {toEmail}, Name: {userName}, Subject: DCL {dclNo} Status Update, Body: Status changed to {status}");
        return Task.CompletedTask;
    }
}
