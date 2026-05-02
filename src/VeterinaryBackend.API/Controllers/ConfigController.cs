using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VeterinaryBackend.Business.DTOs.Common;
using VeterinaryBackend.Business.Options;

namespace VeterinaryBackend.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ConfigController : ControllerBase
{
    private readonly IOptionsMonitor<SiteOptions> _siteMonitor;

    public ConfigController(IOptionsMonitor<SiteOptions> siteMonitor)
    {
        _siteMonitor = siteMonitor;
    }

    /// <summary>
    /// Returns public, non-sensitive configuration consumed by the admin panel
    /// (e.g. the public-facing frontend URL).
    /// </summary>
    [HttpGet("public")]
    [ProducesResponseType(typeof(PublicConfigDto), StatusCodes.Status200OK)]
    public ActionResult<PublicConfigDto> GetPublic()
    {
        var site = _siteMonitor.CurrentValue;
        return Ok(new PublicConfigDto
        {
            FrontendUrl = string.IsNullOrWhiteSpace(site.FrontendUrl) ? "/" : site.FrontendUrl
        });
    }
}
