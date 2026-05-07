using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryBackend.API.Middleware;
using VeterinaryBackend.Business.Common;
using VeterinaryBackend.Business.DTOs.SectionHead;
using VeterinaryBackend.Business.Services;

namespace VeterinaryBackend.API.Controllers;

[ApiController]
[Route("api/v1/section-heads")]
[Produces("application/json")]
public class SectionHeadsController : ControllerBase
{
    private readonly ISectionHeadService _service;

    public SectionHeadsController(ISectionHeadService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns section heads — public.
    /// Pass <c>?sectionId=N</c> to fetch the head tied to a specific section
    /// (used by the public section page).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SectionHeadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SectionHeadDto>>> GetAll(
        [FromQuery] bool onlyActive = true,
        [FromQuery] int? sectionId = null,
        CancellationToken ct = default)
    {
        var language = LanguageMiddleware.GetLanguage(HttpContext);

        if (sectionId.HasValue)
        {
            var single = await _service.GetBySectionAsync(sectionId.Value, language, onlyActive, ct);
            return Ok(single is null ? Array.Empty<SectionHeadDto>() : new[] { single });
        }

        var all = await _service.GetAllAsync(language, onlyActive, ct);
        return Ok(all);
    }

    /// <summary>Returns a single section head — admin.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(SectionHeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SectionHeadDto>> GetById(int id, CancellationToken ct = default)
    {
        var dto = await _service.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    /// <summary>Creates a new section head — admin.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(SectionHeadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SectionHeadDto>> Create(
        [FromBody] CreateSectionHeadDto dto,
        CancellationToken ct = default)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates a section head — admin.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(SectionHeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SectionHeadDto>> Update(
        int id,
        [FromBody] UpdateSectionHeadDto dto,
        CancellationToken ct = default)
    {
        var updated = await _service.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    /// <summary>Deletes a section head — admin.</summary>
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
