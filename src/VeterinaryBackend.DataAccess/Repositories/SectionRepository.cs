using Microsoft.EntityFrameworkCore;
using VeterinaryBackend.DataAccess.Context;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public class SectionRepository : Repository<Section>, ISectionRepository
{
    public SectionRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Section>> GetAllWithContentsAsync(bool onlyActive, CancellationToken ct = default)
    {
        IQueryable<Section> query = DbSet.AsNoTracking()
            .Include(s => s.Contents)
            .Include(s => s.Children);

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

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
        => DbSet.AsNoTracking().AnyAsync(s => s.Id == id, ct);

    public Task<bool> HasChildrenAsync(int id, CancellationToken ct = default)
        => DbSet.AsNoTracking().AnyAsync(s => s.ParentId == id, ct);

    /// <summary>
    /// Returns every transitive descendant of the given section. Used to prevent assigning a
    /// section's ParentId to one of its own descendants (which would create a cycle).
    /// </summary>
    public async Task<IReadOnlyList<int>> GetDescendantIdsAsync(int id, CancellationToken ct = default)
    {
        var all = await DbSet.AsNoTracking()
            .Select(s => new { s.Id, s.ParentId })
            .ToListAsync(ct);

        var byParent = all
            .Where(s => s.ParentId.HasValue)
            .GroupBy(s => s.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var result = new List<int>();
        var stack = new Stack<int>();
        stack.Push(id);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!byParent.TryGetValue(current, out var children)) continue;
            foreach (var childId in children)
            {
                result.Add(childId);
                stack.Push(childId);
            }
        }
        return result;
    }
}
