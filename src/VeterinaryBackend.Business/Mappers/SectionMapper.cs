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
            ParentId = entity.ParentId,
            Slug = entity.Slug,
            Title = PickTitle(entity, language),
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
            ContentCount = entity.Contents?.Count ?? 0,
            ChildrenCount = entity.Children?.Count ?? 0,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static SectionDetailDto ToDetailDto(this Section entity)
    {
        return new SectionDetailDto
        {
            Id = entity.Id,
            ParentId = entity.ParentId,
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
            IsActive = dto.IsActive,
            ParentId = dto.ParentId
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
        entity.ParentId = dto.ParentId;
    }

    public static SectionWithContentsDto ToWithContentsDto(this Section section, Language language)
    {
        return new SectionWithContentsDto
        {
            Id = section.Id,
            ParentId = section.ParentId,
            Slug = section.Slug,
            Title = PickTitle(section, language),
            SortOrder = section.SortOrder,
            Items = section.Contents.Select(c => c.ToLocalizedDto(language)).ToList()
        };
    }

    /// <summary>
    /// Build a hierarchical tree from a flat list of section DTOs by linking each non-root
    /// node to its parent's <see cref="SectionDto.Children"/> collection.
    /// </summary>
    public static IReadOnlyList<SectionDto> BuildTree(IReadOnlyList<SectionDto> flat)
    {
        var byId = flat.ToDictionary(s => s.Id);
        var bucket = flat.ToDictionary(s => s.Id, _ => new List<SectionDto>());
        var roots = new List<SectionDto>();

        foreach (var node in flat)
        {
            if (node.ParentId is int pid && byId.ContainsKey(pid))
                bucket[pid].Add(node);
            else
                roots.Add(node);
        }

        foreach (var node in flat)
            node.Children = bucket[node.Id]
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Id)
                .ToList();

        return roots
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .ToList();
    }

    /// <summary>
    /// Build a hierarchical tree of sections-with-contents from a flat list.
    /// </summary>
    public static IReadOnlyList<SectionWithContentsDto> BuildTree(IReadOnlyList<SectionWithContentsDto> flat)
    {
        var byId = flat.ToDictionary(s => s.Id);
        var bucket = flat.ToDictionary(s => s.Id, _ => new List<SectionWithContentsDto>());
        var roots = new List<SectionWithContentsDto>();

        foreach (var node in flat)
        {
            if (node.ParentId is int pid && byId.ContainsKey(pid))
                bucket[pid].Add(node);
            else
                roots.Add(node);
        }

        foreach (var node in flat)
            node.Children = bucket[node.Id]
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Id)
                .ToList();

        return roots
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .ToList();
    }

    // RU / EN translations are optional — fall back to the UZ source when missing.
    private static string PickTitle(Section section, Language language) => language switch
    {
        Language.Ru => string.IsNullOrWhiteSpace(section.TitleRu) ? section.TitleUz : section.TitleRu,
        Language.En => string.IsNullOrWhiteSpace(section.TitleEn) ? section.TitleUz : section.TitleEn,
        _ => section.TitleUz
    };
}
