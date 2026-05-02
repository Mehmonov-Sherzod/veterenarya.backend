using VeterinaryBackend.Business.DTOs.Section;
using VeterinaryBackend.Domain.Common;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.Business.Mappers;

public static class SectionMapper
{
    public static SectionDto ToLocalizedDto(this Section entity, Language language)
    {
        return new SectionDto
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Title = PickTitle(entity, language),
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
            ContentCount = entity.Contents?.Count ?? 0,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static SectionDetailDto ToDetailDto(this Section entity)
    {
        return new SectionDetailDto
        {
            Id = entity.Id,
            Slug = entity.Slug,
            TitleUz = entity.TitleUz,
            TitleRu = entity.TitleRu,
            TitleEn = entity.TitleEn,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static Section ToEntity(this CreateSectionDto dto, string finalSlug)
    {
        return new Section
        {
            Slug = finalSlug,
            TitleUz = dto.TitleUz.Trim(),
            TitleRu = dto.TitleRu.Trim(),
            TitleEn = dto.TitleEn.Trim(),
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive
        };
    }

    public static void ApplyTo(this UpdateSectionDto dto, Section entity, string finalSlug)
    {
        entity.Slug = finalSlug;
        entity.TitleUz = dto.TitleUz.Trim();
        entity.TitleRu = dto.TitleRu.Trim();
        entity.TitleEn = dto.TitleEn.Trim();
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;
    }

    public static SectionWithContentsDto ToWithContentsDto(this Section section, Language language)
    {
        return new SectionWithContentsDto
        {
            Id = section.Id,
            Slug = section.Slug,
            Title = PickTitle(section, language),
            SortOrder = section.SortOrder,
            Items = section.Contents.Select(c => c.ToLocalizedDto(language)).ToList()
        };
    }

    // RU / EN translations are optional — fall back to the UZ source when missing.
    private static string PickTitle(Section section, Language language) => language switch
    {
        Language.Ru => string.IsNullOrWhiteSpace(section.TitleRu) ? section.TitleUz : section.TitleRu,
        Language.En => string.IsNullOrWhiteSpace(section.TitleEn) ? section.TitleUz : section.TitleEn,
        _ => section.TitleUz
    };
}
