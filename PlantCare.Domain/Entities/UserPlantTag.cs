namespace PlantCare.Domain.Entities;

public sealed class UserPlantTag
{
    public const int NameMaxLength = 32;

    private UserPlantTag()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserPlantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    internal static UserPlantTag Create(Guid userPlantId, string name)
    {
        return new UserPlantTag
        {
            Id = Guid.NewGuid(),
            UserPlantId = userPlantId,
            Name = name
        };
    }
}
