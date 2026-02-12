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
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects([FromQuery] PaginationParams paginationParams)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        _logger.LogInformation("User {UserId} ({Email}) is requesting projects", userId, userEmail);

        var pagedResult = await _projectService.GetAllProjectsAsync(paginationParams);

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
    public async Task<IActionResult> GetProjectById(Guid id)
    {
        _logger.LogInformation("Getting project by ID: {ProjectId}", id);

        var project = await _projectService.GetProjectByIdAsync(id);
        
        if (project == null)
        {
            _logger.LogWarning("Project not found: {ProjectId}", id);
            return NotFound(new { message = $"Project with ID {id} not found" });
        }

        return Ok(project);
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        _logger.LogInformation("User {UserId} with role {Role} is creating a new project: {ProjectName}", 
            userId, userRole, dto.Name);

        var project = await _projectService.CreateProjectAsync(dto, userId);

        _logger.LogInformation("Project created successfully: {ProjectId} - {ProjectName}", 
            project.Id, project.Name);

        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("User {UserId} is updating project {ProjectId}", userId, id);

        var project = await _projectService.UpdateProjectAsync(id, dto);
        
        if (project == null)
        {
            _logger.LogWarning("Project not found for update: {ProjectId}", id);
            return NotFound(new { message = $"Project with ID {id} not found" });
        }

        _logger.LogInformation("Project updated successfully: {ProjectId}", id);

        return Ok(project);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogWarning("User {UserId} is deleting project {ProjectId}", userId, id);

        var success = await _projectService.DeleteProjectAsync(id);
        
        if (!success)
        {
            _logger.LogWarning("Project not found for deletion: {ProjectId}", id);
            return NotFound(new { message = $"Project with ID {id} not found" });
        }

        _logger.LogInformation("Project deleted successfully: {ProjectId}", id);

        return Ok(new { message = $"Project {id} deleted successfully" });
    }

    [HttpPost("{projectId}/members/{userId}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> AddMemberToProject(Guid projectId, Guid userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("User {CurrentUserId} is adding user {UserId} to project {ProjectId}", 
            currentUserId, userId, projectId);

        var success = await _projectService.AddMemberToProjectAsync(projectId, userId);
        
        if (!success)
        {
            _logger.LogWarning("Failed to add member to project. Project: {ProjectId}, User: {UserId}", 
                projectId, userId);
            return BadRequest(new { message = "Failed to add member. Project or user not found, or user is already a member." });
        }

        _logger.LogInformation("Member added successfully. Project: {ProjectId}, User: {UserId}", 
            projectId, userId);

        return Ok(new { message = "Member added successfully" });
    }

    [HttpDelete("{projectId}/members/{userId}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> RemoveMemberFromProject(Guid projectId, Guid userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation("User {CurrentUserId} is removing user {UserId} from project {ProjectId}", 
            currentUserId, userId, projectId);

        var success = await _projectService.RemoveMemberFromProjectAsync(projectId, userId);
        
        if (!success)
        {
            _logger.LogWarning("Failed to remove member from project. Project: {ProjectId}, User: {UserId}", 
                projectId, userId);
            return NotFound(new { message = "Member not found in project" });
        }

        _logger.LogInformation("Member removed successfully. Project: {ProjectId}, User: {UserId}", 
            projectId, userId);

        return Ok(new { message = "Member removed successfully" });
    }
}
