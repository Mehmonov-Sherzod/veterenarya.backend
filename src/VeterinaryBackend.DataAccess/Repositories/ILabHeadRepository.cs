using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public interface ILabHeadRepository : IRepository<LabHead>
{
    Task<IReadOnlyList<LabHead>> GetAllAsync(bool onlyActive, CancellationToken ct = default);
}
