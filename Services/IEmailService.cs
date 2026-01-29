namespace NCBA.DCL.Services;

public interface IEmailService
{
    Task SendCheckerStatusChangedAsync(string toEmail, string userName, string dclNo, string status);
}
