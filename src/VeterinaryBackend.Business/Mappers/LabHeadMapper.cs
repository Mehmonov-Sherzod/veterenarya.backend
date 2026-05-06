using VeterinaryBackend.Business.DTOs.LabHead;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.Business.Mappers;

public static class LabHeadMapper
{
    public static LabHeadDto ToDto(this LabHead entity)
    {
        return new LabHeadDto
        {
            Id = entity.Id,
            FullName = entity.FullName,
            Phone = entity.Phone,
            ReceptionHours = entity.ReceptionHours,
            PhotoUrl = entity.PhotoUrl,
            Department = entity.Department,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static LabHead ToEntity(this CreateLabHeadDto dto)
    {
        return new LabHead
        {
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone.Trim(),
            ReceptionHours = dto.ReceptionHours.Trim(),
            PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim(),
            Department = string.IsNullOrWhiteSpace(dto.Department) ? null : dto.Department.Trim(),
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive
        };
    }

    public static void ApplyTo(this UpdateLabHeadDto dto, LabHead entity)
    {
        entity.FullName = dto.FullName.Trim();
        entity.Phone = dto.Phone.Trim();
        entity.ReceptionHours = dto.ReceptionHours.Trim();
        entity.PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim();
        entity.Department = string.IsNullOrWhiteSpace(dto.Department) ? null : dto.Department.Trim();
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;
    }
}
