namespace NCBA.DCL.Services;

public interface IEmailService
{
    Task SendCheckerStatusChangedAsync(string toEmail, string userName, string dclNo, string status);
    Task SendExtensionApprovalRequestAsync(string toEmail, string userName, string deferralNumber, string requesterName);
    Task SendExtensionStatusUpdateAsync(string toEmail, string userName, string deferralNumber, string status);

    // ✅ NEW: Checker approval/return notifications (aligns with Node.js)
    Task SendCheckerApprovedAsync(string toEmail, string userName, string checklistId, string dclNo, string checkerName);
    Task SendCheckerReturnedAsync(string toEmail, string userName, string checklistId, string dclNo, string checkerName);
}

