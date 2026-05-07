using VeterinaryBackend.Business.DTOs.SectionHead;
using VeterinaryBackend.Domain.Common;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.Business.Mappers;

public static class SectionHeadMapper
{
    public static SectionHeadDto ToDto(this SectionHead entity, Language language = Language.Uz)
    {
        return new SectionHeadDto
        {
            Id = entity.Id,
            FullName = entity.FullName,
            Phone = entity.Phone,
            Email = entity.Email,
            WorkingHours = entity.WorkingHours,
            PhotoUrl = entity.PhotoUrl,
            SectionId = entity.SectionId,
            SectionTitle = entity.Section is null ? null : PickSectionTitle(entity.Section, language),
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static SectionHead ToEntity(this CreateSectionHeadDto dto)
    {
        return new SectionHead
        {
            FullName = dto.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            WorkingHours = string.IsNullOrWhiteSpace(dto.WorkingHours) ? null : dto.WorkingHours.Trim(),
            PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim(),
            SectionId = dto.SectionId,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive
        };
    }

    public static void ApplyTo(this UpdateSectionHeadDto dto, SectionHead entity)
    {
        entity.FullName = dto.FullName.Trim();
        entity.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        entity.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        entity.WorkingHours = string.IsNullOrWhiteSpace(dto.WorkingHours) ? null : dto.WorkingHours.Trim();
        entity.PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim();
        entity.SectionId = dto.SectionId;
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;
    }

    private static string PickSectionTitle(Section section, Language language) => language switch
    {
        Language.Ru => string.IsNullOrWhiteSpace(section.TitleRu) ? section.TitleUz : section.TitleRu,
        Language.En => string.IsNullOrWhiteSpace(section.TitleEn) ? section.TitleUz : section.TitleEn,
        _ => section.TitleUz
    };
}
