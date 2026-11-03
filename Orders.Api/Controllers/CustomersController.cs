using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Orders.Api.Auth;
using Orders.Application.Services;
using Orders.Domain.Entities;

namespace Orders.Api.Controllers;

[ApiController]
[Authorize(Policy = "Integration")]
[EnableRateLimiting("integration")]
[Route("v1/customers")]
public class CustomersController(CustomerService customers) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Save(CustomerRequest request, CancellationToken cancellationToken)
    {
        var (customer, created) = await customers.SaveAsync(User.MerchantId(), request.Name, request.Email, cancellationToken);
        var body = ToResponse(customer);
        return created ? Created($"/v1/customers/{customer.Id}", body) : Ok(body);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var customer = await customers.GetAsync(User.MerchantId(), id, cancellationToken);
        return Ok(ToResponse(customer));
    }

    private static object ToResponse(Customer customer) => new
    {
        id = customer.Id,
        name = customer.Name,
        email = customer.Email,
        created_at = customer.CreatedAt
    };
}

public sealed record CustomerRequest(string Name, string Email);
