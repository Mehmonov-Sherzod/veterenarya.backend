using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryBackend.API.Middleware;
using VeterinaryBackend.Business.Common;
using VeterinaryBackend.Business.DTOs.Common;
using VeterinaryBackend.Business.DTOs.Content;
using VeterinaryBackend.Business.Services;

namespace VeterinaryBackend.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ContentsController : ControllerBase
{
    private readonly IContentService _contentService;

    public ContentsController(IContentService contentService)
    {
        _contentService = contentService;
    }

    /// <summary>
    /// Returns a paginated list of contents in the requested language.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ContentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ContentDto>>> GetAll(
        [FromQuery] PaginationQuery query,
        [FromQuery] bool onlyActive = true,
        [FromQuery] int? sectionId = null,
        CancellationToken ct = default)
    {
        var language = LanguageMiddleware.GetLanguage(HttpContext);
        var result = await _contentService.GetAllAsync(query, language, onlyActive, sectionId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single content in the requested language.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> GetById(int id, CancellationToken ct = default)
    {
        var language = LanguageMiddleware.GetLanguage(HttpContext);
        var result = await _contentService.GetByIdAsync(id, language, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single content with all language translations (admin only — used by the admin panel).
    /// </summary>
    [HttpGet("{id:int}/detail")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ContentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDetailDto>> GetDetail(int id, CancellationToken ct = default)
    {
        var result = await _contentService.GetDetailByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new content (admin endpoint).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ContentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ContentDetailDto>> Create(
        [FromBody] CreateContentDto dto,
        CancellationToken ct = default)
    {
        var created = await _contentService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetDetail), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing content (admin endpoint).
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ContentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDetailDto>> Update(
        int id,
        [FromBody] UpdateContentDto dto,
        CancellationToken ct = default)
    {
        var updated = await _contentService.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Deletes a content by id (admin endpoint).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await _contentService.DeleteAsync(id, ct);
        return NoContent();
    }
}
