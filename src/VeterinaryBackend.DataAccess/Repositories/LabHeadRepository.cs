using Microsoft.EntityFrameworkCore;
using VeterinaryBackend.DataAccess.Context;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public class LabHeadRepository : Repository<LabHead>, ILabHeadRepository
{
    public LabHeadRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<LabHead>> GetAllAsync(bool onlyActive, CancellationToken ct = default)
    {
        IQueryable<LabHead> query = DbSet.AsNoTracking().Include(x => x.Section);
        if (onlyActive) query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LabHead>> GetBySectionAsync(int? sectionId, bool onlyActive, CancellationToken ct = default)
    {
        IQueryable<LabHead> query = DbSet.AsNoTracking()
            .Include(x => x.Section)
            .Where(x => x.SectionId == sectionId);

        if (onlyActive) query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public Task<LabHead?> GetByIdWithSectionAsync(int id, CancellationToken ct = default)
        => DbSet.AsNoTracking().Include(x => x.Section).FirstOrDefaultAsync(x => x.Id == id, ct);
}
