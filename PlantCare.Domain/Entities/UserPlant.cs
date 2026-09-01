using PlantCare.Domain.Enums;

namespace PlantCare.Domain.Entities;

public sealed class UserPlant
{
    public const int NicknameMaxLength = 100;
    public const int LocationMaxLength = 150;
    public const int NotesMaxLength = 2_000;

    private UserPlant()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid PlantSpeciesId { get; private set; }

    public string Nickname { get; private set; } = string.Empty;

    public string? Location { get; private set; }

    public DateOnly? AcquiredOn { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public PlantSpecies PlantSpecies { get; private set; } = null!;

    private readonly List<CareSchedule> _careSchedules = [];
    public IReadOnlyCollection<CareSchedule> CareSchedules => _careSchedules;

    public static UserPlant Create(
        Guid userId,
        Guid plantSpeciesId,
        string nickname,
        string? location,
        DateOnly? acquiredOn,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A valid user ID must be provided.", nameof(userId));
        }

        if (plantSpeciesId == Guid.Empty)
        {
            throw new ArgumentException("A valid plant species ID must be provided.", nameof(plantSpeciesId));
        }

        return new UserPlant
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlantSpeciesId = plantSpeciesId,
            Nickname = NormalizeRequired(nickname, nameof(nickname), NicknameMaxLength),
            Location = NormalizeOptional(location, nameof(location), LocationMaxLength),
            AcquiredOn = acquiredOn,
            Notes = NormalizeOptional(notes, nameof(notes), NotesMaxLength),
            IsActive = true,
            CreatedAtUtc = createdAtUtc
        };
    }

    public CareSchedule AddCareSchedule(CareActionType actionType, int intervalDays)
    {
        if (_careSchedules.Any(schedule => schedule.ActionType == actionType))
        {
            throw new InvalidOperationException($"A {actionType} schedule already exists for this plant.");
        }

        var schedule = CareSchedule.Create(Id, actionType, intervalDays);

        _careSchedules.Add(schedule);

        return schedule;
    }

    public void UpdateDetails(string nickname, string? location, DateOnly? acquiredOn, string? notes)
    {
        Nickname = NormalizeRequired(nickname, nameof(nickname), NicknameMaxLength);

        Location = NormalizeOptional(location, nameof(location), LocationMaxLength);

        AcquiredOn = acquiredOn;

        Notes = NormalizeOptional(notes, nameof(notes), NotesMaxLength);
    }

    public void Archive()
    {
        IsActive = false;
    }

    private static string NormalizeRequired(string value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The value cannot be empty.", parameterName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalizedValue;
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException($"The value cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalizedValue;
    }
}