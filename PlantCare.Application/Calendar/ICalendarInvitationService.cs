namespace PlantCare.Application.Calendar;

public interface ICalendarInvitationService
{
    Task<IReadOnlyList<CalendarShareDto>> GetOwnedAsync(Guid owner, CancellationToken ct = default);
    Task<CalendarShareDto?> CreateAsync(Guid owner, string email, CancellationToken ct = default);
    Task<bool?> ResendAsync(Guid owner, Guid invitationId, CancellationToken ct = default);
    Task<bool> RevokeAsync(Guid owner, Guid invitationId, CancellationToken ct = default);
    Task<CalendarShareDto?> AcceptAsync(Guid recipient, string token, CancellationToken ct = default);
}
public sealed record AcceptCalendarInvitationCommand(string Token);
