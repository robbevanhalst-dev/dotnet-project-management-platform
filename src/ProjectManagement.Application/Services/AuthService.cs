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

}



