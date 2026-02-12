# Testing Guide

Unit tests, test coverage, and testing strategies.

## Test Framework

- **Framework:** xUnit 2.9
- **Assertions:** FluentAssertions
- **Database:** In-Memory (EF Core)

## Test Statistics

**72 Unit Tests** (100% passing):
- ? AuthService (22 tests)
- ? ProjectService (18 tests)
- ? TaskService (20 tests)
- ? UserService (12 tests)

**Coverage:** ~95%  
**Duration:** ~6 seconds

## Running Tests

```bash
# Run all tests
dotnet test

# Detailed output
dotnet test --verbosity detailed

# Run specific test class
dotnet test --filter "FullyQualifiedName~ProjectServiceTests"

# Run tests for specific service
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

## Test Structure

### AAA Pattern (Arrange-Act-Assert)

```csharp
[Fact]
public async Task RegisterAsync_WithValidData_ShouldCreateUser()
{
    // Arrange
    var registerDto = new RegisterDto
    {
        Email = "test@example.com",
        Password = "Password123",
        Role = "User"
    };

    // Act
    var result = await _authService.RegisterAsync(registerDto);

    // Assert
    result.Should().BeTrue();
    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == registerDto.Email);
    user.Should().NotBeNull();
    user!.Email.Should().Be(registerDto.Email);
}
```

## Test Coverage by Service

### AuthService (22 tests)

**Register Tests (7):**
- Valid data creates user
- Duplicate email returns false
- Manager role assignment
- Admin role assignment
- Default role (User)
- Password hashing
- Multiple concurrent registrations

**Login Tests (4):**
- Valid credentials return token
- Invalid email returns null
- Wrong password returns null
- Case-sensitive email handling

**JWT Token Tests (7):**
- Valid JWT generation
- UserId claim presence
- Email claim presence
- Role claim correctness
- Token expiration
- Multiple role validation
- Token format validation

**Configuration Tests (1):**
- JWT Key validation (min 32 chars)

**Edge Cases (3):**
- Empty password handling
- Concurrent login attempts
- Token regeneration

### ProjectService (18 tests)

**GetAllProjectsAsync (3):**
- Paged results
- Pagination correctness
- Search filtering

**GetProjectByIdAsync (2):**
- Valid ID returns project
- Invalid ID returns null

**CreateProjectAsync (2):**
- Valid data creates project
- Creator auto-added as member

**UpdateProjectAsync (2):**
- Valid update succeeds
- Invalid ID returns null

**DeleteProjectAsync (2):**
- Valid delete succeeds
- Invalid ID returns false

**Member Management (7):**
- Add member to project
- Prevent duplicate membership
- Remove member from project
- Check if user is member
- Project/User validation

### TaskService (20 tests)

**GetTasksAsync (3):**
- Filter by user
- Search filtering
- Filter by project

**GetAllTasksAsync (1):**
- Returns all tasks (admin)

**GetTaskByIdAsync (2):**
- Valid ID returns task
- Invalid ID returns null

**CreateTaskAsync (3):**
- Valid data creates task
- Invalid project throws exception
- Task assignment to user

**UpdateTaskAsync (2):**
- Valid update succeeds
- Invalid ID returns null

**DeleteTaskAsync (2):**
- Valid delete succeeds
- Invalid ID returns false

**AssignTaskToUserAsync (2):**
- Valid assignment succeeds
- Invalid task ID returns false

**CanUserAccessTaskAsync (5):**
- Admin can access all
- Assigned user can access
- Project member can access
- Non-authorized returns false
- Authorization validation

### UserService (12 tests)

**GetAllUsersAsync (4):**
- Paged results
- Pagination correctness
- Search filtering
- Role filtering

**GetUserByIdAsync (2):**
- Valid ID returns user
- Invalid ID returns null

**UpdateUserRoleAsync (3):**
- Valid role update
- Invalid ID returns false
- Update to Admin role

**DeleteUserAsync (3):**
- Valid delete succeeds
- Invalid ID returns false
- Cascade delete refresh tokens

**GetManagersAsync (3):**
- Returns only Managers/Admins
- Empty list when no managers
- Excludes regular users

## Writing Tests

### Test Class Setup

```csharp
public class ServiceTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly ServiceToTest _service;

    public ServiceTests()
    {
        // In-memory database
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ProjectManagementDbContext(options);
        _service = new ServiceToTest(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task TestMethod()
    {
        // Test implementation
    }
}
```

### FluentAssertions Examples

```csharp
// Basic assertions
result.Should().BeTrue();
result.Should().NotBeNull();

// String assertions
user.Email.Should().Be("test@example.com");
user.Email.Should().Contain("test");

// Collection assertions
users.Should().HaveCount(3);
users.Should().AllSatisfy(u => u.Role.Should().Be("Admin"));

// DateTime assertions
task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

// Exception assertions
await Assert.ThrowsAsync<KeyNotFoundException>(() => service.Method());
```

### Theory Tests (Data-Driven)

```csharp
[Theory]
[InlineData("User")]
[InlineData("Manager")]
[InlineData("Admin")]
public async Task Method_WithDifferentRoles_ShouldWork(string role)
{
    // Test implementation
}
```

## Test Isolation

Each test uses a unique in-memory database:
```csharp
.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
```

Benefits:
- ? No shared state between tests
- ? Tests can run in parallel
- ? No need for cleanup between tests

## Continuous Integration

### GitHub Actions Example

```yaml
name: .NET Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '9.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

## Code Coverage

Generate coverage report:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

View coverage:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport"
```

---

**See also:**
- [Architecture](ARCHITECTURE.md) - Testable architecture
- [Getting Started](GETTING_STARTED.md) - Run tests
- [Documentation Overview](DOCUMENTATION_OVERVIEW.md) - All documentation
