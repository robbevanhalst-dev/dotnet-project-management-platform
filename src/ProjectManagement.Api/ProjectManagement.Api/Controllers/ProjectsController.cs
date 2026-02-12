using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ProjectManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class ProjectsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAllProjects()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new 
        { 
            message = "List of all projects accessible to user",
            userId,
            userEmail,
            userRole,
            projects = new[] { "Project 1", "Project 2", "Project 3" }
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetProjectById(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        return Ok(new 
        { 
            message = $"Project details for {id}",
            userId,
            projectId = id
        });
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")] // Only Manager or Admin can create
    public IActionResult CreateProject([FromBody] object projectData)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new 
        { 
            message = "Project created successfully",
            createdBy = userId,
            role = userRole
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")] // Only Manager or Admin can update
    public IActionResult UpdateProject(Guid id, [FromBody] object projectData)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new 
        { 
            message = $"Project {id} updated successfully",
            updatedBy = userId
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")] // Only Admin can delete
    public IActionResult DeleteProject(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new 
        { 
            message = $"Project {id} deleted successfully",
            deletedBy = userId
        });
    }

    [HttpGet("public")]
    [AllowAnonymous] // Override controller-level [Authorize]
    public IActionResult GetPublicProjects()
    {
        return Ok(new 
        { 
            message = "Public projects - no authentication required",
            projects = new[] { "Public Project 1", "Public Project 2" }
        });
    }

    [HttpGet("admin-only")]
    [Authorize(Policy = "AdminOnly")] // Using policy
    public IActionResult GetAdminData()
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new 
        { 
            message = "Admin-only data",
            adminEmail = userEmail
        });
    }

    [HttpGet("manager-dashboard")]
    [Authorize(Policy = "ManagerOrAdmin")] // Using policy
    public IActionResult GetManagerDashboard()
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new 
        { 
            message = "Manager dashboard",
            role = userRole,
            dashboardData = new { totalProjects = 10, activeProjects = 7 }
        });
    }
}
