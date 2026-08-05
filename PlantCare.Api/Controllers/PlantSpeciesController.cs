using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Contracts.PlantTypes;
using PlantCare.Application.PlantCatalog;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/plant-species")]
public sealed class PlantSpeciesController(
    IPlantSpeciesService plantSpeciesService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<PlantSpeciesDto>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<IReadOnlyList<PlantSpeciesDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var plantSpecies =
            await plantSpeciesService.GetAllAsync(
                cancellationToken);

        return Ok(plantSpecies);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(PlantSpeciesDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantSpeciesDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var plantSpecies =
            await plantSpeciesService.GetByIdAsync(
                id,
                cancellationToken);

        if (plantSpecies is null)
        {
            return NotFound();
        }

        return Ok(plantSpecies);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(PlantSpeciesDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlantSpeciesDto>> Create(
        CreatePlantSpeciesRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePlantSpeciesCommand(
            CommonName: request.CommonName,
            ScientificName: request.ScientificName,
            Description: request.Description,
            SunlightRequirement:
                request.SunlightRequirement,
            SunlightInstructions:
                request.SunlightInstructions,
            DefaultWateringIntervalDays:
                request.DefaultWateringIntervalDays,
            WateringInstructions:
                request.WateringInstructions,
            DefaultFertilizingIntervalDays:
                request.DefaultFertilizingIntervalDays,
            FertilizingInstructions:
                request.FertilizingInstructions,
            SoilInstructions:
                request.SoilInstructions,
            HumidityInstructions:
                request.HumidityInstructions,
            MinimumTemperatureCelsius:
                request.MinimumTemperatureCelsius,
            MaximumTemperatureCelsius:
                request.MaximumTemperatureCelsius,
            IsToxicToPets:
                request.IsToxicToPets);

        var createdPlantSpecies =
            await plantSpeciesService.CreateAsync(
                command,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdPlantSpecies.Id },
            createdPlantSpecies);
    }
}