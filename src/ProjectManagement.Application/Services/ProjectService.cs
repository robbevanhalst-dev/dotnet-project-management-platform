using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Interfaces;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Application.Services;

public class ProjectService : IProjectService
{
    private readonly ProjectManagementDbContext _context;

    public ProjectService(ProjectManagementDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProjectResponseDto>> GetAllProjectsAsync(PaginationParams paginationParams)
    {
        var query = _context.Projects
            .Include(p => p.Members)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(paginationParams.SearchTerm))
        {
            query = query.Where(p => p.Name.Contains(paginationParams.SearchTerm) ||
                                   (p.Description != null && p.Description.Contains(paginationParams.SearchTerm)));
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(paginationParams.SortBy))
        {
            query = paginationParams.SortBy.ToLower() switch
            {
                "name" => paginationParams.SortDescending
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name),
                "createdat" => paginationParams.SortDescending
                    ? query.OrderByDescending(p => p.CreatedAt)
                    : query.OrderBy(p => p.CreatedAt),
                _ => query.OrderBy(p => p.Name)
            };
        }
        else
        {
            query = query.OrderBy(p => p.Name);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(p => new ProjectResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                MemberCount = p.Members.Count,
                TaskCount = _context.Tasks.Count(t => t.ProjectId == p.Id)
            })
            .ToListAsync();

        return new PagedResult<ProjectResponseDto>(
            items,
            totalCount,
            paginationParams.PageNumber,
            paginationParams.PageSize
        );
    }

    public async Task<ProjectResponseDto?> GetProjectByIdAsync(Guid id)
    {
        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
            return null;

        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            MemberCount = project.Members.Count,
            TaskCount = await _context.Tasks.CountAsync(t => t.ProjectId == id)
        };
    }

    public async Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto dto, Guid createdByUserId)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);

        // Add creator as project member
        var projectMember = new ProjectMember
        {
            UserId = createdByUserId,
            ProjectId = project.Id,
            JoinedAt = DateTime.UtcNow
        };
        _context.ProjectMembers.Add(projectMember);

        await _context.SaveChangesAsync();

        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            MemberCount = 1,
            TaskCount = 0
        };
    }

    public async Task<ProjectResponseDto?> UpdateProjectAsync(Guid id, UpdateProjectDto dto)
    {
        var project = await _context.Projects.FindAsync(id);
        
        if (project == null)
            return null;

        project.Name = dto.Name;
        project.Description = dto.Description;

        await _context.SaveChangesAsync();

        var memberCount = await _context.ProjectMembers.CountAsync(pm => pm.ProjectId == id);
        var taskCount = await _context.Tasks.CountAsync(t => t.ProjectId == id);

        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            MemberCount = memberCount,
            TaskCount = taskCount
        };
    }

    public async Task<bool> DeleteProjectAsync(Guid id)
    {
        var project = await _context.Projects.FindAsync(id);
        
        if (project == null)
            return false;

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AddMemberToProjectAsync(Guid projectId, Guid userId)
    {
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);

        if (!projectExists || !userExists)
            return false;

        var memberExists = await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

        if (memberExists)
            return false;

        var projectMember = new ProjectMember
        {
            UserId = userId,
            ProjectId = projectId,
            JoinedAt = DateTime.UtcNow
        };

        _context.ProjectMembers.Add(projectMember);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveMemberFromProjectAsync(Guid projectId, Guid userId)
    {
        var projectMember = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

        if (projectMember == null)
            return false;

        _context.ProjectMembers.Remove(projectMember);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IsUserProjectMemberAsync(Guid projectId, Guid userId)
    {
        return await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
    }
}
