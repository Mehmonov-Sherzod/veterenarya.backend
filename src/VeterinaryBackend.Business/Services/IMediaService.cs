using VeterinaryBackend.Business.DTOs.Common;
using VeterinaryBackend.Business.DTOs.Media;

namespace VeterinaryBackend.Business.Services;

public interface IMediaService
{
    Task<MediaFileDto> UploadAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken ct = default);

    Task<PagedResultDto<MediaFileDto>> GetAllAsync(PaginationQuery query, CancellationToken ct = default);
    Task<MediaFileDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
