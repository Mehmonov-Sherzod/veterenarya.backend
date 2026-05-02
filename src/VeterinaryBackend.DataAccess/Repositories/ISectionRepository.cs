using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public interface ISectionRepository : IRepository<Section>
{
    Task<IReadOnlyList<Section>> GetAllWithContentsAsync(bool onlyActive, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken ct = default);
}
