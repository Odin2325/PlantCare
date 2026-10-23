namespace PlantCare.Application.Calendar;

public interface ICalendarShareEmailSender
{
    Task<bool> SendAsync(string ownerEmail, string recipientEmail, Guid shareId, CancellationToken cancellationToken = default);
    Task<bool> SendInvitationAsync(string ownerEmail, string recipientEmail, string token, CancellationToken cancellationToken = default);
}
