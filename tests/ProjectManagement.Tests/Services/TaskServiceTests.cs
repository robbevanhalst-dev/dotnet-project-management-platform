using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Services;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Tests.Services;

public class TaskServiceTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _taskService = new TaskService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetTasksAsync Tests

    [Fact]
    public async Task GetTasksAsync_ForSpecificUser_ShouldReturnOnlyUserTasks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Users.Add(new User { Id = userId, Email = "user@test.com", Role = "User" });

        _context.Tasks.AddRange(new List<TaskItem>
        {
            new TaskItem { Id = Guid.NewGuid(), Title = "User Task 1", ProjectId = projectId, AssignedUserId = userId, Status = "ToDo", CreatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "User Task 2", ProjectId = projectId, AssignedUserId = userId, Status = "InProgress", CreatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Other Task", ProjectId = projectId, AssignedUserId = Guid.NewGuid(), Status = "ToDo", CreatedAt = DateTime.UtcNow }
        });
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _taskService.GetTasksAsync(paginationParams, userId);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(t => t.AssignedUserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetTasksAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });

        _context.Tasks.AddRange(new List<TaskItem>
        {
            new TaskItem { Id = Guid.NewGuid(), Title = "Design Homepage", ProjectId = projectId, Status = "ToDo", CreatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Implement API", ProjectId = projectId, Status = "ToDo", CreatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Design Database", ProjectId = projectId, Status = "ToDo", CreatedAt = DateTime.UtcNow }
        });
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Design"
        };

        // Act
        var result = await _taskService.GetTasksAsync(paginationParams);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(t => t.Title.Should().Contain("Design"));
    }

    [Fact]
    public async Task GetTasksAsync_ForSpecificProject_ShouldReturnOnlyProjectTasks()
    {
        // Arrange
        var project1Id = Guid.NewGuid();
        var project2Id = Guid.NewGuid();

        _context.Projects.AddRange(new List<Project>
        {
            new Project { Id = project1Id, Name = "Project 1", CreatedAt = DateTime.UtcNow },
            new Project { Id = project2Id, Name = "Project 2", CreatedAt = DateTime.UtcNow }
        });

        _context.Tasks.AddRange(new List<TaskItem>
        {
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 1", ProjectId = project1Id, Status = "ToDo", CreatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 2", ProjectId = project1Id, Status = "ToDo", CreatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 3", ProjectId = project2Id, Status = "ToDo", CreatedAt = DateTime.UtcNow }
        });
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _taskService.GetTasksAsync(paginationParams, null, project1Id);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(t => t.ProjectId.Should().Be(project1Id));
    }

    #endregion

    #region GetAllTasksAsync Tests

    [Fact]
    public async Task GetAllTasksAsync_ShouldReturnAllTasks()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });

        _context.Tasks.AddRange(new List<TaskItem>
        {
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 1", ProjectId = projectId, Status = "ToDo", CreatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 2", ProjectId = projectId, Status = "InProgress", CreatedAt = DateTime.UtcNow },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 3", ProjectId = projectId, Status = "Done", CreatedAt = DateTime.UtcNow }
        });
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _taskService.GetAllTasksAsync(paginationParams);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    #endregion

    #region GetTaskByIdAsync Tests

    [Fact]
    public async Task GetTaskByIdAsync_WithValidId_ShouldReturnTask()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            Title = "Test Task",
            Description = "Test Description",
            ProjectId = projectId,
            Status = "ToDo",
            Priority = "High",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.GetTaskByIdAsync(taskId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(taskId);
        result.Title.Should().Be("Test Task");
        result.Description.Should().Be("Test Description");
        result.Status.Should().Be("ToDo");
        result.Priority.Should().Be("High");
    }

    [Fact]
    public async Task GetTaskByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _taskService.GetTaskByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateTaskAsync Tests

    [Fact]
    public async Task CreateTaskAsync_WithValidData_ShouldCreateTask()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var dto = new CreateTaskDto
        {
            Title = "New Task",
            Description = "Task Description",
            ProjectId = projectId,
            Status = "ToDo",
            Priority = "Medium"
        };

        // Act
        var result = await _taskService.CreateTaskAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Task");
        result.Description.Should().Be("Task Description");
        result.Status.Should().Be("ToDo");
        result.Priority.Should().Be("Medium");

        var taskInDb = await _context.Tasks.FindAsync(result.Id);
        taskInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateTaskAsync_WithInvalidProjectId_ShouldThrowException()
    {
        // Arrange
        var dto = new CreateTaskDto
        {
            Title = "Task",
            ProjectId = Guid.NewGuid(), // Non-existent project
            Status = "ToDo"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _taskService.CreateTaskAsync(dto));
    }

    [Fact]
    public async Task CreateTaskAsync_WithAssignedUser_ShouldAssignTask()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Users.Add(new User { Id = userId, Email = "user@test.com", Role = "User" });
        await _context.SaveChangesAsync();

        var dto = new CreateTaskDto
        {
            Title = "Assigned Task",
            ProjectId = projectId,
            AssignedUserId = userId,
            Status = "ToDo"
        };

        // Act
        var result = await _taskService.CreateTaskAsync(dto);

        // Assert
        result.AssignedUserId.Should().Be(userId);
        result.AssignedUserEmail.Should().Be("user@test.com");
    }

    #endregion

    #region UpdateTaskAsync Tests

    [Fact]
    public async Task UpdateTaskAsync_WithValidData_ShouldUpdateTask()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            Title = "Old Title",
            Description = "Old Description",
            ProjectId = projectId,
            Status = "ToDo",
            Priority = "Low",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new UpdateTaskDto
        {
            Title = "New Title",
            Description = "New Description",
            Status = "InProgress",
            Priority = "High"
        };

        // Act
        var result = await _taskService.UpdateTaskAsync(taskId, dto, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("New Title");
        result.Description.Should().Be("New Description");
        result.Status.Should().Be("InProgress");
        result.Priority.Should().Be("High");

        var updatedTask = await _context.Tasks.FindAsync(taskId);
        updatedTask!.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task UpdateTaskAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var dto = new UpdateTaskDto { Title = "Title", Status = "ToDo" };

        // Act
        var result = await _taskService.UpdateTaskAsync(Guid.NewGuid(), dto, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteTaskAsync Tests

    [Fact]
    public async Task DeleteTaskAsync_WithValidId_ShouldDeleteTask()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            Title = "Task to Delete",
            ProjectId = projectId,
            Status = "ToDo",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.DeleteTaskAsync(taskId);

        // Assert
        result.Should().BeTrue();

        var deletedTask = await _context.Tasks.FindAsync(taskId);
        deletedTask.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTaskAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _taskService.DeleteTaskAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region AssignTaskToUserAsync Tests

    [Fact]
    public async Task AssignTaskToUserAsync_WithValidData_ShouldAssignTask()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Users.Add(new User { Id = userId, Email = "user@test.com", Role = "User" });
        _context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            Title = "Task",
            ProjectId = projectId,
            Status = "ToDo",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.AssignTaskToUserAsync(taskId, userId);

        // Assert
        result.Should().BeTrue();

        var task = await _context.Tasks.FindAsync(taskId);
        task!.AssignedUserId.Should().Be(userId);
    }

    [Fact]
    public async Task AssignTaskToUserAsync_WithInvalidTaskId_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _context.Users.Add(new User { Id = userId, Email = "user@test.com", Role = "User" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.AssignTaskToUserAsync(Guid.NewGuid(), userId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CanUserAccessTaskAsync Tests

    [Fact]
    public async Task CanUserAccessTaskAsync_AdminUser_ShouldReturnTrue()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            Title = "Task",
            ProjectId = projectId,
            Status = "ToDo",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.CanUserAccessTaskAsync(taskId, userId, "Admin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanUserAccessTaskAsync_AssignedUser_ShouldReturnTrue()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            Title = "Task",
            ProjectId = projectId,
            AssignedUserId = userId,
            Status = "ToDo",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.CanUserAccessTaskAsync(taskId, userId, "User");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanUserAccessTaskAsync_ProjectMember_ShouldReturnTrue()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            Title = "Task",
            ProjectId = projectId,
            Status = "ToDo",
            CreatedAt = DateTime.UtcNow
        });
        _context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = projectId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.CanUserAccessTaskAsync(taskId, userId, "User");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanUserAccessTaskAsync_NotAuthorized_ShouldReturnFalse()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Tasks.Add(new TaskItem
        {
            Id = taskId,
            Title = "Task",
            ProjectId = projectId,
            Status = "ToDo",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _taskService.CanUserAccessTaskAsync(taskId, userId, "User");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
