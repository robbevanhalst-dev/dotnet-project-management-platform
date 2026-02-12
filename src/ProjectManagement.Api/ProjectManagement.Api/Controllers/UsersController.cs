using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Interfaces;
using System.Security.Claims;

namespace ProjectManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        _logger.LogInformation("User {UserId} is requesting their profile", userId);

        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("User profile not found: {UserId}", userId);
            return NotFound(new { message = "User not found" });
        }

        return Ok(user);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers([FromQuery] PaginationParams paginationParams)
    {
        var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        _logger.LogInformation("Admin {Email} is requesting all users", adminEmail);

        var pagedResult = await _userService.GetAllUsersAsync(paginationParams);

        return Ok(new
        {
            pagination = new
            {
                pagedResult.PageNumber,
                pagedResult.PageSize,
                pagedResult.TotalPages,
                pagedResult.TotalCount,
                pagedResult.HasPrevious,
                pagedResult.HasNext
            },
            data = pagedResult.Items
        });
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        _logger.LogInformation("User {CurrentUserId} is requesting user {UserId}", currentUserId, id);

        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", id);
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        return Ok(user);
    }

    [HttpPut("{id}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Admin {AdminId} is updating role for user {UserId} to {Role}", 
            adminId, id, dto.Role);

        var success = await _userService.UpdateUserRoleAsync(id, dto.Role);

        if (!success)
        {
            _logger.LogWarning("User not found for role update: {UserId}", id);
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        _logger.LogInformation("User role updated successfully: {UserId} to {Role}", id, dto.Role);

        return Ok(new { message = "User role updated successfully", userId = id, newRole = dto.Role });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Prevent admin from deleting themselves
        if (id == currentUserId)
        {
            _logger.LogWarning("Admin {AdminId} attempted to delete their own account", adminId);
            return BadRequest(new { message = "You cannot delete your own account" });
        }

        _logger.LogWarning("Admin {AdminId} is deleting user {UserId}", adminId, id);

        var success = await _userService.DeleteUserAsync(id);

        if (!success)
        {
            _logger.LogWarning("User not found for deletion: {UserId}", id);
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        _logger.LogInformation("User deleted successfully: {UserId}", id);

        return Ok(new { message = $"User {id} deleted successfully" });
    }

    [HttpGet("managers")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> GetManagers()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("User {UserId} is requesting list of managers", userId);

        var managers = await _userService.GetManagersAsync();

        return Ok(new
        {
            message = "Managers and Admins",
            count = managers.Count,
            data = managers
        });
    }
}
