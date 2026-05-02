using VeterinaryBackend.Domain.Common;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public interface IMediaFileRepository : IRepository<MediaFile>
{
    Task<PagedResult<MediaFile>> GetPagedNewestAsync(int page, int pageSize, CancellationToken ct = default);
}
