# Architecture Guide

Clean Architecture implementation and design patterns.

## Overview

This project implements **Clean Architecture** (also known as Onion Architecture or Hexagonal Architecture) to ensure:

- ? Separation of concerns
- ? Testability
- ? Independence from frameworks
- ? Independence from UI
- ? Independence from database

## Layer Structure

```
ProjectManagement.sln
?
??? Domain (Core)
?   ??? Entities
?   ??? Constants
?
??? Application (Use Cases)
?   ??? Services
?   ??? DTOs
?   ??? Interfaces
?   ??? Common
?
??? Infrastructure (External)
?   ??? Data (EF Core)
?   ??? Migrations
?
??? API (Presentation)
?   ??? Controllers
?   ??? Middleware
?   ??? Program.cs
?
??? Tests
    ??? Services
```

## Dependency Flow

```
API ? Application ? Domain
  ?         ?
Infrastructure ?
```

**Rules:**
- Domain has **NO** dependencies
- Application depends only on Domain
- Infrastructure depends on Application and Domain
- API depends on Application (and injects Infrastructure)

## Layer Responsibilities

### Domain Layer

**Purpose:** Core business logic and entities

**Contains:**
- Entities (User, Project, Task, RefreshToken)
- Constants (Roles)
- Domain logic (no infrastructure)

**Dependencies:** None

**Example:**
```csharp
namespace ProjectManagement.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
```

### Application Layer

**Purpose:** Business logic and use cases

**Contains:**
- Services (AuthService, ProjectService, TaskService, UserService)
- DTOs (Data Transfer Objects)
- Interfaces (IAuthService, IProjectService, etc.)
- Pagination helpers

**Dependencies:** Domain

**Example:**
```csharp
namespace ProjectManagement.Application.Services;

public class ProjectService : IProjectService
{
    private readonly ProjectManagementDbContext _context;

    public ProjectService(ProjectManagementDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto dto, Guid userId)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return new ProjectResponseDto { ... };
    }
}
```

### Infrastructure Layer

**Purpose:** External concerns (database, file system, etc.)

**Contains:**
- DbContext
- Migrations
- Database configuration

**Dependencies:** Application, Domain

**Example:**
```csharp
namespace ProjectManagement.Infrastructure.Data;

public class ProjectManagementDbContext : DbContext
{
    public ProjectManagementDbContext(DbContextOptions<ProjectManagementDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships
    }
}
```

### API Layer

**Purpose:** HTTP endpoints and middleware

**Contains:**
- Controllers
- Middleware (global exception handler)
- Program.cs (DI container)
- Filters

**Dependencies:** Application

**Example:**
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var project = await _projectService.CreateProjectAsync(dto, userId);
        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
    }
}
```

## Design Patterns

### Dependency Injection

**Registration (Program.cs):**
```csharp
builder.Services.AddDbContext<ProjectManagementDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IUserService, UserService>();
```

**Usage:**
```csharp
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    // Constructor injection
    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }
}
```

### Service Pattern

**Interface:**
```csharp
public interface IProjectService
{
    Task<PagedResult<ProjectResponseDto>> GetAllProjectsAsync(PaginationParams paginationParams);
    Task<ProjectResponseDto?> GetProjectByIdAsync(Guid id);
    Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto dto, Guid userId);
}
```

**Implementation:**
```csharp
public class ProjectService : IProjectService
{
    private readonly ProjectManagementDbContext _context;

    public ProjectService(ProjectManagementDbContext context)
    {
        _context = context;
    }

    // Implement methods...
}
```

### DTO Pattern

**Separation of concerns:**
```csharp
// Request DTO
public class CreateProjectDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
}

// Response DTO
public class ProjectResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public int TaskCount { get; set; }
}
```

### Middleware Pattern

**Global Exception Handling:**
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

### Repository Pattern (Implicit)

DbContext acts as Unit of Work + Repository:
```csharp
// Unit of Work
await _context.SaveChangesAsync();

// Repository methods via DbSet
_context.Projects.Add(project);
_context.Projects.Remove(project);
await _context.Projects.FindAsync(id);
await _context.Projects.ToListAsync();
```

## SOLID Principles

### Single Responsibility Principle (SRP)

Each class has one reason to change:
- `AuthService` - Authentication only
- `ProjectService` - Project management only
- `TaskService` - Task management only

### Open/Closed Principle (OCP)

Open for extension, closed for modification:
```csharp
// Extend via inheritance or interfaces
public interface IAuthService { }
public class AuthService : IAuthService { }
public class ExtendedAuthService : AuthService { }  // Extension
```

### Liskov Substitution Principle (LSP)

Subtypes must be substitutable:
```csharp
IAuthService service = new AuthService();
// Can be replaced with any IAuthService implementation
```

### Interface Segregation Principle (ISP)

Many specific interfaces > one general interface:
```csharp
// ? Good - Specific interfaces
public interface IAuthService { }
public interface IProjectService { }
public interface ITaskService { }

// ? Bad - One large interface
public interface IDataService
{
    // Auth methods
    // Project methods
    // Task methods
}
```

### Dependency Inversion Principle (DIP)

Depend on abstractions, not concretions:
```csharp
// ? Good - Depend on interface
public class ProjectsController
{
    private readonly IProjectService _service;
}

// ? Bad - Depend on concrete class
public class ProjectsController
{
    private readonly ProjectService _service;
}
```

## Benefits

### Testability

```csharp
// Easy to mock dependencies
var mockService = new Mock<IProjectService>();
var controller = new ProjectsController(mockService.Object);
```

### Maintainability

- Clear separation of concerns
- Each layer can evolve independently
- Easy to locate code

### Flexibility

- Swap implementations (e.g., SQL Server ? PostgreSQL)
- Add new features without breaking existing code
- Support multiple UIs (Web API, gRPC, GraphQL)

## Anti-Patterns to Avoid

? **Direct database access in controllers:**
```csharp
// ? Bad
public class ProjectsController
{
    private readonly DbContext _context;
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _context.Projects.ToListAsync());
    }
}
```

? **Business logic in controllers:**
```csharp
// ? Bad
[HttpPost]
public async Task<IActionResult> Create(Project project)
{
    if (project.Name.Length < 3) return BadRequest();
    project.CreatedAt = DateTime.UtcNow;
    _context.Add(project);
    await _context.SaveChangesAsync();
}
```

? **Infrastructure dependencies in Domain:**
```csharp
// ? Bad - Domain should not reference EF Core
public class User
{
    [Key]  // EF Core attribute
    public Guid Id { get; set; }
}
```

---

**See also:**
- [Testing Guide](TESTING.md) - Testing the architecture
- [API Reference](API_REFERENCE.md) - API design
- [Documentation Overview](DOCUMENTATION_OVERVIEW.md) - All documentation
