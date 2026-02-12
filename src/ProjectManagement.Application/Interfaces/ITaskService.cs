using ProjectManagement.Application.Common;
using ProjectManagement.Application.DTOs;

namespace ProjectManagement.Application.Interfaces;

public interface ITaskService
{
    Task<PagedResult<TaskResponseDto>> GetTasksAsync(PaginationParams paginationParams, Guid? userId = null, Guid? projectId = null);
    Task<PagedResult<TaskResponseDto>> GetAllTasksAsync(PaginationParams paginationParams);
    Task<TaskResponseDto?> GetTaskByIdAsync(Guid id);
    Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto);
    Task<TaskResponseDto?> UpdateTaskAsync(Guid id, UpdateTaskDto dto, Guid userId);
    Task<bool> DeleteTaskAsync(Guid id);
    Task<bool> AssignTaskToUserAsync(Guid taskId, Guid userId);
    Task<bool> CanUserAccessTaskAsync(Guid taskId, Guid userId, string userRole);
}
