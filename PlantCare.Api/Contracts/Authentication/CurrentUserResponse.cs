namespace PlantCare.Api.Contracts.Authentication;

public sealed record CurrentUserResponse(Guid Id, string Email);