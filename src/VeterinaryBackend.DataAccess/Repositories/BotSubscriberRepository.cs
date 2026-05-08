using Microsoft.EntityFrameworkCore;
using VeterinaryBackend.DataAccess.Context;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public class BotSubscriberRepository : Repository<BotSubscriber>, IBotSubscriberRepository
{
    public BotSubscriberRepository(AppDbContext context) : base(context) { }

    public Task<BotSubscriber?> GetByChatIdAsync(long chatId, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(x => x.ChatId == chatId, ct);

    public async Task<IReadOnlyList<BotSubscriber>> GetActiveAsync(CancellationToken ct = default)
        => await DbSet.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);

    public Task<int> CountActiveAsync(CancellationToken ct = default)
        => DbSet.CountAsync(x => x.IsActive, ct);
}

public class BotMessageRepository : Repository<BotMessage>, IBotMessageRepository
{
    public BotMessageRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<BotMessage>> GetAllAsync(bool onlyUnread, int take, CancellationToken ct = default)
    {
        IQueryable<BotMessage> query = DbSet.AsNoTracking();
        if (onlyUnread) query = query.Where(x => !x.IsRead);
        return await query.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync(ct);
    }

    public Task<int> CountUnreadAsync(CancellationToken ct = default)
        => DbSet.CountAsync(x => !x.IsRead, ct);
}

public class BotChannelRepository : Repository<BotChannel>, IBotChannelRepository
{
    public BotChannelRepository(AppDbContext context) : base(context) { }

    public Task<BotChannel?> GetByChatIdAsync(long chatId, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(x => x.ChatId == chatId, ct);

    public async Task<IReadOnlyList<BotChannel>> GetActiveAsync(CancellationToken ct = default)
        => await DbSet.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
}
