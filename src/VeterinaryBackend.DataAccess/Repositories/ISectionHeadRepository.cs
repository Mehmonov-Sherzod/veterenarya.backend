using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public interface ISectionHeadRepository : IRepository<SectionHead>
{
    Task<IReadOnlyList<SectionHead>> GetAllAsync(bool onlyActive, CancellationToken ct = default);
    Task<SectionHead?> GetBySectionAsync(int sectionId, bool onlyActive, CancellationToken ct = default);
    Task<SectionHead?> GetByIdWithSectionAsync(int id, CancellationToken ct = default);
}
