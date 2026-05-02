using VeterinaryBackend.Business.DTOs.Common;
using VeterinaryBackend.Business.DTOs.Media;
using VeterinaryBackend.Domain.Common;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.Business.Mappers;

public static class MediaFileMapper
{
    public static MediaFileDto ToDto(this MediaFile entity) => new()
    {
        Id = entity.Id,
        OriginalFileName = entity.OriginalFileName,
        Url = entity.Url,
        ContentType = entity.ContentType,
        Extension = entity.Extension,
        SizeBytes = entity.SizeBytes,
        CreatedAt = entity.CreatedAt
    };

    public static PagedResultDto<MediaFileDto> ToPagedDto(this PagedResult<MediaFile> source) => new()
    {
        Items = source.Items.Select(x => x.ToDto()).ToList(),
        TotalCount = source.TotalCount,
        Page = source.Page,
        PageSize = source.PageSize,
        TotalPages = source.TotalPages,
        HasPrevious = source.HasPrevious,
        HasNext = source.HasNext
    };
}
