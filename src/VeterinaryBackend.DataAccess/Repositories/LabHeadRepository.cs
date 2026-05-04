using Microsoft.EntityFrameworkCore;
using VeterinaryBackend.DataAccess.Context;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public class LabHeadRepository : Repository<LabHead>, ILabHeadRepository
{
    public LabHeadRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<LabHead>> GetAllAsync(bool onlyActive, CancellationToken ct = default)
    {
        IQueryable<LabHead> query = DbSet.AsNoTracking();
        if (onlyActive) query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }
}
