using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public interface ILabHeadRepository : IRepository<LabHead>
{
    Task<IReadOnlyList<LabHead>> GetAllAsync(bool onlyActive, CancellationToken ct = default);

    /// <summary>Returns heads filtered by an optional section. Pass null to get only the unassigned heads.</summary>
    Task<IReadOnlyList<LabHead>> GetBySectionAsync(int? sectionId, bool onlyActive, CancellationToken ct = default);

    Task<LabHead?> GetByIdWithSectionAsync(int id, CancellationToken ct = default);
}
