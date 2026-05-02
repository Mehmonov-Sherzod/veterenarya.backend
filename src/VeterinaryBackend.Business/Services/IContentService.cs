using VeterinaryBackend.Business.DTOs.Common;
using VeterinaryBackend.Business.DTOs.Content;
using VeterinaryBackend.Domain.Common;

namespace VeterinaryBackend.Business.Services;

public interface IContentService
{
    Task<PagedResultDto<ContentDto>> GetAllAsync(PaginationQuery query, Language language, bool onlyActive, int? sectionId, CancellationToken ct = default);
    Task<ContentDto> GetByIdAsync(int id, Language language, CancellationToken ct = default);
    Task<ContentDetailDto> GetDetailByIdAsync(int id, CancellationToken ct = default);
    Task<ContentDetailDto> CreateAsync(CreateContentDto dto, CancellationToken ct = default);
    Task<ContentDetailDto> UpdateAsync(int id, UpdateContentDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
