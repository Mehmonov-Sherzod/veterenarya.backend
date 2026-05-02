using VeterinaryBackend.Business.DTOs.Common;
using VeterinaryBackend.Business.DTOs.Content;
using VeterinaryBackend.Domain.Common;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.Business.Mappers;

public static class ContentMapper
{
    public static ContentDto ToLocalizedDto(this Content entity, Language language)
    {
        return new ContentDto
        {
            Id = entity.Id,
            SectionId = entity.SectionId,
            SectionTitle = entity.Section is null ? null : PickSectionTitle(entity.Section, language),
            Title = PickTitle(entity, language),
            Description = PickDescription(entity, language),
            ImageUrl = entity.ImageUrl,
            ImagePosition = entity.ImagePosition,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static ContentDetailDto ToDetailDto(this Content entity)
    {
        return new ContentDetailDto
        {
            Id = entity.Id,
            SectionId = entity.SectionId,
            TitleUz = entity.TitleUz,
            TitleRu = entity.TitleRu,
            TitleEn = entity.TitleEn,
            DescriptionUz = entity.DescriptionUz,
            DescriptionRu = entity.DescriptionRu,
            DescriptionEn = entity.DescriptionEn,
            ImageUrl = entity.ImageUrl,
            ImagePosition = entity.ImagePosition,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static Content ToEntity(this CreateContentDto dto)
    {
        return new Content
        {
            SectionId = dto.SectionId,
            TitleUz = dto.TitleUz.Trim(),
            TitleRu = dto.TitleRu.Trim(),
            TitleEn = dto.TitleEn.Trim(),
            DescriptionUz = dto.DescriptionUz.Trim(),
            DescriptionRu = dto.DescriptionRu.Trim(),
            DescriptionEn = dto.DescriptionEn.Trim(),
            ImageUrl = dto.ImageUrl.Trim(),
            ImagePosition = NormalizePosition(dto.ImagePosition),
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive
        };
    }

    public static void ApplyTo(this UpdateContentDto dto, Content entity)
    {
        entity.SectionId = dto.SectionId;
        entity.TitleUz = dto.TitleUz.Trim();
        entity.TitleRu = dto.TitleRu.Trim();
        entity.TitleEn = dto.TitleEn.Trim();
        entity.DescriptionUz = dto.DescriptionUz.Trim();
        entity.DescriptionRu = dto.DescriptionRu.Trim();
        entity.DescriptionEn = dto.DescriptionEn.Trim();
        entity.ImageUrl = dto.ImageUrl.Trim();
        entity.ImagePosition = NormalizePosition(dto.ImagePosition);
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;
    }

    private static string NormalizePosition(string? value)
    {
        var v = (value ?? "").Trim().ToLowerInvariant();
        return v == "bottom" ? "bottom" : "top";
    }

    public static PagedResultDto<ContentDto> ToLocalizedPagedDto(this PagedResult<Content> source, Language language)
    {
        return new PagedResultDto<ContentDto>
        {
            Items = source.Items.Select(x => x.ToLocalizedDto(language)).ToList(),
            TotalCount = source.TotalCount,
            Page = source.Page,
            PageSize = source.PageSize,
            TotalPages = source.TotalPages,
            HasPrevious = source.HasPrevious,
            HasNext = source.HasNext
        };
    }

    // RU / EN translations are optional — fall back to the UZ source when missing.
    private static string PickTitle(Content entity, Language language) => language switch
    {
        Language.Ru => Coalesce(entity.TitleRu, entity.TitleUz),
        Language.En => Coalesce(entity.TitleEn, entity.TitleUz),
        _ => entity.TitleUz
    };

    private static string PickDescription(Content entity, Language language) => language switch
    {
        Language.Ru => Coalesce(entity.DescriptionRu, entity.DescriptionUz),
        Language.En => Coalesce(entity.DescriptionEn, entity.DescriptionUz),
        _ => entity.DescriptionUz
    };

    private static string PickSectionTitle(Section section, Language language) => language switch
    {
        Language.Ru => Coalesce(section.TitleRu, section.TitleUz),
        Language.En => Coalesce(section.TitleEn, section.TitleUz),
        _ => section.TitleUz
    };

    private static string Coalesce(string preferred, string fallback)
        => string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
}
