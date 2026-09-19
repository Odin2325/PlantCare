using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Domain.Tests.Entities;

public sealed class UserPlantTests
{
    [Fact]
    public void Create_WithValidValues_CreatesActiveUserPlant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var plantSpeciesId = Guid.NewGuid();
        var createdAtUtc =
            new DateTimeOffset(
                2026,
                8,
                6,
                12,
                0,
                0,
                TimeSpan.Zero);

        // Act
        var userPlant = UserPlant.Create(
            userId: userId,
            plantSpeciesId: plantSpeciesId,
            nickname: "  Living Room Monstera  ",
            location: "  Living room window  ",
            acquiredOn: new DateOnly(2026, 8, 1),
            notes: "  First plant  ",
            createdAtUtc: createdAtUtc);

        // Assert
        Assert.NotEqual(Guid.Empty, userPlant.Id);
        Assert.Equal(userId, userPlant.UserId);
        Assert.Equal(
            plantSpeciesId,
            userPlant.PlantSpeciesId);

        Assert.Equal(
            "Living Room Monstera",
            userPlant.Nickname);

        Assert.Equal(
            "Living room window",
            userPlant.Location);

        Assert.Equal("First plant", userPlant.Notes);
        Assert.True(userPlant.IsActive);
        Assert.Equal(createdAtUtc, userPlant.CreatedAtUtc);
    }

    [Fact]
    public void Create_WithEmptyNickname_ThrowsException()
    {
        var action = () => UserPlant.Create(
            userId: Guid.NewGuid(),
            plantSpeciesId: Guid.NewGuid(),
            nickname: " ",
            location: null,
            acquiredOn: null,
            notes: null,
            createdAtUtc: DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsException()
    {
        var action = () => UserPlant.Create(
            userId: Guid.Empty,
            plantSpeciesId: Guid.NewGuid(),
            nickname: "My plant",
            location: null,
            acquiredOn: null,
            notes: null,
            createdAtUtc: DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Archive_SetsIsActiveToFalse()
    {
        var userPlant = UserPlant.Create(
            userId: Guid.NewGuid(),
            plantSpeciesId: Guid.NewGuid(),
            nickname: "My plant",
            location: null,
            acquiredOn: null,
            notes: null,
            createdAtUtc: DateTimeOffset.UtcNow);

        userPlant.Archive();

        Assert.False(userPlant.IsActive);
    }

    [Fact]
    public void AddCareSchedule_SetsInitialDueDateFromStartDate()
    {
        var startsAt = new DateTimeOffset(
            2026,
            8,
            6,
            12,
            0,
            0,
            TimeSpan.Zero);

        var userPlant = UserPlant.Create(
            userId: Guid.NewGuid(),
            plantSpeciesId: Guid.NewGuid(),
            nickname: "My plant",
            location: null,
            acquiredOn: null,
            notes: null,
            createdAtUtc: startsAt);

        var schedule = userPlant.AddCareSchedule(
            CareActionType.Watering,
            7,
            startsAt);

        Assert.Equal(startsAt.AddDays(7), schedule.NextDueAtUtc);
    }

    [Fact]
    public void ArchiveAndRestoreCareSchedule_PreservesHistoryState()
    {
        var startsAt = new DateTimeOffset(
            2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var userPlant = UserPlant.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "My plant",
            null,
            null,
            null,
            startsAt);
        var schedule = userPlant.AddCareSchedule(
            CareActionType.Fertilizing,
            30,
            startsAt);
        var completedAt = startsAt.AddDays(5);
        schedule.MarkCompleted(completedAt);

        userPlant.ArchiveCareSchedule(
            CareActionType.Fertilizing);
        var restored = userPlant.AddOrRestoreCareSchedule(
            CareActionType.Fertilizing,
            45,
            startsAt.AddDays(10));

        Assert.Same(schedule, restored);
        Assert.False(restored.IsArchived);
        Assert.True(restored.IsEnabled);
        Assert.Equal(completedAt, restored.LastCompletedAtUtc);
        Assert.Equal(45, restored.IntervalDays);
        Assert.Equal(
            completedAt.AddDays(45),
            restored.NextDueAtUtc);
    }
}
