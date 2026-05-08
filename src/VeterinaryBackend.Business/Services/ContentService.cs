using FluentValidation;
using Microsoft.Extensions.Logging;
using VeterinaryBackend.Business.DTOs.Common;
using VeterinaryBackend.Business.DTOs.Content;
using VeterinaryBackend.Business.Mappers;
using VeterinaryBackend.DataAccess.Repositories;
using VeterinaryBackend.DataAccess.UnitOfWork;
using VeterinaryBackend.Domain.Common;
using VeterinaryBackend.Domain.Exceptions;

namespace VeterinaryBackend.Business.Services;

public class ContentService : IContentService
{
    private readonly IContentRepository _contentRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateContentDto> _createValidator;
    private readonly IValidator<UpdateContentDto> _updateValidator;
    private readonly ITelegramBotService _bot;
    private readonly ILogger<ContentService> _logger;

    public ContentService(
        IContentRepository contentRepository,
        ISectionRepository sectionRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateContentDto> createValidator,
        IValidator<UpdateContentDto> updateValidator,
        ITelegramBotService bot,
        ILogger<ContentService> logger)
    {
        _contentRepository = contentRepository;
        _sectionRepository = sectionRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _bot = bot;
        _logger = logger;
    }

    public async Task<PagedResultDto<ContentDto>> GetAllAsync(
        PaginationQuery query, Language language, bool onlyActive, int? sectionId, CancellationToken ct = default)
    {
        var paged = await _contentRepository.GetPagedWithSectionAsync(
            query.Page, query.PageSize, sectionId, onlyActive ? true : (bool?)null, ct);

        return paged.ToLocalizedPagedDto(language);
    }

    public async Task<ContentDto> GetByIdAsync(int id, Language language, CancellationToken ct = default)
    {
        var entity = await _contentRepository.GetByIdWithSectionAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Content), id);

        return entity.ToLocalizedDto(language);
    }

    public async Task<ContentDetailDto> GetDetailByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _contentRepository.GetByIdWithSectionAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Content), id);

        return entity.ToDetailDto();
    }

    public async Task<ContentDetailDto> CreateAsync(CreateContentDto dto, CancellationToken ct = default)
    {
        var result = await _createValidator.ValidateAsync(dto, ct);
        if (!result.IsValid)
            throw new ValidationAppException(BuildErrors(result));

        await EnsureSectionExistsAsync(dto.SectionId!.Value, ct);

        var entity = dto.ToEntity();
        await _contentRepository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Fire-and-forget Telegram broadcast — admin should not feel a slow Telegram
        // round-trip and a bot outage must never break content creation.
        _ = Task.Run(async () =>
        {
            try
            {
                var title = !string.IsNullOrWhiteSpace(entity.TitleUz) ? entity.TitleUz : entity.TitleRu;
                var excerpt = ExtractExcerpt(entity.DescriptionUz, entity.DescriptionRu);
                await _bot.BroadcastContentAsync(entity.Id, title, excerpt, entity.ImageUrl, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram broadcast failed for content {ContentId}.", entity.Id);
            }
        });

        return entity.ToDetailDto();
    }

    /// <summary>
    /// Strip the admin block-editor prefix (__VS_BLOCKS_V1__[...]) and concatenate
    /// every <c>"t":"text"</c> block so the bot caption shows the full description
    /// (image-only blocks are skipped). Trim to ~800 chars so the link still fits.
    /// </summary>
    private static string? ExtractExcerpt(string uz, string ru)
    {
        var raw = !string.IsNullOrWhiteSpace(uz) ? uz : ru;
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (raw.StartsWith("__VS_BLOCKS_V1__", StringComparison.Ordinal))
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(
                raw, "\"t\"\\s*:\\s*\"text\"\\s*,\\s*\"v\"\\s*:\\s*\"([^\"]+)\"");
            if (matches.Count > 0)
            {
                raw = string.Join("\n\n",
                    matches.Cast<System.Text.RegularExpressions.Match>()
                          .Select(m => System.Text.RegularExpressions.Regex.Unescape(m.Groups[1].Value)));
            }
            else
            {
                raw = string.Empty;
            }
        }

        raw = System.Text.RegularExpressions.Regex.Replace(raw, "<[^>]+>", " ");
        raw = System.Text.RegularExpressions.Regex.Replace(raw, "[ \\t]+", " ");
        raw = System.Text.RegularExpressions.Regex.Replace(raw, "\\s*\\n\\s*", "\n").Trim();

        if (string.IsNullOrWhiteSpace(raw)) return null;
        return raw.Length > 800 ? raw[..800] + "…" : raw;
    }

    public async Task<ContentDetailDto> UpdateAsync(int id, UpdateContentDto dto, CancellationToken ct = default)
    {
        var result = await _updateValidator.ValidateAsync(dto, ct);
        if (!result.IsValid)
            throw new ValidationAppException(BuildErrors(result));

        var entity = await _contentRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Content), id);

        await EnsureSectionExistsAsync(dto.SectionId!.Value, ct);

        dto.ApplyTo(entity);
        _contentRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(ct);

        return entity.ToDetailDto();
    }

    private async Task EnsureSectionExistsAsync(int sectionId, CancellationToken ct)
    {
        var section = await _sectionRepository.GetByIdAsync(sectionId, ct);
        if (section is null)
            throw new NotFoundException(nameof(Domain.Entities.Section), sectionId);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _contentRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Content), id);

        _contentRepository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static IReadOnlyDictionary<string, string[]> BuildErrors(FluentValidation.Results.ValidationResult result)
        => result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
