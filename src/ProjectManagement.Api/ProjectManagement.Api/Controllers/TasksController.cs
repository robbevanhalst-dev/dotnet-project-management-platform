using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ProjectManagement.Application.Common;

namespace ProjectManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ILogger<TasksController> _logger;

    public TasksController(ILogger<TasksController> logger)
    {
        _logger = logger;
    }

    [HttpGet("status")]
    [AllowAnonymous] // Public endpoint
    public IActionResult GetApiStatus()
    {
        _logger.LogInformation("API status check requested");
        return Ok(new { status = "operational", timestamp = DateTime.UtcNow });
    }

    [HttpGet]
    [Authorize] // Requires authentication
    public IActionResult GetMyTasks([FromQuery] PaginationParams paginationParams)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        _logger.LogInformation("User {UserId} is fetching their tasks", userId);

        // Sample data
        var allTasks = Enumerable.Range(1, 30)
            .Select(i => new
            {
                id = i,
                title = $"Task {i}",
                status = i % 3 == 0 ? "Completed" : i % 2 == 0 ? "In Progress" : "Pending"
            })
            .AsEnumerable();

        var pagedResult = allTasks.ToPagedResult(
            paginationParams.PageNumber,
            paginationParams.PageSize);

        return Ok(new 
        { 
            userId,
            userEmail,
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
    [Authorize]
    public IActionResult GetTaskById(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("User {UserId} is fetching task {TaskId}", userId, id);

        return Ok(new 
        { 
            message = $"Task details for {id}",
            taskId = id,
            assignedTo = userId
        });
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult CreateTask([FromBody] object taskData)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        _logger.LogInformation("User {UserId} with role {Role} is creating a new task", userId, userRole);

        return Ok(new 
        { 
            message = "Task created successfully",
            createdBy = userId,
            role = userRole
        });
    }

    [HttpPut("{id}")]
    [Authorize]
    public IActionResult UpdateTask(Guid id, [FromBody] object taskData)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("User {UserId} is updating task {TaskId}", userId, id);

        return Ok(new 
        { 
            message = $"Task {id} updated successfully",
            updatedBy = userId
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    public IActionResult DeleteTask(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogWarning("User {UserId} is deleting task {TaskId}", userId, id);

        return Ok(new 
        { 
            message = $"Task {id} deleted successfully",
            deletedBy = userId
        });
    }

    [HttpPost("{id}/assign")]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult AssignTask(Guid id, [FromBody] object assignmentData)
    {
        var managerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("Manager {ManagerId} is assigning task {TaskId}", managerId, id);

        return Ok(new 
        { 
            message = $"Task {id} assigned successfully",
            assignedBy = managerId
        });
    }

    [HttpGet("all")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult GetAllTasks([FromQuery] PaginationParams paginationParams)
    {
        _logger.LogInformation("Admin is fetching all tasks with pagination");

        var allTasks = Enumerable.Range(1, 150)
            .Select(i => new
            {
                id = i,
                title = $"Task {i}",
                assignedTo = $"User {(i % 10) + 1}"
            })
            .AsEnumerable();

        var pagedResult = allTasks.ToPagedResult(
            paginationParams.PageNumber,
            paginationParams.PageSize);

        return Ok(new 
        { 
            message = "All tasks in the system (Admin only)",
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
}
