using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public interface IBotSubscriberRepository : IRepository<BotSubscriber>
{
    Task<BotSubscriber?> GetByChatIdAsync(long chatId, CancellationToken ct = default);
    Task<IReadOnlyList<BotSubscriber>> GetActiveAsync(CancellationToken ct = default);
    Task<int> CountActiveAsync(CancellationToken ct = default);
}

public interface IBotMessageRepository : IRepository<BotMessage>
{
    Task<IReadOnlyList<BotMessage>> GetAllAsync(bool onlyUnread, int take, CancellationToken ct = default);
    Task<int> CountUnreadAsync(CancellationToken ct = default);
}
