using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ProjectManagement.Application.Common;

namespace ProjectManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class ProjectsController : ControllerBase
{
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(ILogger<ProjectsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAllProjects([FromQuery] PaginationParams paginationParams)
    {
        _logger.LogInformation("Getting all projects with pagination: Page {Page}, Size {Size}", 
            paginationParams.PageNumber, paginationParams.PageSize);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        // Sample data
        var allProjects = Enumerable.Range(1, 50)
            .Select(i => new { Id = i, Name = $"Project {i}", Status = i % 2 == 0 ? "Active" : "Completed" })
            .AsEnumerable();

        // Apply search if provided
        if (!string.IsNullOrWhiteSpace(paginationParams.SearchTerm))
        {
            allProjects = allProjects.Where(p => 
                p.Name.Contains(paginationParams.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        // Apply pagination
        var pagedResult = allProjects.ToPagedResult(
            paginationParams.PageNumber, 
            paginationParams.PageSize);

        _logger.LogInformation("Returning {Count} projects out of {Total}", 
            pagedResult.Items.Count, pagedResult.TotalCount);

        return Ok(new
        {
            userId,
            userEmail,
            userRole,
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
    public IActionResult GetProjectById(Guid id)
    {
        _logger.LogInformation("Getting project by ID: {ProjectId}", id);

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

        _logger.LogInformation("User {UserId} with role {Role} is creating a new project", userId, userRole);

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

        _logger.LogInformation("User {UserId} is updating project {ProjectId}", userId, id);

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

        _logger.LogWarning("User {UserId} is deleting project {ProjectId}", userId, id);

        return Ok(new 
        { 
            message = $"Project {id} deleted successfully",
            deletedBy = userId
        });
    }

    [HttpGet("public")]
    [AllowAnonymous] // Override controller-level [Authorize]
    public IActionResult GetPublicProjects([FromQuery] PaginationParams paginationParams)
    {
        _logger.LogInformation("Getting public projects");

        var publicProjects = Enumerable.Range(1, 20)
            .Select(i => new { Id = i, Name = $"Public Project {i}" })
            .AsEnumerable();

        var pagedResult = publicProjects.ToPagedResult(
            paginationParams.PageNumber,
            paginationParams.PageSize);

        return Ok(new 
        { 
            message = "Public projects - no authentication required",
            pagination = new
            {
                pagedResult.PageNumber,
                pagedResult.PageSize,
                pagedResult.TotalPages,
                pagedResult.TotalCount
            },
            data = pagedResult.Items
        });
    }

    [HttpGet("admin-only")]
    [Authorize(Policy = "AdminOnly")] // Using policy
    public IActionResult GetAdminData()
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        _logger.LogInformation("Admin {Email} is accessing admin-only data", userEmail);

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

        _logger.LogInformation("User with role {Role} is accessing manager dashboard", userRole);

        return Ok(new 
        { 
            message = "Manager dashboard",
            role = userRole,
            dashboardData = new { totalProjects = 10, activeProjects = 7 }
        });
    }

    [HttpGet("test-error")]
    [AllowAnonymous]
    public IActionResult TestError()
    {
        _logger.LogWarning("Test error endpoint called - throwing exception");
        throw new InvalidOperationException("This is a test exception to demonstrate global error handling");
    }
}
