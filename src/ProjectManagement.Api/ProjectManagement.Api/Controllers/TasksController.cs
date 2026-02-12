using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ProjectManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    [HttpGet("status")]
    [AllowAnonymous] // Public endpoint
    public IActionResult GetApiStatus()
    {
        return Ok(new { status = "operational", timestamp = DateTime.UtcNow });
    }

    [HttpGet]
    [Authorize] // Requires authentication
    public IActionResult GetMyTasks()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new 
        { 
            message = "Your tasks",
            userId,
            userEmail,
            tasks = new[] 
            { 
                new { id = 1, title = "Task 1", status = "In Progress" },
                new { id = 2, title = "Task 2", status = "Completed" }
            }
        });
    }

    [HttpGet("{id}")]
    [Authorize]
    public IActionResult GetTaskById(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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

        return Ok(new 
        { 
            message = $"Task {id} assigned successfully",
            assignedBy = managerId
        });
    }

    [HttpGet("all")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult GetAllTasks()
    {
        return Ok(new 
        { 
            message = "All tasks in the system (Admin only)",
            totalTasks = 150,
            tasks = new[] 
            { 
                new { id = 1, title = "Task 1", assignedTo = "User 1" },
                new { id = 2, title = "Task 2", assignedTo = "User 2" }
            }
        });
    }
}
