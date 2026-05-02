using Microsoft.EntityFrameworkCore;
using VeterinaryBackend.DataAccess.Context;
using VeterinaryBackend.Domain.Common;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public class ContentRepository : Repository<Content>, IContentRepository
{
    public ContentRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<Content>> GetPagedWithSectionAsync(int page, int pageSize, int? sectionId, bool? onlyActive, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<Content> query = DbSet.AsNoTracking().Include(x => x.Section);
        if (sectionId.HasValue) query = query.Where(x => x.SectionId == sectionId.Value);
        if (onlyActive == true) query = query.Where(x => x.IsActive);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Content>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<Content?> GetByIdWithSectionAsync(int id, CancellationToken ct = default)
        => DbSet.Include(x => x.Section).FirstOrDefaultAsync(x => x.Id == id, ct);
}
