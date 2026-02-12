using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Services;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Tests.Services;

public class ProjectServiceTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly ProjectService _projectService;

    public ProjectServiceTests()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _projectService = new ProjectService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetAllProjectsAsync Tests

    [Fact]
    public async Task GetAllProjectsAsync_ShouldReturnPagedProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            new Project { Id = Guid.NewGuid(), Name = "Project 1", CreatedAt = DateTime.UtcNow },
            new Project { Id = Guid.NewGuid(), Name = "Project 2", CreatedAt = DateTime.UtcNow },
            new Project { Id = Guid.NewGuid(), Name = "Project 3", CreatedAt = DateTime.UtcNow }
        };
        _context.Projects.AddRange(projects);
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _projectService.GetAllProjectsAsync(paginationParams);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllProjectsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            _context.Projects.Add(new Project
            {
                Id = Guid.NewGuid(),
                Name = $"Project {i}",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams { PageNumber = 2, PageSize = 5 };

        // Act
        var result = await _projectService.GetAllProjectsAsync(paginationParams);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(3);
        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllProjectsAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        _context.Projects.AddRange(new List<Project>
        {
            new Project { Id = Guid.NewGuid(), Name = "Website Redesign", CreatedAt = DateTime.UtcNow },
            new Project { Id = Guid.NewGuid(), Name = "Mobile App", CreatedAt = DateTime.UtcNow },
            new Project { Id = Guid.NewGuid(), Name = "Website Backend", CreatedAt = DateTime.UtcNow }
        });
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Website"
        };

        // Act
        var result = await _projectService.GetAllProjectsAsync(paginationParams);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.Name.Should().Contain("Website"));
    }

    #endregion

    #region GetProjectByIdAsync Tests

    [Fact]
    public async Task GetProjectByIdAsync_WithValidId_ShouldReturnProject()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act
        var result = await _projectService.GetProjectByIdAsync(project.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(project.Id);
        result.Name.Should().Be("Test Project");
        result.Description.Should().Be("Test Description");
    }

    [Fact]
    public async Task GetProjectByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _projectService.GetProjectByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateProjectAsync Tests

    [Fact]
    public async Task CreateProjectAsync_WithValidData_ShouldCreateProject()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _context.Users.Add(new User { Id = userId, Email = "user@test.com", Role = "Manager" });
        await _context.SaveChangesAsync();

        var dto = new CreateProjectDto
        {
            Name = "New Project",
            Description = "Project Description"
        };

        // Act
        var result = await _projectService.CreateProjectAsync(dto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Project");
        result.Description.Should().Be("Project Description");

        var projectInDb = await _context.Projects.FindAsync(result.Id);
        projectInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProjectAsync_ShouldAddCreatorAsMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _context.Users.Add(new User { Id = userId, Email = "user@test.com", Role = "Manager" });
        await _context.SaveChangesAsync();

        var dto = new CreateProjectDto { Name = "Project", Description = "Desc" };

        // Act
        var result = await _projectService.CreateProjectAsync(dto, userId);

        // Assert
        var membership = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == result.Id && pm.UserId == userId);

        membership.Should().NotBeNull();
        membership!.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region UpdateProjectAsync Tests

    [Fact]
    public async Task UpdateProjectAsync_WithValidData_ShouldUpdateProject()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Description = "Old Description",
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var dto = new UpdateProjectDto
        {
            Name = "New Name",
            Description = "New Description"
        };

        // Act
        var result = await _projectService.UpdateProjectAsync(project.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        result.Description.Should().Be("New Description");

        var updatedProject = await _context.Projects.FindAsync(project.Id);
        updatedProject!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateProjectAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var dto = new UpdateProjectDto { Name = "Name", Description = "Desc" };

        // Act
        var result = await _projectService.UpdateProjectAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteProjectAsync Tests

    [Fact]
    public async Task DeleteProjectAsync_WithValidId_ShouldDeleteProject()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Project to Delete",
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        // Act
        var result = await _projectService.DeleteProjectAsync(project.Id);

        // Assert
        result.Should().BeTrue();

        var deletedProject = await _context.Projects.FindAsync(project.Id);
        deletedProject.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProjectAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _projectService.DeleteProjectAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Member Management Tests

    [Fact]
    public async Task AddMemberToProjectAsync_WithValidData_ShouldAddMember()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Users.Add(new User { Id = userId, Email = "user@test.com", Role = "User" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _projectService.AddMemberToProjectAsync(projectId, userId);

        // Assert
        result.Should().BeTrue();

        var membership = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        membership.Should().NotBeNull();
    }

    [Fact]
    public async Task AddMemberToProjectAsync_WhenAlreadyMember_ShouldReturnFalse()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Users.Add(new User { Id = userId, Email = "user@test.com", Role = "User" });
        _context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = projectId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _projectService.AddMemberToProjectAsync(projectId, userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveMemberFromProjectAsync_WithValidData_ShouldRemoveMember()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Projects.Add(new Project { Id = projectId, Name = "Project", CreatedAt = DateTime.UtcNow });
        _context.Users.Add(new User { Id = userId, Email = "user@test.com", Role = "User" });
        _context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = projectId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _projectService.RemoveMemberFromProjectAsync(projectId, userId);

        // Assert
        result.Should().BeTrue();

        var membership = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        membership.Should().BeNull();
    }

    [Fact]
    public async Task IsUserProjectMemberAsync_WhenMember_ShouldReturnTrue()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = projectId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _projectService.IsUserProjectMemberAsync(projectId, userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserProjectMemberAsync_WhenNotMember_ShouldReturnFalse()
    {
        // Act
        var result = await _projectService.IsUserProjectMemberAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
