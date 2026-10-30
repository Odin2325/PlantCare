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
    public void Restore_SetsIsActiveToTrue()
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

        userPlant.Restore();

        Assert.True(userPlant.IsActive);
    }

    [Fact]
    public void ReplaceTags_NormalizesAndDeduplicatesTags()
    {
        var userPlant = UserPlant.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Fern", null, null, null,
            DateTimeOffset.UtcNow);

        userPlant.ReplaceTags([" tropical ", "Favorite", "TROPICAL"]);

        Assert.Equal(2, userPlant.Tags.Count);
        Assert.Contains(userPlant.Tags, tag => tag.Name == "tropical");
        Assert.Contains(userPlant.Tags, tag => tag.Name == "Favorite");
    }

    [Fact]
    public void ReplaceTags_WithTooManyTags_ThrowsException()
    {
        var userPlant = UserPlant.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Fern", null, null, null,
            DateTimeOffset.UtcNow);

        var action = () => userPlant.ReplaceTags(
            Enumerable.Range(1, UserPlant.MaximumTagCount + 1)
                .Select(index => $"tag-{index}"));

        Assert.Throws<ArgumentException>(action);
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
    [Fact]
    public void GrowingPlanAllowsPartialDatesAndDoesNotChangeAcquiredDateOrCare()
    {
        var plant = UserPlant.Create(Guid.NewGuid(), Guid.NewGuid(), "Crop", null, new DateOnly(2026, 1, 1), null, DateTimeOffset.UtcNow);
        var schedule = plant.AddCareSchedule(CareActionType.Watering, 7, DateTimeOffset.UtcNow);
        var due = schedule.NextDueAtUtc;
        plant.UpdateGrowingPlan(null, new DateOnly(2027, 5, 1), new DateOnly(2027, 8, 1));
        Assert.Null(plant.PlannedSowingOn); Assert.Equal(new DateOnly(2027, 5, 1), plant.PlannedPlantingOutOn);
        Assert.Equal(new DateOnly(2026, 1, 1), plant.AcquiredOn); Assert.Equal(due, schedule.NextDueAtUtc);
        plant.UpdateGrowingPlan(null, null, null); Assert.Null(plant.PlannedPlantingOutOn); Assert.Null(plant.ExpectedHarvestOn);
    }
    [Theory]
    [InlineData("2027-05-01", "2027-04-01", null)]
    [InlineData("2027-05-01", null, "2027-04-01")]
    [InlineData(null, "2027-05-01", "2027-04-01")]
    public void GrowingPlanRejectsReversedDatesAtomically(string? sowing, string? planting, string? harvest)
    {
        var plant = UserPlant.Create(Guid.NewGuid(), Guid.NewGuid(), "Crop", null, null, null, DateTimeOffset.UtcNow);
        var saved = new DateOnly(2027, 1, 1); plant.UpdateGrowingPlan(saved, null, null);
        Assert.Throws<ArgumentException>(() => plant.UpdateGrowingPlan(sowing is null ? null : DateOnly.Parse(sowing), planting is null ? null : DateOnly.Parse(planting), harvest is null ? null : DateOnly.Parse(harvest)));
        Assert.Equal(saved, plant.PlannedSowingOn); Assert.Null(plant.PlannedPlantingOutOn);
    }
    [Fact]
    public void ArchivedPlanIsPreservedAndCannotBeEditedUntilRestored()
    {
        var plant = UserPlant.Create(Guid.NewGuid(), Guid.NewGuid(), "Crop", null, null, null, DateTimeOffset.UtcNow);
        var harvest = new DateOnly(2027, 8, 1); plant.UpdateGrowingPlan(null, null, harvest); plant.Archive();
        Assert.Throws<InvalidOperationException>(() => plant.UpdateGrowingPlan(null, null, null));
        plant.Restore(); Assert.Equal(harvest, plant.ExpectedHarvestOn);
    }

    [Fact]
    public void GrowingContextValidatesBothFieldsBeforeChangingState()
    {
        var plant=UserPlant.Create(Guid.NewGuid(),Guid.NewGuid(),"Test",null,null,null,DateTimeOffset.UtcNow);
        foreach(var pair in new[] {("indoor","spring"),("balcony","summer"),("greenhouse","autumn"),("outdoor","winter")}) {
            plant.UpdateGrowingContext(pair.Item1,pair.Item2);Assert.Equal(pair.Item1,plant.GrowingEnvironment);Assert.Equal(pair.Item2,plant.CareSeason);
        }
        Assert.Throws<ArgumentException>(()=>plant.UpdateGrowingContext("indoor","invalid"));Assert.Equal("outdoor",plant.GrowingEnvironment);
        Assert.Throws<ArgumentException>(()=>plant.UpdateGrowingContext("invalid","spring"));Assert.Equal("winter",plant.CareSeason);
        plant.UpdateGrowingContext(null,null);Assert.Null(plant.GrowingEnvironment);Assert.Null(plant.CareSeason);
    }

}
