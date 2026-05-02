using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryBackend.API.Middleware;
using VeterinaryBackend.Business.Common;
using VeterinaryBackend.Business.DTOs.Section;
using VeterinaryBackend.Business.Services;

namespace VeterinaryBackend.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SectionsController : ControllerBase
{
    private readonly ISectionService _sectionService;

    public SectionsController(ISectionService sectionService)
    {
        _sectionService = sectionService;
    }

    /// <summary>Returns the full list of sections (localized) — public.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SectionDto>>> GetAll(
        [FromQuery] bool onlyActive = true,
        CancellationToken ct = default)
    {
        var language = LanguageMiddleware.GetLanguage(HttpContext);
        var result = await _sectionService.GetAllAsync(language, onlyActive, ct);
        return Ok(result);
    }

    /// <summary>Returns each section together with its localized content blocks — public.</summary>
    [HttpGet("with-contents")]
    [ProducesResponseType(typeof(IReadOnlyList<SectionWithContentsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SectionWithContentsDto>>> GetAllWithContents(
        [FromQuery] bool onlyActive = true,
        CancellationToken ct = default)
    {
        var language = LanguageMiddleware.GetLanguage(HttpContext);
        var result = await _sectionService.GetAllWithContentsAsync(language, onlyActive, ct);
        return Ok(result);
    }

    /// <summary>Returns a single section's full multilingual detail — admin.</summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(SectionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SectionDetailDto>> GetDetail(int id, CancellationToken ct = default)
    {
        var dto = await _sectionService.GetDetailByIdAsync(id, ct);
        return Ok(dto);
    }

    /// <summary>Creates a new section — admin.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(SectionDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SectionDetailDto>> Create(
        [FromBody] CreateSectionDto dto,
        CancellationToken ct = default)
    {
        var created = await _sectionService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetDetail), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing section — admin.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(SectionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SectionDetailDto>> Update(
        int id,
        [FromBody] UpdateSectionDto dto,
        CancellationToken ct = default)
    {
        var updated = await _sectionService.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    /// <summary>Deletes a section and all its content blocks — admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await _sectionService.DeleteAsync(id, ct);
        return NoContent();
    }
}
