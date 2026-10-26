using PlantCare.Domain.Entities;
namespace PlantCare.Domain.Tests.Entities;

public sealed class HarvestRecordTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
    [Fact]
    public void RecordsDateAndOptionalQuantityWithoutInventingCare()
    {
        var plant = Guid.NewGuid();
        var r = HarvestRecord.Create(plant, new(2026, 10, 25), 1.001m, " kg ", " First crop ", Now);
        Assert.Equal(plant, r.UserPlantId);
        Assert.NotEqual(Guid.Empty, r.Id);
        Assert.Equal("kg", r.Unit);
        Assert.Equal("First crop", r.Notes);
        Assert.Equal(1.001m, r.Quantity);
        var notesOnly = HarvestRecord.Create(plant, new(2026, 10, 25), null, null, " ", Now);
        Assert.Null(notesOnly.Quantity);
        Assert.Null(notesOnly.Unit);
        Assert.Null(notesOnly.Notes);
    }
    [Theory]
    [InlineData(0, "kg")]
    [InlineData(-1, "g")]
    [InlineData(1000001, "g")]
    [InlineData(1, "litres")]
    [InlineData(1, null)]
    [InlineData(1.5, "items")]
    [InlineData(0.0001, "g")]
    public void RejectsInvalidQuantity(double qty, string? unit) => Assert.Throws<ArgumentException>(() => HarvestRecord.Create(Guid.NewGuid(), new(2026, 10, 25), (decimal)qty, unit, null, Now));
    [Fact]
    public void RejectsMissingDateFutureDateUnitWithoutQuantityAndLongNotes()
    {
        Assert.Throws<ArgumentException>(() => HarvestRecord.Create(Guid.Empty, new(2026, 10, 25), null, null, null, Now));
        Assert.Throws<ArgumentException>(() => HarvestRecord.Create(Guid.NewGuid(), default, null, null, null, Now));
        Assert.Throws<ArgumentException>(() => HarvestRecord.Create(Guid.NewGuid(), new(2026, 10, 28), null, null, null, Now));
        Assert.Throws<ArgumentException>(() => HarvestRecord.Create(Guid.NewGuid(), new(2026, 10, 25), null, "g", null, Now));
        Assert.Throws<ArgumentException>(() => HarvestRecord.Create(Guid.NewGuid(), new(2026, 10, 25), null, null, new string('a', 1001), Now));
        Assert.Equal(new DateOnly(2026, 10, 27), HarvestRecord.Create(Guid.NewGuid(), new(2026, 10, 27), null, null, null, Now).HarvestedOn);
    }
}