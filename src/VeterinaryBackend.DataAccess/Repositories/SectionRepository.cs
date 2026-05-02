using Microsoft.EntityFrameworkCore;
using VeterinaryBackend.DataAccess.Context;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public class SectionRepository : Repository<Section>, ISectionRepository
{
    public SectionRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Section>> GetAllWithContentsAsync(bool onlyActive, CancellationToken ct = default)
    {
        IQueryable<Section> query = DbSet.AsNoTracking().Include(s => s.Contents);
        if (onlyActive) query = query.Where(s => s.IsActive);

        var list = await query
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);

        if (onlyActive)
        {
            foreach (var section in list)
                section.Contents = section.Contents.Where(c => c.IsActive)
                    .OrderBy(c => c.SortOrder)
                    .ThenByDescending(c => c.CreatedAt)
                    .ToList();
        }
        else
        {
            foreach (var section in list)
                section.Contents = section.Contents
                    .OrderBy(c => c.SortOrder)
                    .ThenByDescending(c => c.CreatedAt)
                    .ToList();
        }

        return list;
    }

    public Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken ct = default)
    {
        var q = DbSet.AsNoTracking().Where(s => s.Slug == slug);
        if (excludeId.HasValue) q = q.Where(s => s.Id != excludeId.Value);
        return q.AnyAsync(ct);
    }
}
