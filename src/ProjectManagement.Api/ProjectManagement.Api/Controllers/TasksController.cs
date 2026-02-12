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
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTasks([FromQuery] PaginationParams paginationParams)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        _logger.LogInformation("User {UserId} ({Email}) is requesting their tasks", userId, userEmail);

        var pagedResult = await _taskService.GetTasksAsync(paginationParams, userId);

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

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllTasks([FromQuery] PaginationParams paginationParams)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

        _logger.LogInformation("Admin {Email} is requesting all tasks", userEmail);

        var pagedResult = await _taskService.GetAllTasksAsync(paginationParams);

        return Ok(new
        {
            message = "All tasks (Admin view)",
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
    public async Task<IActionResult> GetTaskById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

        _logger.LogInformation("User {UserId} is requesting task {TaskId}", userId, id);

        // Check if user can access this task
        var canAccess = await _taskService.CanUserAccessTaskAsync(id, userId, userRole);
        if (!canAccess)
        {
            _logger.LogWarning("User {UserId} attempted to access task {TaskId} without permission", userId, id);
            return Forbid();
        }

        var task = await _taskService.GetTaskByIdAsync(id);

        if (task == null)
        {
            _logger.LogWarning("Task not found: {TaskId}", id);
            return NotFound(new { message = $"Task with ID {id} not found" });
        }

        return Ok(task);
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        _logger.LogInformation("User {UserId} ({Role}) is creating a new task: {TaskTitle}", 
            userId, userRole, dto.Title);

        try
        {
            var task = await _taskService.CreateTaskAsync(dto);

            _logger.LogInformation("Task created successfully: {TaskId} - {TaskTitle}", 
                task.Id, task.Title);

            return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Failed to create task: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

        _logger.LogInformation("User {UserId} is updating task {TaskId}", userId, id);

        // Check if user can access this task
        var canAccess = await _taskService.CanUserAccessTaskAsync(id, userId, userRole);
        if (!canAccess)
        {
            _logger.LogWarning("User {UserId} attempted to update task {TaskId} without permission", userId, id);
            return Forbid();
        }

        var task = await _taskService.UpdateTaskAsync(id, dto, userId);

        if (task == null)
        {
            _logger.LogWarning("Task not found for update: {TaskId}", id);
            return NotFound(new { message = $"Task with ID {id} not found" });
        }

        _logger.LogInformation("Task updated successfully: {TaskId}", id);

        return Ok(task);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogWarning("User {UserId} is deleting task {TaskId}", userId, id);

        var success = await _taskService.DeleteTaskAsync(id);

        if (!success)
        {
            _logger.LogWarning("Task not found for deletion: {TaskId}", id);
            return NotFound(new { message = $"Task with ID {id} not found" });
        }

        _logger.LogInformation("Task deleted successfully: {TaskId}", id);

        return Ok(new { message = $"Task {id} deleted successfully" });
    }

    [HttpPost("{taskId}/assign/{userId}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> AssignTaskToUser(Guid taskId, Guid userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("User {CurrentUserId} is assigning task {TaskId} to user {UserId}", 
            currentUserId, taskId, userId);

        var success = await _taskService.AssignTaskToUserAsync(taskId, userId);

        if (!success)
        {
            _logger.LogWarning("Failed to assign task. Task: {TaskId}, User: {UserId}", taskId, userId);
            return BadRequest(new { message = "Failed to assign task. Task or user not found." });
        }

        _logger.LogInformation("Task assigned successfully. Task: {TaskId}, User: {UserId}", taskId, userId);

        return Ok(new { message = "Task assigned successfully" });
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            message = "Tasks API is running",
            timestamp = DateTime.UtcNow
        });
    }
}
