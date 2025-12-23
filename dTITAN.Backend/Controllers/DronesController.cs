using Microsoft.AspNetCore.Mvc;
using dTITAN.Backend.Services.Domain;
using dTITAN.Backend.Data.DTO;

namespace dTITAN.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DronesController : ControllerBase
{
    private readonly IDroneService _droneService;

    public DronesController(IDroneService droneService)
    {
        _droneService = droneService;
    }

    [HttpPost]
    public async Task<IActionResult> AddDrone([FromBody] DroneTelemetry drone)
    {
        var createdDrone = await _droneService.AddDroneAsync(drone);
        return Ok(new { createdDrone.Id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDrone(string id)
    {
        var drone = await _droneService.GetDroneAsync(id);
        if (drone == null) return NotFound();
        return Ok(drone);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllDrones()
    {
        var drones = await _droneService.GetAllDronesAsync();
        return Ok(drones);
    }
}
