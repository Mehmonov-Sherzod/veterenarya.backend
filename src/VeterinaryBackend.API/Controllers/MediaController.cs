using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinaryBackend.API.Middleware;
using VeterinaryBackend.Business.Common;
using VeterinaryBackend.Business.DTOs.Common;
using VeterinaryBackend.Business.DTOs.Media;
using VeterinaryBackend.Business.Services;

namespace VeterinaryBackend.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    /// <summary>
    /// Returns all uploaded files (admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(PagedResultDto<MediaFileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<MediaFileDto>>> GetAll(
        [FromQuery] PaginationQuery query,
        CancellationToken ct = default)
    {
        var result = await _mediaService.GetAllAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single file's metadata (admin only).
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(MediaFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MediaFileDto>> GetById(int id, CancellationToken ct = default)
    {
        var dto = await _mediaService.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Uploads a single file (any type — image, json, html, pdf, xlsx, docx, etc.).
    /// Admin only. Returns a public URL ready to use as Content.imageUrl or any link.
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Roles = AppRoles.Admin)]
    [Consumes("multipart/form-data")]
    [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
    [RequestSizeLimit(524_288_000)]
    [ProducesResponseType(typeof(MediaFileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MediaFileDto>> Upload(IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "File is required."
            });

        await using var stream = file.OpenReadStream();
        var dto = await _mediaService.UploadAsync(stream, file.FileName, file.ContentType, file.Length, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Deletes a file from disk and the database (admin only).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await _mediaService.DeleteAsync(id, ct);
        return NoContent();
    }
}
