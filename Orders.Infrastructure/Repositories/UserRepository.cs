using Microsoft.EntityFrameworkCore;
using Orders.Application.Interfaces.Repositories;
using Orders.Domain.Entities;
using Orders.Domain.ValueObjects;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly OrdersDbContext _context;

    public UserRepository(OrdersDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(Email email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value);
    }

    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
}
