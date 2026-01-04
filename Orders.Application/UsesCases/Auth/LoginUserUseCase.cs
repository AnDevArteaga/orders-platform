using Orders.Domain.ValueObjects;
using Orders.Application.Interfaces.Repositories;
using Orders.Application.Interfaces.Services;
using BCrypt.Net;

namespace Orders.Application.UsesCases.Auth;
    public class LoginUserUseCase(IUserRepository userRepository, IJwtService jwtService)
    {
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IJwtService _jwtService = jwtService;

    public async Task<string> ExecuteAsync(string emailRaw, string password)
    {
        var email = new Email(emailRaw);
        var user = await _userRepository.GetByEmailAsync(email)
            ?? throw new InvalidOperationException("Invalid email or password.");
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new InvalidOperationException("Invalid email or password.");

        return _jwtService.GenerateToken(user);
    }
}

 