using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ProjectManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    [HttpGet("profile")]
    public IActionResult GetMyProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

        return Ok(new 
        { 
            userId, 
            email, 
            role, 
            isAuthenticated,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    [HttpPut("profile")]
    public IActionResult UpdateMyProfile([FromBody] object profileData)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new 
        { 
            message = "Profile updated successfully",
            userId
        });
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult GetAllUsers()
    {
        return Ok(new 
        { 
            message = "All users (Admin only)",
            users = new[] 
            { 
                new { id = 1, email = "user1@example.com", role = "User" },
                new { id = 2, email = "manager@example.com", role = "Manager" },
                new { id = 3, email = "admin@example.com", role = "Admin" }
            }
        });
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult GetUserById(Guid id)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new 
        { 
            message = $"User details for {id}",
            requestedBy = currentUserId
        });
    }

    [HttpPut("{id}/role")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult UpdateUserRole(Guid id, [FromBody] object roleData)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new 
        { 
            message = $"User {id} role updated",
            updatedBy = adminId
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult DeleteUser(Guid id)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new 
        { 
            message = $"User {id} deleted",
            deletedBy = adminId
        });
    }

    [HttpGet("managers")]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult GetManagers()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new 
        { 
            message = "List of managers",
            requestedByRole = role,
            managers = new[] 
            { 
                new { id = 1, email = "manager1@example.com" },
                new { id = 2, email = "manager2@example.com" }
            }
        });
    }
}
