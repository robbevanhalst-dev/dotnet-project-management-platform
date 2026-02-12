using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Interfaces;

namespace ProjectManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        if (!result)
            return BadRequest("User already exists");

        return Ok("User registered");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var token = await _authService.LoginAsync(dto);

        if (token == null)
            return Unauthorized();

        return Ok(new { token });
    }

}

