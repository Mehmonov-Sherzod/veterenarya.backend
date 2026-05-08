using Microsoft.EntityFrameworkCore;
using VeterinaryBackend.DataAccess.Context;
using VeterinaryBackend.Domain.Entities;

namespace VeterinaryBackend.DataAccess.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default)
        => DbSet.AnyAsync(ct);
}
