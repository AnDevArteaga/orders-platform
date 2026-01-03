using Microsoft.AspNetCore.Mvc;
using Orders.Application.UsesCases.Auth;
using Orders.Api.DTOs;
namespace Orders.Api.Controllers;



[ApiController]
    [Route("api/auth")]
public class AuthController(LoginUserUseCase loginUser, RegisterUserUseCase registerUser ): ControllerBase
    {
    private readonly LoginUserUseCase _loginUser = loginUser;
    private readonly RegisterUserUseCase _registerUser = registerUser;

    [HttpPost("/login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var token = await _loginUser.ExecuteAsync(request.Email, request.Password);
        return Ok(new { token });
    }

}

