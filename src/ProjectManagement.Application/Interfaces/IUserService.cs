using ProjectManagement.Application.Common;
using ProjectManagement.Application.DTOs;

namespace ProjectManagement.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserResponseDto>> GetAllUsersAsync(PaginationParams paginationParams);
    Task<UserResponseDto?> GetUserByIdAsync(Guid id);
    Task<bool> UpdateUserRoleAsync(Guid id, string role);
    Task<bool> DeleteUserAsync(Guid id);
    Task<List<UserResponseDto>> GetManagersAsync();
}
