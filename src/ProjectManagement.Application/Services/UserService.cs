using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Interfaces;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Application.Services;

public class UserService : IUserService
{
    private readonly ProjectManagementDbContext _context;

    public UserService(ProjectManagementDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<UserResponseDto>> GetAllUsersAsync(PaginationParams paginationParams)
    {
        var query = _context.Users.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(paginationParams.SearchTerm))
        {
            query = query.Where(u => u.Email.Contains(paginationParams.SearchTerm) ||
                                   u.Role.Contains(paginationParams.SearchTerm));
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(paginationParams.SortBy))
        {
            query = paginationParams.SortBy.ToLower() switch
            {
                "email" => paginationParams.SortDescending
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email),
                "role" => paginationParams.SortDescending
                    ? query.OrderByDescending(u => u.Role)
                    : query.OrderBy(u => u.Role),
                _ => query.OrderBy(u => u.Email)
            };
        }
        else
        {
            query = query.OrderBy(u => u.Email);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role
            })
            .ToListAsync();

        return new PagedResult<UserResponseDto>(
            items,
            totalCount,
            paginationParams.PageNumber,
            paginationParams.PageSize
        );
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _context.Users
            .Where(u => u.Id == id)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role
            })
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<bool> UpdateUserRoleAsync(Guid id, string role)
    {
        var user = await _context.Users.FindAsync(id);
        
        if (user == null)
            return false;

        user.Role = role;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        
        if (user == null)
            return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<UserResponseDto>> GetManagersAsync()
    {
        return await _context.Users
            .Where(u => u.Role == "Manager" || u.Role == "Admin")
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role
            })
            .ToListAsync();
    }
}
