using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Interfaces;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Application.Services;

public class TaskService : ITaskService
{
    private readonly ProjectManagementDbContext _context;

    public TaskService(ProjectManagementDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TaskResponseDto>> GetTasksAsync(
        PaginationParams paginationParams,
        Guid? userId = null,
        Guid? projectId = null)
    {
        var query = _context.Tasks.AsQueryable();

        // Filter by user (assigned tasks)
        if (userId.HasValue)
        {
            query = query.Where(t => t.AssignedUserId == userId);
        }

        // Filter by project
        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(paginationParams.SearchTerm))
        {
            query = query.Where(t => t.Title.Contains(paginationParams.SearchTerm) ||
                                   (t.Description != null && t.Description.Contains(paginationParams.SearchTerm)));
        }

        // Apply sorting
        query = ApplySorting(query, paginationParams);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                ProjectId = t.ProjectId,
                ProjectName = _context.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).FirstOrDefault() ?? "",
                AssignedUserId = t.AssignedUserId,
                AssignedUserEmail = t.AssignedUserId.HasValue
                    ? _context.Users.Where(u => u.Id == t.AssignedUserId).Select(u => u.Email).FirstOrDefault()
                    : null,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<TaskResponseDto>(
            items,
            totalCount,
            paginationParams.PageNumber,
            paginationParams.PageSize
        );
    }

    public async Task<PagedResult<TaskResponseDto>> GetAllTasksAsync(PaginationParams paginationParams)
    {
        return await GetTasksAsync(paginationParams, null, null);
    }

    public async Task<TaskResponseDto?> GetTaskByIdAsync(Guid id)
    {
        var task = await _context.Tasks
            .Where(t => t.Id == id)
            .Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                ProjectId = t.ProjectId,
                ProjectName = _context.Projects.Where(p => p.Id == t.ProjectId).Select(p => p.Name).FirstOrDefault() ?? "",
                AssignedUserId = t.AssignedUserId,
                AssignedUserEmail = t.AssignedUserId.HasValue
                    ? _context.Users.Where(u => u.Id == t.AssignedUserId).Select(u => u.Email).FirstOrDefault()
                    : null,
                Status = t.Status,
                Priority = t.Priority,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync();

        return task;
    }

    public async Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto)
    {
        // Validate project exists
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
        if (!projectExists)
        {
            throw new KeyNotFoundException($"Project with ID {dto.ProjectId} not found");
        }

        // Validate assigned user exists (if provided)
        if (dto.AssignedUserId.HasValue)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.AssignedUserId);
            if (!userExists)
            {
                throw new KeyNotFoundException($"User with ID {dto.AssignedUserId} not found");
            }
        }

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            ProjectId = dto.ProjectId,
            AssignedUserId = dto.AssignedUserId,
            Status = dto.Status,
            Priority = dto.Priority,
            DueDate = dto.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return (await GetTaskByIdAsync(task.Id))!;
    }

    public async Task<TaskResponseDto?> UpdateTaskAsync(Guid id, UpdateTaskDto dto, Guid userId)
    {
        var task = await _context.Tasks.FindAsync(id);
        
        if (task == null)
            return null;

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;
        task.Priority = dto.Priority;
        task.DueDate = dto.DueDate;

        await _context.SaveChangesAsync();

        return await GetTaskByIdAsync(id);
    }

    public async Task<bool> DeleteTaskAsync(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);
        
        if (task == null)
            return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AssignTaskToUserAsync(Guid taskId, Guid userId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);

        if (task == null || !userExists)
            return false;

        task.AssignedUserId = userId;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CanUserAccessTaskAsync(Guid taskId, Guid userId, string userRole)
    {
        // Admin can access all tasks
        if (userRole == "Admin")
            return true;

        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
            return false;

        // Check if user is assigned to the task
        if (task.AssignedUserId == userId)
            return true;

        // Check if user is a member of the project
        var isMember = await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId);

        return isMember;
    }

    private IQueryable<TaskItem> ApplySorting(IQueryable<TaskItem> query, PaginationParams paginationParams)
    {
        if (string.IsNullOrWhiteSpace(paginationParams.SortBy))
            return query.OrderBy(t => t.CreatedAt);

        return paginationParams.SortBy.ToLower() switch
        {
            "title" => paginationParams.SortDescending
                ? query.OrderByDescending(t => t.Title)
                : query.OrderBy(t => t.Title),
            "status" => paginationParams.SortDescending
                ? query.OrderByDescending(t => t.Status)
                : query.OrderBy(t => t.Status),
            "priority" => paginationParams.SortDescending
                ? query.OrderByDescending(t => t.Priority)
                : query.OrderBy(t => t.Priority),
            "duedate" => paginationParams.SortDescending
                ? query.OrderByDescending(t => t.DueDate)
                : query.OrderBy(t => t.DueDate),
            "createdat" => paginationParams.SortDescending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
            _ => query.OrderBy(t => t.CreatedAt)
        };
    }
}
