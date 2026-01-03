using Orders.Domain.Entities;
namespace Orders.Application.Interfaces.Services;
    public interface IJwtService
    {
    string GenerateToken(User user);
}

