using Orders.Domain.Entities;
using Orders.Domain.ValueObjects;

namespace Orders.Application.Interfaces.Repositories;
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email email);
    Task AddAsync(User user);
}