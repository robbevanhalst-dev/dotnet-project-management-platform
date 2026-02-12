# Project Management Platform API

**Production-Ready ASP.NET Core Web API**  
Enterprise-grade backend met JWT authentication, refresh tokens, role-based authorization en clean architecture.

---

## ?? Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Security](#security)
4. [API Endpoints](#api-endpoints)
5. [Database](#database)
6. [Getting Started](#getting-started)
7. [Configuration](#configuration)
8. [Testing](#testing)
9. [Troubleshooting](#troubleshooting)

---

## Overview

### Features

- ? **JWT Authentication** + Refresh Tokens (7 days)
- ? **Role-Based Authorization** (User, Manager, Admin)
- ? **Pagination** (customizable page size)
- ? **Global Exception Handling**
- ? **Structured Logging** (Serilog - Console + File)
- ? **HTTP-Only Cookies** (XSS/CSRF protection)
- ? **Token Rotation** (anti-replay)
- ? **IP Address Tracking** (audit trail)

### Tech Stack

| Component | Technology |
|-----------|------------|
| Language | C# 13 |
| Framework | .NET 9 |
| API | ASP.NET Core Web API 9.0 |
| ORM | Entity Framework Core 9.0 |
| Database | SQL Server (LocalDB) |
| Logging | Serilog 8.0 |
| Testing | xUnit 2.9 |
| Documentation | Swagger/OpenAPI |

---

## Architecture

### Layered Structure

```
ProjectManagement.sln
??? Api          ? Controllers, Middleware, HTTP
??? Application  ? Services, DTOs, Business Logic
??? Domain       ? Entities, Constants (no dependencies)
??? Infrastructure ? Database, EF Core
??? Tests        ? Unit Tests
```

### Responsibilities

**API Layer:**
- REST Controllers (Auth, Projects, Tasks, Users)
- JWT Authentication
- Global Exception Middleware
- Cookie Management
- Serilog Logging

**Application Layer:**
- AuthService (login, register, refresh tokens)
- DTOs (LoginDto, RegisterDto, AuthenticateResponse)
- Pagination (PagedResult, PaginationParams)

**Domain Layer:**
- Entities (User, Project, Task, RefreshToken)
- Constants (Roles: Admin, Manager, User)

**Infrastructure Layer:**
- DbContext (EF Core)
- Migrations

---

## Security

### Authentication Flow

```
1. Login
   POST /api/auth/authenticate
   ? Access Token (JWT, 15 min)
   ? Refresh Token (random, 7 days, HTTP-only cookie)

2. API Requests
   Authorization: Bearer {accessToken}

3. Token Expires (after 15 min)
   POST /api/auth/refresh-token
   ? New Access Token
   ? New Refresh Token (old one revoked)

4. Logout
   POST /api/auth/logout
   ? Revoke refresh token
```

### Security Features

**JWT Access Tokens:**
- ?? Expires: 15 minutes
- ?? Contains: UserId, Email, Role
- ? Stateless validation

**Refresh Tokens:**
- ?? Expires: 7 days
- ?? Stored in database
- ?? Automatic rotation (prevents replay attacks)
- ?? HTTP-only cookies (XSS protection)
- ?? IP address tracking

**Authorization:**
```csharp
[Authorize]                        // Any authenticated user
[Authorize(Roles = "Admin")]       // Admin only
[Authorize(Roles = "Manager,Admin")] // Manager OR Admin
[Authorize(Policy = "AdminOnly")]  // Policy-based
```

### Role Hierarchy

```
Admin    ? Full access (user management, all operations)
Manager  ? Project/task management, view all tasks
User     ? View own projects/tasks, update profile
```

---

## API Endpoints

### Authentication

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/auth/register` | POST | None | Register user |
| `/api/auth/authenticate` | POST | None | Login with refresh token |
| `/api/auth/refresh-token` | POST | None | Refresh access token |
| `/api/auth/logout` | POST | User | Logout & revoke token |
| `/api/auth/refresh-tokens?userId={id}` | GET | Admin | View user's tokens |

### Projects

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/projects` | GET | User | Get paginated projects |
| `/api/projects/{id}` | GET | User | Get project by ID |
| `/api/projects` | POST | Manager, Admin | Create project |
| `/api/projects/{id}` | PUT | Manager, Admin | Update project |
| `/api/projects/{id}` | DELETE | Admin | Delete project |

### Tasks

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/tasks` | GET | User | Get my tasks (paginated) |
| `/api/tasks/{id}` | GET | User | Get task by ID |
| `/api/tasks` | POST | Manager, Admin | Create task |
| `/api/tasks/{id}` | PUT | User | Update task |
| `/api/tasks/{id}` | DELETE | Manager, Admin | Delete task |
| `/api/tasks/all` | GET | Admin | Get all tasks (admin) |

**Pagination Parameters:**
```
?pageNumber=1&pageSize=10&searchTerm=keyword
```

### Request/Response Examples

**Register:**
```http
POST /api/auth/register
{
  "email": "user@example.com",
  "password": "Password123",
  "role": "User"
}
```

**Authenticate:**
```http
POST /api/auth/authenticate
{
  "email": "user@example.com",
  "password": "Password123"
}

Response:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "rQP+5vXm...",
  "userId": "guid",
  "email": "user@example.com",
  "role": "User"
}
+ Cookie: refreshToken (HttpOnly, Secure, SameSite=Strict)
```

**Refresh Token:**
```http
POST /api/auth/refresh-token
Cookie: refreshToken=rQP+5vXm...

Response:
{
  "accessToken": "NEW_TOKEN...",
  "refreshToken": "NEW_REFRESH...",
  ...
}
```

**Get Projects (Paginated):**
```http
GET /api/projects?pageNumber=1&pageSize=10
Authorization: Bearer eyJhbGc...

Response:
{
  "pagination": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalCount": 50,
    "hasPrevious": false,
    "hasNext": true
  },
  "data": [...]
}
```

---

## Database

### Schema

```
User
??? Id (PK)
??? Email (unique)
??? PasswordHash
??? Role
??? RefreshTokens (1:N)

RefreshToken
??? Id (PK)
??? UserId (FK)
??? Token (unique)
??? ExpiresAt
??? CreatedAt
??? RevokedAt
??? CreatedByIp
??? RevokedByIp

Project
??? Id (PK)
??? Name
??? Description
??? CreatedAt

ProjectMember (Join Table)
??? UserId (PK, FK)
??? ProjectId (PK, FK)
??? JoinedAt

TaskItem
??? Id (PK)
??? ProjectId (FK)
??? AssignedUserId (FK, nullable)
??? Title
??? Status
??? DueDate
```

### Delete Behaviors

| Relationship | Behavior |
|--------------|----------|
| User ? RefreshTokens | CASCADE |
| User ? ProjectMembers | CASCADE |
| Project ? Tasks | CASCADE |
| User ? Tasks (assigned) | SET NULL |

---

## Getting Started

### Prerequisites

- Visual Studio 2022
- .NET 9 SDK
- SQL Server (LocalDB)

### Installation

**1. Clone**
```bash
git clone https://github.com/robbevanhalst-dev/dotnet-project-management-platform.git
cd dotnet-project-management-platform
```

**2. Restore**
```bash
dotnet restore
```

**3. Update Database**
```bash
dotnet ef database update \
  --project src/ProjectManagement.Infrastructure \
  --startup-project src/ProjectManagement.Api/ProjectManagement.Api
```

**4. Run**
```bash
dotnet run --project src/ProjectManagement.Api/ProjectManagement.Api
```

Or in Visual Studio: Set `ProjectManagement.Api` as startup project ? Press `F5`

**5. Access Swagger**
```
https://localhost:7264/swagger
```

### Quick Test

```bash
# Register
curl -X POST https://localhost:7264/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Admin123","role":"Admin"}'

# Authenticate
curl -X POST https://localhost:7264/api/auth/authenticate \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Admin123"}' \
  -c cookies.txt

# Get Projects (use accessToken from response)
curl https://localhost:7264/api/projects?pageNumber=1&pageSize=10 \
  -H "Authorization: Bearer {YOUR_ACCESS_TOKEN}"
```

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectManagementDb;Trusted_Connection=true"
  },
  "Jwt": {
    "Key": "SUPER_SECRET_KEY_Min32Chars!",
    "Issuer": "ProjectManagementApi",
    "Audience": "ProjectManagementApiUsers",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

### Environment Variables

```bash
# Override via environment variables
export Jwt__Key="YourProductionKey32CharsMin"
export Jwt__AccessTokenExpirationMinutes=30
export ConnectionStrings__DefaultConnection="Server=prod;..."
```

### Production Recommendations

**JWT Key:**
```powershell
# Generate secure key (PowerShell)
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

**CORS:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourfrontend.com")
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

---

## Testing

### Unit Tests (22 tests)

**Coverage:**
- ? Register (7 tests) - Valid data, duplicates, roles
- ? Login (4 tests) - Success, failure, validation
- ? JWT Tokens (7 tests) - Generation, claims, expiration
- ? Configuration (1 test) - Key validation
- ? Edge Cases (3 tests) - Concurrent operations

**Run Tests:**
```bash
dotnet test
dotnet test --verbosity detailed
```

**Results:**
```
Test summary:
- Total: 22
- Passed: 22 ?
- Failed: 0
- Duration: ~8 seconds
```

**Example Test:**
```csharp
[Fact]
public async Task RegisterAsync_WithValidData_ShouldCreateUser()
{
    var registerDto = new RegisterDto
    {
        Email = "test@example.com",
        Password = "Password123",
        Role = "User"
    };

    var result = await _authService.RegisterAsync(registerDto);

    result.Should().BeTrue();
    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == registerDto.Email);
    user.Should().NotBeNull();
}
```

---

## Troubleshooting

### Common Issues

**1. "Refresh token not provided"**

**Cause:** Cookie not sent, CORS blocks credentials

**Fix:**
```javascript
// Client: Add credentials
fetch('/api/auth/refresh-token', {
    credentials: 'include' // Important!
});

// Server: CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowCookies", policy =>
        policy.WithOrigins("https://frontend.com")
              .AllowCredentials()  // Required!
    );
});
```

**2. "Invalid or expired refresh token"**

**Cause:** Token expired (>7 days), revoked, or not in DB

**Fix:**
```sql
-- Check token in database
SELECT Token, ExpiresAt, RevokedAt 
FROM RefreshTokens 
WHERE Token = 'your-token';
```

Force re-authentication if expired.

**3. "Cookies not working"**

**Cause:** SameSite=Strict with cross-origin, HTTPS required

**Fix (Development):**
```csharp
var cookieOptions = new CookieOptions
{
    Secure = false,  // Allow HTTP
    SameSite = SameSiteMode.Lax  // Less strict
};
```

**Fix (Production):**
```csharp
var cookieOptions = new CookieOptions
{
    Secure = true,  // HTTPS only
    SameSite = SameSiteMode.None,  // Cross-origin
    Domain = ".yourcompany.com"
};
```

**4. "JWT token validation fails"**

**Cause:** Token expired, clock skew, invalid key

**Fix:**
```csharp
// Allow clock skew
options.TokenValidationParameters = new TokenValidationParameters
{
    ClockSkew = TimeSpan.FromMinutes(5)
};
```

**5. "Database migration fails"**

**Fix:**
```bash
# Reset database
dotnet ef database drop --force
dotnet ef database update

# Or check connection
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT name FROM sys.databases"
```

**6. "Logging not working"**

**Fix:**
```csharp
// Ensure Serilog is configured
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();  // Important!
```

---

## Client Implementation Example

### JavaScript/TypeScript

```typescript
class AuthService {
    private accessToken: string | null = null;

    async authenticate(email: string, password: string) {
        const response = await fetch('/api/auth/authenticate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password }),
            credentials: 'include' // Important!
        });

        const data = await response.json();
        this.accessToken = data.accessToken;
        return data;
    }

    async apiCall(endpoint: string, options: RequestInit = {}) {
        let response = await fetch(endpoint, {
            ...options,
            headers: {
                ...options.headers,
                'Authorization': `Bearer ${this.accessToken}`
            }
        });

        // Auto-refresh on 401
        if (response.status === 401) {
            const refreshed = await this.refreshToken();
            if (refreshed) {
                response = await fetch(endpoint, {
                    ...options,
                    headers: {
                        ...options.headers,
                        'Authorization': `Bearer ${this.accessToken}`
                    }
                });
            }
        }

        return response;
    }

    async refreshToken(): Promise<boolean> {
        try {
            const response = await fetch('/api/auth/refresh-token', {
                method: 'POST',
                credentials: 'include'
            });

            if (response.ok) {
                const data = await response.json();
                this.accessToken = data.accessToken;
                return true;
            }
        } catch {}
        return false;
    }

    async logout() {
        await fetch('/api/auth/logout', {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${this.accessToken}` },
            credentials: 'include'
        });
        this.accessToken = null;
    }
}
```

---

## Best Practices

### Security

**? Do:**
- Use short-lived access tokens (15 min)
- Rotate refresh tokens on each use
- Store refresh tokens in HTTP-only cookies
- Use HTTPS in production
- Log security events

**? Don't:**
- Store tokens in localStorage (XSS risk)
- Use long-lived access tokens (>1 hour)
- Log passwords or sensitive data
- Reuse refresh tokens

### Code Quality

**? Do:**
```csharp
// Structured logging
_logger.LogInformation("User {UserId} logged in", userId);

// Async/await
public async Task<IActionResult> GetProjectsAsync()
{
    var projects = await _context.Projects.ToListAsync();
    return Ok(projects);
}

// Dependency injection
public AuthController(IAuthService authService, ILogger logger)
{
    _authService = authService;
    _logger = logger;
}
```

**? Don't:**
```csharp
// String interpolation in logs
_logger.LogInformation($"User {userId} logged in");

// Blocking async
var projects = _context.Projects.ToListAsync().Result;

// Direct instantiation
var authService = new AuthService();
```

---

## License & Support

**License:** MIT License

**Author:** Robbe Vanhalst  
**GitHub:** https://github.com/robbevanhalst-dev  
**Repository:** https://github.com/robbevanhalst-dev/dotnet-project-management-platform

**Additional Documentation:**
- RBAC_GUIDE.md - Detailed Role-Based Access Control guide

---

**Last Updated:** January 2025  
**Version:** 1.0  
**Status:** ? Production Ready

**? If this helped you, please star the repository!**

