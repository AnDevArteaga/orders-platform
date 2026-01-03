using Orders.Domain.Entities;
using Orders.Domain.ValueObjects;
using Orders.Application.Interfaces.Repositories;
using BCrypt.Net;

namespace Orders.Application.UsesCases.Auth;

public class RegisterUserUseCase(IUserRepository userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<Guid> ExecuteAsync(string username, string emailRaw, string password)
    {
        var email = new Email(emailRaw);
        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser != null)
        {
            throw new Exception("email already exists");
        }
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User(
            username: username,
            email: email,
            passwordHash: hashedPassword
        );
        await _userRepository.AddAsync(user);
        return user.Id;
    }

}

