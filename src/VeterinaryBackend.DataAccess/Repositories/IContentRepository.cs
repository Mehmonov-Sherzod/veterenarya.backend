using VeterinaryBackend.Domain.Common;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public interface IContentRepository : IRepository<Content>
{
    Task<PagedResult<Content>> GetPagedWithSectionAsync(int page, int pageSize, int? sectionId, bool? onlyActive, CancellationToken ct = default);
    Task<Content?> GetByIdWithSectionAsync(int id, CancellationToken ct = default);
}
