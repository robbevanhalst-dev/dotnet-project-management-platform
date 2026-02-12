using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Entities;

namespace ProjectManagement.Application.Interfaces;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterDto dto);
    Task<string?> LoginAsync(LoginDto dto);
    Task<AuthenticateResponse?> AuthenticateAsync(LoginDto dto, string ipAddress);
    Task<AuthenticateResponse?> RefreshTokenAsync(string token, string ipAddress);
    Task<bool> RevokeTokenAsync(string token, string ipAddress);
    Task<List<RefreshToken>> GetRefreshTokensAsync(Guid userId);
}

