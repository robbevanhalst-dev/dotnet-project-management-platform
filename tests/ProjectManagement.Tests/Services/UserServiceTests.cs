using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.Services;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Infrastructure.Data;

namespace ProjectManagement.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _userService = new UserService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetAllUsersAsync Tests

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnPagedUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Email = "user1@test.com", Role = "User" },
            new User { Id = Guid.NewGuid(), Email = "user2@test.com", Role = "Manager" },
            new User { Id = Guid.NewGuid(), Email = "user3@test.com", Role = "Admin" }
        };
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _userService.GetAllUsersAsync(paginationParams);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            _context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = $"user{i}@test.com",
                Role = "User"
            });
        }
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams { PageNumber = 2, PageSize = 5 };

        // Act
        var result = await _userService.GetAllUsersAsync(paginationParams);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(3);
        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllUsersAsync_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        _context.Users.AddRange(new List<User>
        {
            new User { Id = Guid.NewGuid(), Email = "admin@test.com", Role = "Admin" },
            new User { Id = Guid.NewGuid(), Email = "manager@test.com", Role = "Manager" },
            new User { Id = Guid.NewGuid(), Email = "user@test.com", Role = "User" }
        });
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "admin"
        };

        // Act
        var result = await _userService.GetAllUsersAsync(paginationParams);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Email.Should().Contain("admin");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithRoleSearch_ShouldFilterByRole()
    {
        // Arrange
        _context.Users.AddRange(new List<User>
        {
            new User { Id = Guid.NewGuid(), Email = "admin1@test.com", Role = "Admin" },
            new User { Id = Guid.NewGuid(), Email = "admin2@test.com", Role = "Admin" },
            new User { Id = Guid.NewGuid(), Email = "user@test.com", Role = "User" }
        });
        await _context.SaveChangesAsync();

        var paginationParams = new PaginationParams
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Admin"
        };

        // Act
        var result = await _userService.GetAllUsersAsync(paginationParams);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(u => u.Role.Should().Be("Admin"));
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("test@example.com");
        result.Role.Should().Be("User");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _userService.GetUserByIdAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateUserRoleAsync Tests

    [Fact]
    public async Task UpdateUserRoleAsync_WithValidData_ShouldUpdateRole()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.UpdateUserRoleAsync(user.Id, "Manager");

        // Assert
        result.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.Role.Should().Be("Manager");
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _userService.UpdateUserRoleAsync(Guid.NewGuid(), "Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserRoleAsync_ToAdmin_ShouldUpdateSuccessfully()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "manager@test.com",
            Role = "Manager"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.UpdateUserRoleAsync(user.Id, "Admin");

        // Assert
        result.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.Role.Should().Be("Admin");
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WithValidId_ShouldDeleteUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "delete@test.com",
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.DeleteUserAsync(user.Id);

        // Assert
        result.Should().BeTrue();

        var deletedUser = await _context.Users.FindAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUserAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _userService.DeleteUserAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldCascadeDeleteRefreshTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            Role = "User"
        };
        _context.Users.Add(user);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1"
        };
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.DeleteUserAsync(userId);

        // Assert
        result.Should().BeTrue();

        var deletedToken = await _context.RefreshTokens.FindAsync(refreshToken.Id);
        deletedToken.Should().BeNull();
    }

    #endregion

    #region GetManagersAsync Tests

    [Fact]
    public async Task GetManagersAsync_ShouldReturnOnlyManagersAndAdmins()
    {
        // Arrange
        _context.Users.AddRange(new List<User>
        {
            new User { Id = Guid.NewGuid(), Email = "user1@test.com", Role = "User" },
            new User { Id = Guid.NewGuid(), Email = "user2@test.com", Role = "User" },
            new User { Id = Guid.NewGuid(), Email = "manager1@test.com", Role = "Manager" },
            new User { Id = Guid.NewGuid(), Email = "manager2@test.com", Role = "Manager" },
            new User { Id = Guid.NewGuid(), Email = "admin@test.com", Role = "Admin" }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetManagersAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(u =>
            u.Role.Should().Match(r => r == "Manager" || r == "Admin"));
    }

    [Fact]
    public async Task GetManagersAsync_WhenNoManagers_ShouldReturnEmptyList()
    {
        // Arrange
        _context.Users.AddRange(new List<User>
        {
            new User { Id = Guid.NewGuid(), Email = "user1@test.com", Role = "User" },
            new User { Id = Guid.NewGuid(), Email = "user2@test.com", Role = "User" }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetManagersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetManagersAsync_ShouldNotReturnRegularUsers()
    {
        // Arrange
        _context.Users.AddRange(new List<User>
        {
            new User { Id = Guid.NewGuid(), Email = "user@test.com", Role = "User" },
            new User { Id = Guid.NewGuid(), Email = "manager@test.com", Role = "Manager" },
            new User { Id = Guid.NewGuid(), Email = "admin@test.com", Role = "Admin" }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetManagersAsync();

        // Assert
        result.Should().NotContain(u => u.Role == "User");
        result.Should().HaveCount(2);
    }

    #endregion
}
