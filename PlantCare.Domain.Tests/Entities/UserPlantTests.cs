using PlantCare.Domain.Entities;

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
}