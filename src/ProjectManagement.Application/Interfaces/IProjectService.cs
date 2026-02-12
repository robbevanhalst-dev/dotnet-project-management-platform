using ProjectManagement.Application.Common;
using ProjectManagement.Application.DTOs;

namespace ProjectManagement.Application.Interfaces;

public interface IProjectService
{
    Task<PagedResult<ProjectResponseDto>> GetAllProjectsAsync(PaginationParams paginationParams);
    Task<ProjectResponseDto?> GetProjectByIdAsync(Guid id);
    Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto dto, Guid createdByUserId);
    Task<ProjectResponseDto?> UpdateProjectAsync(Guid id, UpdateProjectDto dto);
    Task<bool> DeleteProjectAsync(Guid id);
    Task<bool> AddMemberToProjectAsync(Guid projectId, Guid userId);
    Task<bool> RemoveMemberFromProjectAsync(Guid projectId, Guid userId);
    Task<bool> IsUserProjectMemberAsync(Guid projectId, Guid userId);
}
