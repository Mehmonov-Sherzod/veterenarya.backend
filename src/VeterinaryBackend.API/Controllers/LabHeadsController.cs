using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryBackend.API.Middleware;
using VeterinaryBackend.Business.Common;
using VeterinaryBackend.Business.DTOs.LabHead;
using VeterinaryBackend.Business.Services;

namespace VeterinaryBackend.API.Controllers;

[ApiController]
[Route("api/v1/lab-heads")]
[Produces("application/json")]
public class LabHeadsController : ControllerBase
{
    private readonly ILabHeadService _service;

    public LabHeadsController(ILabHeadService service)
    {
        _service = service;
    }

    /// <summary>Returns all lab department heads — public.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LabHeadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LabHeadDto>>> GetAll(
        [FromQuery] bool onlyActive = true,
        CancellationToken ct = default)
    {
        var result = await _service.GetAllAsync(onlyActive, ct);
        return Ok(result);
    }

    /// <summary>Returns a single lab head — admin.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(LabHeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LabHeadDto>> GetById(int id, CancellationToken ct = default)
    {
        var dto = await _service.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    /// <summary>Creates a new lab head — admin.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(LabHeadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LabHeadDto>> Create(
        [FromBody] CreateLabHeadDto dto,
        CancellationToken ct = default)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates a lab head — admin.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(LabHeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LabHeadDto>> Update(
        int id,
        [FromBody] UpdateLabHeadDto dto,
        CancellationToken ct = default)
    {
        var updated = await _service.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    /// <summary>Deletes a lab head — admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
