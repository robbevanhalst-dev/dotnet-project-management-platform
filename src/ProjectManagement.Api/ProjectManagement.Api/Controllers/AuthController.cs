using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Interfaces;
using System.Security.Claims;

namespace ProjectManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", dto.Email);

        var result = await _authService.RegisterAsync(dto);

        if (!result)
        {
            _logger.LogWarning("Registration failed - user already exists: {Email}", dto.Email);
            return BadRequest("User already exists");
        }

        _logger.LogInformation("User registered successfully: {Email}", dto.Email);
        return Ok("User registered");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        _logger.LogInformation("Login attempt for email: {Email}", dto.Email);

        var token = await _authService.LoginAsync(dto);

        if (token == null)
        {
            _logger.LogWarning("Login failed for email: {Email}", dto.Email);
            return Unauthorized();
        }

        _logger.LogInformation("User logged in successfully: {Email}", dto.Email);
        return Ok(new { token });
    }

    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate(LoginDto dto)
    {
        _logger.LogInformation("Authentication attempt for email: {Email}", dto.Email);

        var ipAddress = GetIpAddress();
        var response = await _authService.AuthenticateAsync(dto, ipAddress);

        if (response == null)
        {
            _logger.LogWarning("Authentication failed for email: {Email}", dto.Email);
            return Unauthorized(new { message = "Email or password is incorrect" });
        }

        // Set refresh token in HTTP-only cookie
        SetTokenCookie(response.RefreshToken);

        _logger.LogInformation("User authenticated successfully: {Email}, Role: {Role}", 
            response.Email, response.Role);

        return Ok(response);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request = null)
    {
        // Try to get token from request body, fallback to cookie
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Refresh token not provided");
            return BadRequest(new { message = "Refresh token is required" });
        }

        _logger.LogInformation("Refresh token attempt");

        var ipAddress = GetIpAddress();
        var response = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

        if (response == null)
        {
            _logger.LogWarning("Invalid or expired refresh token");
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        // Set new refresh token in cookie
        SetTokenCookie(response.RefreshToken);

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", response.UserId);

        return Ok(response);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest? request = null)
    {
        // Try to get token from request body, fallback to cookie
        var refreshToken = request?.RefreshToken ?? Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Revoke token - no token provided");
            return BadRequest(new { message = "Refresh token is required" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("Revoke token attempt by user: {UserId}", userId);

        var ipAddress = GetIpAddress();
        var success = await _authService.RevokeTokenAsync(refreshToken, ipAddress);

        if (!success)
        {
            _logger.LogWarning("Failed to revoke token");
            return BadRequest(new { message = "Invalid refresh token" });
        }

        // Remove cookie
        Response.Cookies.Delete("refreshToken");

        _logger.LogInformation("Token revoked successfully");

        return Ok(new { message = "Token revoked" });
    }

    [HttpGet("refresh-tokens")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRefreshTokens(Guid userId)
    {
        _logger.LogInformation("Admin fetching refresh tokens for user: {UserId}", userId);

        var tokens = await _authService.GetRefreshTokensAsync(userId);

        return Ok(tokens);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var ipAddress = GetIpAddress();
            await _authService.RevokeTokenAsync(refreshToken, ipAddress);
        }

        Response.Cookies.Delete("refreshToken");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User logged out: {UserId}", userId);

        return Ok(new { message = "Logged out successfully" });
    }

    // Helper methods
    private void SetTokenCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(7),
            Secure = true, // HTTPS only
            SameSite = SameSiteMode.Strict,
            IsEssential = true
        };

        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }

    private string GetIpAddress()
    {
        // Try to get IP from X-Forwarded-For header (for proxies/load balancers)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
        }

        // Fallback to RemoteIpAddress
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

