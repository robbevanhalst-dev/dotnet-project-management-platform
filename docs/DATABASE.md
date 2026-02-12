# Database Guide

Database schema, migrations, and Entity Framework Core configuration.

## Database Provider

- **Provider:** SQL Server
- **Development:** LocalDB
- **ORM:** Entity Framework Core 9.0

## Connection String

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectManagementDb;Trusted_Connection=true"
  }
}
```

## Schema

### User
```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(50) NOT NULL DEFAULT 'User'
);
```

### RefreshToken
```sql
CREATE TABLE RefreshTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Token NVARCHAR(MAX) NOT NULL UNIQUE,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    RevokedAt DATETIME2 NULL,
    CreatedByIp NVARCHAR(50),
    RevokedByIp NVARCHAR(50),
    ReplacedByToken NVARCHAR(MAX),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

### Project
```sql
CREATE TABLE Projects (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000),
    CreatedAt DATETIME2 NOT NULL
);
```

### ProjectMember
```sql
CREATE TABLE ProjectMembers (
    UserId UNIQUEIDENTIFIER NOT NULL,
    ProjectId UNIQUEIDENTIFIER NOT NULL,
    JoinedAt DATETIME2 NOT NULL,
    PRIMARY KEY (UserId, ProjectId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
);
```

### TaskItem
```sql
CREATE TABLE Tasks (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(2000),
    ProjectId UNIQUEIDENTIFIER NOT NULL,
    AssignedUserId UNIQUEIDENTIFIER NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'ToDo',
    Priority NVARCHAR(50),
    DueDate DATETIME2,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE,
    FOREIGN KEY (AssignedUserId) REFERENCES Users(Id) ON DELETE SET NULL
);
```

## Relationships

```
User (1) ??? (N) RefreshTokens
User (N) ??? (M) Projects (via ProjectMembers)
User (1) ??? (N) Tasks (assigned)
Project (1) ??? (N) Tasks
```

## Delete Behaviors

| Parent | Child | Behavior |
|--------|-------|----------|
| User | RefreshTokens | CASCADE |
| User | ProjectMembers | CASCADE |
| User | Tasks (assigned) | SET NULL |
| Project | Tasks | CASCADE |
| Project | ProjectMembers | CASCADE |

## Migrations

### Create Migration

```bash
dotnet ef migrations add MigrationName \
  --project src/ProjectManagement.Infrastructure \
  --startup-project src/ProjectManagement.Api/ProjectManagement.Api
```

### Apply Migration

```bash
dotnet ef database update \
  --project src/ProjectManagement.Infrastructure \
  --startup-project src/ProjectManagement.Api/ProjectManagement.Api
```

### Rollback Migration

```bash
dotnet ef database update PreviousMigration \
  --project src/ProjectManagement.Infrastructure \
  --startup-project src/ProjectManagement.Api/ProjectManagement.Api
```

### Remove Last Migration

```bash
dotnet ef migrations remove \
  --project src/ProjectManagement.Infrastructure \
  --startup-project src/ProjectManagement.Api/ProjectManagement.Api
```

## Reset Database

```bash
# Drop database
dotnet ef database drop --force \
  --project src/ProjectManagement.Infrastructure \
  --startup-project src/ProjectManagement.Api/ProjectManagement.Api

# Recreate
dotnet ef database update \
  --project src/ProjectManagement.Infrastructure \
  --startup-project src/ProjectManagement.Api/ProjectManagement.Api
```

## DbContext Configuration

**ProjectManagementDbContext.cs:**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Composite key for ProjectMember
    modelBuilder.Entity<ProjectMember>()
        .HasKey(pm => new { pm.UserId, pm.ProjectId });

    // Task -> Project (CASCADE)
    modelBuilder.Entity<TaskItem>()
        .HasOne<Project>()
        .WithMany()
        .HasForeignKey(t => t.ProjectId)
        .OnDelete(DeleteBehavior.Cascade);

    // Task -> User (SET NULL)
    modelBuilder.Entity<TaskItem>()
        .HasOne<User>()
        .WithMany()
        .HasForeignKey(t => t.AssignedUserId)
        .OnDelete(DeleteBehavior.SetNull);

    // RefreshToken -> User (CASCADE)
    modelBuilder.Entity<RefreshToken>()
        .HasOne(rt => rt.User)
        .WithMany(u => u.RefreshTokens)
        .HasForeignKey(rt => rt.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // Unique index on RefreshToken.Token
    modelBuilder.Entity<RefreshToken>()
        .HasIndex(rt => rt.Token)
        .IsUnique();
}
```

## Common Queries

### Get User with Refresh Tokens
```csharp
var user = await _context.Users
    .Include(u => u.RefreshTokens)
    .FirstOrDefaultAsync(u => u.Email == email);
```

### Get Project with Members
```csharp
var project = await _context.Projects
    .Include(p => p.Members)
    .FirstOrDefaultAsync(p => p.Id == id);
```

### Get Tasks with Project and User
```csharp
var tasks = await _context.Tasks
    .Include(t => t.Project)
    .Include(t => t.AssignedUser)
    .Where(t => t.AssignedUserId == userId)
    .ToListAsync();
```

## Seeding Data (Development)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Seed admin user
    var adminId = Guid.NewGuid();
    modelBuilder.Entity<User>().HasData(new User
    {
        Id = adminId,
        Email = "admin@test.com",
        PasswordHash = "...", // Use PasswordHasher
        Role = "Admin"
    });
}
```

---

**See also:**
- [Getting Started](GETTING_STARTED.md) - Database setup
- [Troubleshooting](TROUBLESHOOTING.md) - Database issues
- [Documentation Overview](DOCUMENTATION_OVERVIEW.md) - All documentation
