using Microsoft.EntityFrameworkCore;
using VeterinaryBackend.DataAccess.Context;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public class SectionHeadRepository : Repository<SectionHead>, ISectionHeadRepository
{
    public SectionHeadRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<SectionHead>> GetAllAsync(bool onlyActive, CancellationToken ct = default)
    {
        IQueryable<SectionHead> query = DbSet.AsNoTracking().Include(x => x.Section);
        if (onlyActive) query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<SectionHead?> GetBySectionAsync(int sectionId, bool onlyActive, CancellationToken ct = default)
    {
        IQueryable<SectionHead> query = DbSet.AsNoTracking()
            .Include(x => x.Section)
            .Where(x => x.SectionId == sectionId);

        if (onlyActive) query = query.Where(x => x.IsActive);

        return await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).FirstOrDefaultAsync(ct);
    }

    public Task<SectionHead?> GetByIdWithSectionAsync(int id, CancellationToken ct = default)
        => DbSet.AsNoTracking().Include(x => x.Section).FirstOrDefaultAsync(x => x.Id == id, ct);
}
