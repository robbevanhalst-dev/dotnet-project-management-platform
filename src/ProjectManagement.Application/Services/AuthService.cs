using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Interfaces;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly ProjectManagementDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    private readonly IConfiguration _configuration;

    public AuthService(ProjectManagementDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<User>();
    }


    public async Task<bool> RegisterAsync(RegisterDto dto)
    {
        var exists = await _context.Users
            .AnyAsync(u => u.Email == dto.Email);

        if (exists) return false;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            Role = dto.Role // Set role from DTO
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return true;
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var keyString = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
        
        // Ensure the key is at least 32 characters (256 bits) for HS256
        if (keyString.Length < 32)
        {
            throw new InvalidOperationException($"JWT Key must be at least 32 characters long. Current length: {keyString.Length}");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    public async Task<string?> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null) return null;

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password
        );

        if (result == PasswordVerificationResult.Failed)
            return null;

        return GenerateJwtToken(user);
    }

    public async Task<AuthenticateResponse?> AuthenticateAsync(LoginDto dto, string ipAddress)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null) return null;

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password
        );

        if (result == PasswordVerificationResult.Failed)
            return null;

        // Generate JWT and refresh token
        var jwtToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken(ipAddress);

        // Save refresh token
        user.RefreshTokens.Add(refreshToken);

        // Remove old refresh tokens (keep only last 5)
        RemoveOldRefreshTokens(user);

        await _context.SaveChangesAsync();

        return new AuthenticateResponse
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken.Token,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<AuthenticateResponse?> RefreshTokenAsync(string token, string ipAddress)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

        if (user == null) return null;

        var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

        if (!refreshToken.IsActive)
            return null;

        // Replace old refresh token with new one
        var newRefreshToken = RotateRefreshToken(refreshToken, ipAddress);
        user.RefreshTokens.Add(newRefreshToken);

        // Remove old refresh tokens
        RemoveOldRefreshTokens(user);

        await _context.SaveChangesAsync();

        // Generate new JWT
        var jwtToken = GenerateJwtToken(user);

        return new AuthenticateResponse
        {
            AccessToken = jwtToken,
            RefreshToken = newRefreshToken.Token,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<bool> RevokeTokenAsync(string token, string ipAddress)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

        if (user == null) return false;

        var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

        if (!refreshToken.IsActive) return false;

        // Revoke token
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<RefreshToken>> GetRefreshTokensAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.RefreshTokens.ToList() ?? new List<RefreshToken>();
    }

    private RefreshToken GenerateRefreshToken(string ipAddress)
    {
        using var rngCryptoServiceProvider = RandomNumberGenerator.Create();
        var randomBytes = new byte[64];
        rngCryptoServiceProvider.GetBytes(randomBytes);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days expiration
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }

    private RefreshToken RotateRefreshToken(RefreshToken refreshToken, string ipAddress)
    {
        var newRefreshToken = GenerateRefreshToken(ipAddress);
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReplacedByToken = newRefreshToken.Token;
        return newRefreshToken;
    }

    private void RemoveOldRefreshTokens(User user)
    {
        // Keep only the 5 most recent refresh tokens
        var tokensToRemove = user.RefreshTokens
            .OrderByDescending(x => x.CreatedAt)
            .Skip(5)
            .ToList();

        foreach (var token in tokensToRemove)
        {
            user.RefreshTokens.Remove(token);
        }
    }

}



