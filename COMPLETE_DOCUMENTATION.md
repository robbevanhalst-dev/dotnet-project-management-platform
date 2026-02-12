# Project Management Platform API - Complete Documentation

**Production-Ready ASP.NET Core Web API**  
Enterprise-grade backend system met JWT authentication, refresh tokens, role-based authorization, structured logging en clean architecture.

---

## ?? Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Security Features](#security-features)
4. [Authentication & Authorization](#authentication--authorization)
5. [Refresh Tokens](#refresh-tokens)
6. [API Endpoints](#api-endpoints)
7. [Logging & Monitoring](#logging--monitoring)
8. [Exception Handling](#exception-handling)
9. [Pagination](#pagination)
10. [Database Design](#database-design)
11. [Technologies](#technologies)
12. [Getting Started](#getting-started)
13. [Testing](#testing)
14. [Client Implementation](#client-implementation)
15. [Configuration](#configuration)
16. [Best Practices](#best-practices)
17. [Troubleshooting](#troubleshooting)

---

## Project Overview

### ?? Project Goal

Simulatie van een **real-world SaaS-style backend system** met professionele backend architectuur en security best practices.

**Key Features:**
- ? User registration en authentication met **refresh tokens**
- ? **Role-based authorization** (User, Manager, Admin)
- ? Project creation en management met **pagination**
- ? Task assignment en tracking
- ? Comprehensive **audit logging**
- ? **Global exception handling**
- ? **Structured logging** met Serilog
- ? **HTTP-only cookies** voor security
- ? **Token rotation** anti-replay mechanism

### ? Highlights

Dit project demonstreert:
- Enterprise-grade authentication system
- Clean Architecture principles
- Production-ready security
- Comprehensive error handling
- Professional logging & monitoring
- Full API documentation
- Unit test coverage

---

## Architecture

### ??? Layered Architecture

```
ProjectManagement.sln  
??? ProjectManagement.Api           ? Controllers, Middleware, HTTP
??? ProjectManagement.Application   ? Business Logic, Services, DTOs
??? ProjectManagement.Domain        ? Entities, Constants, Core Logic
??? ProjectManagement.Infrastructure ? Database, EF Core, Persistence
??? ProjectManagement.Tests         ? Unit Tests, Integration Tests
```

### Layer Responsibilities

#### ?? **API Layer** (`ProjectManagement.Api`)

**Responsibilities:**
- RESTful Controllers
- JWT Authentication & Authorization
- Global Exception Middleware
- HTTP-only Cookie Management
- Serilog Request Logging
- Swagger/OpenAPI Documentation

**Key Files:**
```
Controllers/
??? AuthController.cs      ? Authentication & Refresh Tokens
??? ProjectsController.cs  ? Project Management
??? TasksController.cs     ? Task Management
??? UsersController.cs     ? User Management

Middleware/
??? GlobalExceptionMiddleware.cs ? Centralized Error Handling

Program.cs ? Application Configuration
```

#### ?? **Application Layer** (`ProjectManagement.Application`)

**Responsibilities:**
- Business Logic Implementation
- Authentication Services
- DTO Definitions & Validation
- Pagination Logic
- Service Interfaces

**Key Files:**
```
Services/
??? AuthService.cs ? Authentication, Token Generation

Interfaces/
??? IAuthService.cs ? Service Contracts

DTOs/
??? LoginDto.cs
??? RegisterDto.cs
??? AuthenticateResponse.cs
??? RefreshTokenRequest.cs
??? RevokeTokenRequest.cs

Common/
??? PagedResult.cs
??? PaginationParams.cs
??? PaginationExtensions.cs
```

#### ?? **Domain Layer** (`ProjectManagement.Domain`)

**Responsibilities:**
- Core Business Entities
- Domain Constants
- Business Rules
- **No external dependencies**

**Entities:**
```
Entities/
??? User.cs
??? Project.cs
??? ProjectMember.cs
??? TaskItem.cs
??? RefreshToken.cs

Constants/
??? Roles.cs ? Role Definitions
```

**Entity Relationships:**
```
User ??< RefreshTokens      (1:N)
User ??< ProjectMembers      (1:N)
Project ??< ProjectMembers   (1:N)
Project ??< Tasks            (1:N)
User ??< Tasks (Assigned)    (1:N, nullable)
```

#### ?? **Infrastructure Layer** (`ProjectManagement.Infrastructure`)

**Responsibilities:**
- EF Core DbContext
- Database Migrations
- Data Persistence
- Configuration

**Key Files:**
```
Data/
??? ProjectManagementDbContext.cs

Migrations/
??? [Timestamped migrations]
```

---

## Security Features

### ?? Multi-Layer Security

#### 1. **Authentication System**

**JWT Access Tokens:**
- ?? Short-lived: **15 minutes**
- ?? HS256 algorithm
- ?? Contains: UserId, Email, Role claims
- ? Stateless validation

**Refresh Tokens:**
- ?? Long-lived: **7 days**
- ?? Cryptographically random (64 bytes, Base64)
- ?? Stored in database
- ?? Automatic rotation on use
- ?? HTTP-only cookies
- ?? IP address tracking

#### 2. **Authorization System**

**Role-Based Access Control (RBAC):**
```csharp
// Roles defined in Domain/Constants/Roles.cs
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";
}
```

**Authorization Policies:**
```csharp
// Configured in Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("ManagerOrAdmin", policy => 
        policy.RequireRole("Manager", "Admin"));
    
    options.AddPolicy("RequireEmail", policy => 
        policy.RequireClaim(ClaimTypes.Email));
});
```

**Usage Examples:**
```csharp
// Role-based
[Authorize(Roles = "Admin")]
[Authorize(Roles = "Manager,Admin")]

// Policy-based
[Authorize(Policy = "AdminOnly")]
[Authorize(Policy = "ManagerOrAdmin")]

// Basic auth
[Authorize]

// Public
[AllowAnonymous]
```

#### 3. **Cookie Security**

```csharp
var cookieOptions = new CookieOptions
{
    HttpOnly = true,           // XSS Protection
    Secure = true,             // HTTPS only
    SameSite = SameSiteMode.Strict,  // CSRF Protection
    Expires = DateTime.UtcNow.AddDays(7)
};
```

**Security Benefits:**
- ? **HttpOnly**: Niet toegankelijk via JavaScript (XSS protection)
- ? **Secure**: Alleen over HTTPS verzonden
- ? **SameSite=Strict**: CSRF attack prevention
- ? **Expiration**: Automatic cleanup

#### 4. **Password Security**

```csharp
// Using ASP.NET Core Identity PasswordHasher
var passwordHasher = new PasswordHasher<User>();
user.PasswordHash = passwordHasher.HashPassword(user, password);

// Verification
var result = passwordHasher.VerifyHashedPassword(
    user, 
    user.PasswordHash, 
    providedPassword
);
```

**Features:**
- ? PBKDF2 algorithm
- ? Automatic salt generation
- ? Configurable iterations
- ? Industry standard

#### 5. **Token Rotation**

**Anti-Replay Mechanism:**

Elke refresh token gebruik:
1. Valideer oude token
2. Genereer nieuwe access token
3. Genereer nieuwe refresh token
4. **Revoke** oude refresh token
5. Link via `ReplacedByToken`
6. Return nieuwe tokens

**Benefits:**
- ? Prevents token replay attacks
- ? Audit trail via linked tokens
- ? Detectable suspicious activity

---

## Authentication & Authorization

### ?? Authentication Flow

#### **Basic Flow (Legacy)**

```
1. User Registration
   POST /api/auth/register
   ? User created with default role

2. User Login
   POST /api/auth/login
   ? JWT access token (15 min)

3. API Requests
   Authorization: Bearer {token}
   ? Validated via middleware

4. Token Expires
   ? User must login again
```

#### **Refresh Token Flow (Recommended)**

```
????????????
?  Client  ?
????????????
     ?
     ? 1. Register/Login
     ? POST /api/auth/authenticate
     ?????????????????????????????????>
     ?
     ? 2. Response
     ? ?? Access Token (15 min)
     ? ?? Refresh Token (7 days)
     ? ?? Cookie: refreshToken
     ?<????????????????????????????????
     ?
     ? 3. API Calls
     ? Authorization: Bearer {access}
     ?????????????????????????????????>
     ?
     ? 4. Access Token Expires
     ? (after 15 minutes)
     ?
     ? 5. Refresh Request
     ? POST /api/auth/refresh-token
     ? Cookie: refreshToken
     ?????????????????????????????????>
     ?
     ? 6. New Tokens
     ? ?? New Access Token
     ? ?? New Refresh Token (rotated)
     ? ?? Old refresh token revoked
     ?<????????????????????????????????
     ?
     ? 7. Continue API Calls
     ? Authorization: Bearer {new_access}
     ?????????????????????????????????>
     ?
     ? 8. Logout
     ? POST /api/auth/logout
     ? ? Revoke refresh token
     ?????????????????????????????????>
```

### ?? Role-Based Authorization

#### **Role Hierarchy**

```
Admin (Highest)
  ?? Full system access
  ?? User management
  ?? Role assignment
  ?? All Manager + User permissions
     
Manager
  ?? Project management
  ?? Task creation/assignment
  ?? View all tasks
  ?? All User permissions
     
User (Lowest)
  ?? View own projects
  ?? View assigned tasks
  ?? Update own profile
  ?? Basic operations
```

#### **Permission Matrix**

| Endpoint | User | Manager | Admin |
|----------|------|---------|-------|
| GET /api/projects | ? (own) | ? (all) | ? (all) |
| POST /api/projects | ? | ? | ? |
| DELETE /api/projects/{id} | ? | ? | ? |
| POST /api/tasks | ? | ? | ? |
| GET /api/tasks/all | ? | ? | ? |
| PUT /api/users/{id}/role | ? | ? | ? |
| GET /api/auth/refresh-tokens | ? | ? | ? |

#### **Implementation Examples**

**Controller Level:**
```csharp
[Authorize] // All endpoints require authentication
public class ProjectsController : ControllerBase
{
    [HttpGet] // Any authenticated user
    public IActionResult GetProjects() { }
    
    [Authorize(Roles = "Manager,Admin")] // Manager OR Admin
    public IActionResult CreateProject() { }
    
    [Authorize(Policy = "AdminOnly")] // Admin only
    public IActionResult DeleteProject() { }
}
```

**Accessing User Claims:**
```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = User.FindFirst(ClaimTypes.Email)?.Value;
var role = User.FindFirst(ClaimTypes.Role)?.Value;
var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
```

---

## Refresh Tokens

### ?? What are Refresh Tokens?

**Two-Token System:**

| Token Type | Access Token (JWT) | Refresh Token |
|------------|-------------------|---------------|
| **Lifespan** | 15 minutes | 7 days |
| **Purpose** | API access | Get new access token |
| **Storage** | Client memory | HTTP-only cookie + DB |
| **Size** | ~800 bytes | 88 bytes (Base64) |
| **Security** | Stateless | Stateful + Revocable |

### ?? Benefits

1. **?? Security**
   - Access tokens expire quickly (15 min)
   - Limited damage if stolen
   - Forced token refresh

2. **?? User Experience**
   - Stay logged in for 7 days
   - Seamless token renewal
   - No constant re-login

3. **?? Revocability**
   - Individual token revocation
   - Logout all devices
   - Security breach response

4. **?? Auditing**
   - Track token usage
   - IP address logging
   - Suspicious activity detection

### ??? Database Schema

```sql
CREATE TABLE RefreshTokens (
    Id              UNIQUEIDENTIFIER PRIMARY KEY,
    UserId          UNIQUEIDENTIFIER NOT NULL,
    Token           NVARCHAR(MAX) NOT NULL UNIQUE,
    ExpiresAt       DATETIME2 NOT NULL,
    CreatedAt       DATETIME2 NOT NULL,
    RevokedAt       DATETIME2 NULL,
    ReplacedByToken NVARCHAR(MAX) NULL,
    RevokedByIp     NVARCHAR(50) NULL,
    CreatedByIp     NVARCHAR(50) NOT NULL,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX IX_RefreshTokens_Token (Token)
)
```

### ?? Implementation

#### **Entity Definition**

```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? RevokedByIp { get; set; }
    public string CreatedByIp { get; set; }
    
    // Computed properties
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
    
    // Navigation
    public User User { get; set; }
}
```

#### **Token Generation**

```csharp
private RefreshToken GenerateRefreshToken(string ipAddress)
{
    using var rng = RandomNumberGenerator.Create();
    var randomBytes = new byte[64];
    rng.GetBytes(randomBytes);

    return new RefreshToken
    {
        Token = Convert.ToBase64String(randomBytes),
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow,
        CreatedByIp = ipAddress
    };
}
```

#### **Token Rotation**

```csharp
private RefreshToken RotateRefreshToken(
    RefreshToken refreshToken, 
    string ipAddress)
{
    var newToken = GenerateRefreshToken(ipAddress);
    
    // Revoke old token
    refreshToken.RevokedAt = DateTime.UtcNow;
    refreshToken.RevokedByIp = ipAddress;
    refreshToken.ReplacedByToken = newToken.Token;
    
    return newToken;
}
```

#### **Automatic Cleanup**

```csharp
private void RemoveOldRefreshTokens(User user)
{
    // Keep only 5 most recent tokens
    var tokensToRemove = user.RefreshTokens
        .OrderByDescending(x => x.CreatedAt)
        .Skip(5)
        .ToList();
    
    foreach (var token in tokensToRemove)
    {
        user.RefreshTokens.Remove(token);
    }
}
```

### ?? Security Features

#### **1. Token Rotation**

**How it works:**
```
User requests refresh
  ?
Validate old refresh token
  ?
Generate NEW access token (15 min)
Generate NEW refresh token (7 days)
  ?
REVOKE old refresh token
Link old ? new via ReplacedByToken
  ?
Return new tokens
Delete old refresh token cookie
Set new refresh token cookie
```

**Benefits:**
- ? Prevents replay attacks
- ? Detectable token theft
- ? Audit trail

#### **2. HTTP-Only Cookies**

```csharp
private void SetTokenCookie(string token)
{
    Response.Cookies.Append("refreshToken", token, new CookieOptions
    {
        HttpOnly = true,      // No JavaScript access
        Secure = true,        // HTTPS only
        SameSite = SameSiteMode.Strict,  // CSRF protection
        Expires = DateTime.UtcNow.AddDays(7)
    });
}
```

**Protection against:**
- ? XSS attacks (JavaScript can't read)
- ? CSRF attacks (SameSite=Strict)
- ? Man-in-the-middle (Secure flag)

#### **3. IP Address Tracking**

```csharp
private string GetIpAddress()
{
    if (Request.Headers.ContainsKey("X-Forwarded-For"))
        return Request.Headers["X-Forwarded-For"].ToString()
            .Split(',')[0].Trim();
    
    return HttpContext.Connection.RemoteIpAddress?.ToString() 
        ?? "unknown";
}
```

**Use cases:**
- ?? Audit logging
- ?? Suspicious activity detection
- ?? Geographic restrictions
- ?? Usage analytics

---

## API Endpoints

### ?? Complete Endpoint Reference

#### **Authentication Endpoints**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | None | Register new user |
| POST | `/api/auth/login` | None | Login (access token only - legacy) |
| POST | `/api/auth/authenticate` | None | Login with refresh token |
| POST | `/api/auth/refresh-token` | None | Refresh access token |
| POST | `/api/auth/revoke-token` | User | Revoke specific refresh token |
| POST | `/api/auth/logout` | User | Logout & revoke token |
| GET | `/api/auth/refresh-tokens?userId={id}` | Admin | View user's refresh tokens |

#### **Project Endpoints**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/projects` | User | Get paginated projects |
| GET | `/api/projects/{id}` | User | Get project by ID |
| POST | `/api/projects` | Manager, Admin | Create new project |
| PUT | `/api/projects/{id}` | Manager, Admin | Update project |
| DELETE | `/api/projects/{id}` | Admin | Delete project |
| GET | `/api/projects/public` | None | Public projects (paginated) |
| GET | `/api/projects/admin-only` | Admin | Admin-only data |
| GET | `/api/projects/manager-dashboard` | Manager, Admin | Manager dashboard |
| GET | `/api/projects/test-error` | None | Test exception middleware |

**Query Parameters (GET endpoints):**
```
?pageNumber=1          // Page number (default: 1)
&pageSize=10           // Items per page (default: 10, max: 100)
&searchTerm=webapp     // Search filter (optional)
&sortBy=name           // Sort field (optional)
&sortDescending=true   // Sort direction (optional)
```

#### **Task Endpoints**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/tasks/status` | None | API health check |
| GET | `/api/tasks` | User | Get my tasks (paginated) |
| GET | `/api/tasks/{id}` | User | Get task by ID |
| POST | `/api/tasks` | Manager, Admin | Create task |
| PUT | `/api/tasks/{id}` | User | Update task |
| DELETE | `/api/tasks/{id}` | Manager, Admin | Delete task |
| POST | `/api/tasks/{id}/assign` | Manager, Admin | Assign task to user |
| GET | `/api/tasks/all` | Admin | Get all tasks (paginated) |

#### **User Endpoints**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/users/profile` | User | Get my profile |
| PUT | `/api/users/profile` | User | Update my profile |
| GET | `/api/users` | Admin | Get all users |
| GET | `/api/users/{id}` | Manager, Admin | Get user by ID |
| PUT | `/api/users/{id}/role` | Admin | Update user role |
| DELETE | `/api/users/{id}` | Admin | Delete user |
| GET | `/api/users/managers` | Manager, Admin | Get all managers |

### ?? Request/Response Examples

#### **1. Register User**

**Request:**
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123",
  "role": "User"
}
```

**Response (200 OK):**
```json
"User registered"
```

**Response (400 Bad Request):**
```json
"User already exists"
```

#### **2. Authenticate (with Refresh Token)**

**Request:**
```http
POST /api/auth/authenticate
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password123"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "rQP+5vXm8F7k2w3N9B1hC6sE...",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "role": "User"
}
```

**Cookie Set:**
```
Set-Cookie: refreshToken=rQP+5vXm8F7k2w3N9B1hC6sE...; 
            Path=/; 
            HttpOnly; 
            Secure; 
            SameSite=Strict; 
            Expires=Wed, 15 Jan 2025 10:00:00 GMT
```

#### **3. Refresh Access Token**

**Request (with cookie):**
```http
POST /api/auth/refresh-token
Cookie: refreshToken=rQP+5vXm8F7k2w3N9B1hC6sE...
```

**OR Request (with body):**
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "rQP+5vXm8F7k2w3N9B1hC6sE..."
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "NEW_TOKEN_HERE...",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "role": "User"
}
```

**Response (401 Unauthorized):**
```json
{
  "message": "Invalid or expired refresh token"
}
```

#### **4. Get Projects (Paginated)**

**Request:**
```http
GET /api/projects?pageNumber=1&pageSize=10&searchTerm=webapp
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userEmail": "user@example.com",
  "userRole": "User",
  "pagination": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalCount": 50,
    "hasPrevious": false,
    "hasNext": true
  },
  "data": [
    { "id": 1, "name": "Project 1", "status": "Active" },
    { "id": 2, "name": "Project 2", "status": "Completed" }
  ]
}
```

#### **5. Logout**

**Request:**
```http
POST /api/auth/logout
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Cookie: refreshToken=rQP+5vXm8F7k2w3N9B1hC6sE...
```

**Response (200 OK):**
```json
{
  "message": "Logged out successfully"
}
```

**Cookie Deleted:**
```
Set-Cookie: refreshToken=; 
            Path=/; 
            Expires=Thu, 01 Jan 1970 00:00:00 GMT
```

---

## Logging & Monitoring

### ?? Serilog Structured Logging

#### **Configuration**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithThreadId" ]
  }
}
```

#### **Log Levels**

| Level | When to Use | Example |
|-------|-------------|---------|
| **Trace** | Very detailed debug info | `_logger.LogTrace("Query: {Sql}", sql)` |
| **Debug** | Development debugging | `_logger.LogDebug("Cache miss for {Key}", key)` |
| **Information** | General events | `_logger.LogInformation("User {UserId} logged in", userId)` |
| **Warning** | Unexpected but handled | `_logger.LogWarning("Retry attempt {Count}", retryCount)` |
| **Error** | Errors that can be handled | `_logger.LogError(ex, "Failed to process {Item}", item)` |
| **Critical** | Fatal application errors | `_logger.LogCritical(ex, "Database unavailable")` |

#### **Structured Logging Examples**

```csharp
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    // ? Don't: String interpolation
    _logger.LogInformation($"User {userId} logged in");

    // ? Do: Structured logging
    _logger.LogInformation("User {UserId} logged in", userId);
    
    // ? Multiple properties
    _logger.LogInformation(
        "User {UserId} authenticated successfully: {Email}, Role: {Role}",
        user.Id, user.Email, user.Role
    );
    
    // ? With exception
    _logger.LogError(
        ex, 
        "Failed to authenticate user {Email}",
        dto.Email
    );
}
```

#### **HTTP Request Logging**

Automatically logs all HTTP requests:

```csharp
// In Program.cs
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = 
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"]);
    };
});
```

**Output:**
```
[10:15:30 INF] HTTP GET /api/projects responded 200 in 45.23 ms
[10:15:31 INF] HTTP POST /api/auth/login responded 200 in 123.45 ms
[10:15:32 WRN] HTTP GET /api/projects/999 responded 404 in 12.34 ms
```

#### **Log Output Locations**

**Console (Development):**
```
[10:15:30 INF] Starting ProjectManagement API {"ThreadId": 1}
[10:15:31 INF] User 3fa85f64... logged in {"ThreadId": 4}
[10:15:32 INF] HTTP GET /api/projects responded 200 in 45.23 ms
```

**Files (Production):**
```
Logs/
??? log-20250108.txt   (Today's logs)
??? log-20250107.txt   (Yesterday)
??? log-20250106.txt
??? ... (30 days retention)
```

**File Format:**
```
2025-01-08 10:15:30.123 +01:00 [INF] User 3fa85f64-5717-4562-b3fc-2c963f66afa6 logged in {"ThreadId": 4}
2025-01-08 10:15:31.456 +01:00 [INF] HTTP GET /api/projects responded 200 in 45.23 ms
2025-01-08 10:15:32.789 +01:00 [ERR] Failed to authenticate user test@example.com
System.InvalidOperationException: Invalid credentials
   at AuthService.LoginAsync(LoginDto dto) in C:\...\AuthService.cs:line 123
```

#### **Key Log Events**

**Authentication:**
```
[INF] Registration attempt for email: user@example.com
[INF] User registered successfully: user@example.com
[INF] Authentication attempt for email: user@example.com
[INF] User authenticated successfully: user@example.com, Role: User
[WRN] Login failed for email: user@example.com
```

**Refresh Tokens:**
```
[INF] Refresh token attempt
[INF] Token refreshed successfully for user: 3fa85f64...
[WRN] Invalid or expired refresh token
[INF] Token revoked successfully
```

**Authorization:**
```
[WRN] User user@example.com attempted to access admin-only endpoint
[INF] Manager manager@example.com is accessing manager dashboard
```

**Errors:**
```
[ERR] An unhandled exception occurred: Invalid operation
[ERR] Error Response: StatusCode=400, Message=Invalid operation
```

---

## Exception Handling

### ??? Global Exception Middleware

#### **Implementation**

Located in: `src/ProjectManagement.Api/Middleware/GlobalExceptionMiddleware.cs`

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

#### **Exception Type Mapping**

| Exception Type | HTTP Status | Response |
|---------------|-------------|----------|
| `ApplicationException` | 400 Bad Request | Custom message |
| `KeyNotFoundException` | 404 Not Found | Resource not found |
| `UnauthorizedAccessException` | 401 Unauthorized | Not authorized |
| `InvalidOperationException` | 400 Bad Request | Invalid operation |
| **All Others** | 500 Internal Server Error | Generic message (prod) |

#### **Error Response Format**

**Development:**
```json
{
  "success": false,
  "statusCode": 400,
  "message": "JWT Key must be at least 32 characters long",
  "details": "Stack trace here..."
}
```

**Production:**
```json
{
  "success": false,
  "statusCode": 500,
  "message": "An internal server error occurred. Please try again later."
}
```

#### **Usage Example**

**Test Endpoint:**
```csharp
[HttpGet("test-error")]
[AllowAnonymous]
public IActionResult TestError()
{
    _logger.LogWarning("Test error endpoint called");
    throw new InvalidOperationException(
        "This is a test exception to demonstrate global error handling"
    );
}
```

**Request:**
```http
GET /api/projects/test-error
```

**Response (400 Bad Request):**
```json
{
  "success": false,
  "statusCode": 400,
  "message": "This is a test exception to demonstrate global error handling",
  "details": "   at ProjectsController.TestError() in ..."
}
```

**Log Output:**
```
[WRN] Test error endpoint called
[ERR] An unhandled exception occurred: This is a test exception...
[ERR] Error Response: StatusCode=400, Message=This is a test exception...
```

#### **Custom Exceptions**

```csharp
// Domain/Exceptions/ResourceNotFoundException.cs
public class ResourceNotFoundException : KeyNotFoundException
{
    public ResourceNotFoundException(string resource, Guid id)
        : base($"{resource} with ID {id} was not found")
    {
    }
}

// Usage
var project = await _context.Projects.FindAsync(id);
if (project == null)
    throw new ResourceNotFoundException("Project", id);
```

**Response:**
```json
{
  "success": false,
  "statusCode": 404,
  "message": "Project with ID 3fa85f64-5717-4562-b3fc-2c963f66afa6 was not found"
}
```

---

## Pagination

### ?? Pagination System

#### **Components**

**1. PaginationParams (Request)**
```csharp
public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }

    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public string? SearchTerm { get; set; }
}
```

**2. PagedResult (Response)**
```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}
```

**3. Extensions**
```csharp
public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
    this IQueryable<T> query,
    int pageNumber,
    int pageSize)
{
    var count = await query.CountAsync();
    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<T>(items, count, pageNumber, pageSize);
}
```

#### **Usage Examples**

**Basic Pagination:**
```csharp
[HttpGet]
public IActionResult GetProjects([FromQuery] PaginationParams paginationParams)
{
    var allProjects = Enumerable.Range(1, 50)
        .Select(i => new { Id = i, Name = $"Project {i}" });
    
    var pagedResult = allProjects.ToPagedResult(
        paginationParams.PageNumber,
        paginationParams.PageSize
    );
    
    return Ok(new
    {
        pagination = new
        {
            pagedResult.PageNumber,
            pagedResult.PageSize,
            pagedResult.TotalPages,
            pagedResult.TotalCount,
            pagedResult.HasPrevious,
            pagedResult.HasNext
        },
        data = pagedResult.Items
    });
}
```

**With Search:**
```csharp
[HttpGet]
public IActionResult GetProjects([FromQuery] PaginationParams paginationParams)
{
    var query = _context.Projects.AsQueryable();
    
    // Apply search filter
    if (!string.IsNullOrWhiteSpace(paginationParams.SearchTerm))
    {
        query = query.Where(p => 
            p.Name.Contains(paginationParams.SearchTerm)
        );
    }
    
    var pagedResult = query.ToPagedResult(
        paginationParams.PageNumber,
        paginationParams.PageSize
    );
    
    return Ok(pagedResult);
}
```

**Async with EF Core:**
```csharp
[HttpGet]
public async Task<IActionResult> GetProjectsAsync(
    [FromQuery] PaginationParams paginationParams)
{
    var query = _context.Projects
        .Include(p => p.Members)
        .AsQueryable();
    
    var pagedResult = await query.ToPagedResultAsync(
        paginationParams.PageNumber,
        paginationParams.PageSize
    );
    
    return Ok(pagedResult);
}
```

#### **API Requests**

**Basic:**
```http
GET /api/projects?pageNumber=1&pageSize=10
```

**With Search:**
```http
GET /api/projects?pageNumber=1&pageSize=20&searchTerm=webapp
```

**With Sorting (custom implementation):**
```http
GET /api/projects?pageNumber=2&pageSize=15&sortBy=name&sortDescending=true
```

#### **Response Format**

```json
{
  "pagination": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalCount": 50,
    "hasPrevious": false,
    "hasNext": true
  },
  "data": [
    { "id": 1, "name": "Project 1", "status": "Active" },
    { "id": 2, "name": "Project 2", "status": "Completed" },
    ...
  ]
}
```

---

## Database Design

### ??? Entity Relationship Diagram

```
????????????????
?     User     ?
????????????????
? Id (PK)      ?
? Email        ?
? PasswordHash ?
? Role         ?
????????????????
       ?
       ? 1:N
       ???????????????????
       ?                 ?
       ?                 ?
????????????????   ????????????????
?RefreshToken  ?   ?ProjectMember ?
????????????????   ????????????????
? Id (PK)      ?   ? UserId (PK)  ?
? UserId (FK)  ?   ? ProjectId(PK)?
? Token        ?   ? JoinedAt     ?
? ExpiresAt    ?   ????????????????
? CreatedAt    ?          ?
? RevokedAt    ?          ? N:1
? CreatedByIp  ?          ?
????????????????          ?
                   ????????????????
       ?????????????   Project    ?
       ?           ????????????????
       ?           ? Id (PK)      ?
       ?           ? Name         ?
       ?           ? Description  ?
       ?           ? CreatedAt    ?
       ?           ????????????????
       ?                  ?
       ?                  ? 1:N
       ?                  ?
       ?                  ?
       ?           ????????????????
       ???????????>?   TaskItem   ?
                   ????????????????
                   ? Id (PK)      ?
                   ? ProjectId(FK)?
                   ? AssignedUser ?
                   ?   Id (FK)    ?
                   ? Title        ?
                   ? Description  ?
                   ? Status       ?
                   ? Priority     ?
                   ? DueDate      ?
                   ????????????????
```

### ?? Table Definitions

#### **Users Table**

```sql
CREATE TABLE Users (
    Id              UNIQUEIDENTIFIER PRIMARY KEY,
    Email           NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(MAX) NOT NULL,
    Role            NVARCHAR(50) NOT NULL DEFAULT 'User'
)
```

**Indexes:**
- Primary Key on `Id`
- Unique Index on `Email`

#### **RefreshTokens Table**

```sql
CREATE TABLE RefreshTokens (
    Id              UNIQUEIDENTIFIER PRIMARY KEY,
    UserId          UNIQUEIDENTIFIER NOT NULL,
    Token           NVARCHAR(MAX) NOT NULL,
    ExpiresAt       DATETIME2 NOT NULL,
    CreatedAt       DATETIME2 NOT NULL,
    RevokedAt       DATETIME2 NULL,
    ReplacedByToken NVARCHAR(MAX) NULL,
    RevokedByIp     NVARCHAR(50) NULL,
    CreatedByIp     NVARCHAR(50) NOT NULL,
    
    CONSTRAINT FK_RefreshTokens_Users 
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
)

CREATE UNIQUE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token)
```

**Relationships:**
- Many RefreshTokens ? One User (CASCADE delete)

#### **Projects Table**

```sql
CREATE TABLE Projects (
    Id          UNIQUEIDENTIFIER PRIMARY KEY,
    Name        NVARCHAR(256) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE()
)
```

#### **ProjectMembers Table (Join Table)**

```sql
CREATE TABLE ProjectMembers (
    UserId      UNIQUEIDENTIFIER NOT NULL,
    ProjectId   UNIQUEIDENTIFIER NOT NULL,
    JoinedAt    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT PK_ProjectMembers PRIMARY KEY (UserId, ProjectId),
    CONSTRAINT FK_ProjectMembers_Users 
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProjectMembers_Projects 
        FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
)
```

**Composite Primary Key:** `(UserId, ProjectId)`

#### **Tasks Table**

```sql
CREATE TABLE Tasks (
    Id              UNIQUEIDENTIFIER PRIMARY KEY,
    ProjectId       UNIQUEIDENTIFIER NOT NULL,
    AssignedUserId  UNIQUEIDENTIFIER NULL,
    Title           NVARCHAR(256) NOT NULL,
    Description     NVARCHAR(MAX) NULL,
    Status          NVARCHAR(50) NOT NULL,
    Priority        NVARCHAR(50) NULL,
    DueDate         DATETIME2 NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Tasks_Projects 
        FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Tasks_Users 
        FOREIGN KEY (AssignedUserId) REFERENCES Users(Id) ON DELETE SET NULL
)
```

**Relationships:**
- Many Tasks ? One Project (CASCADE delete)
- Many Tasks ? One User (SET NULL on delete)

### ?? EF Core Configuration

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ProjectMember composite key
    modelBuilder.Entity<ProjectMember>()
        .HasKey(pm => new { pm.UserId, pm.ProjectId });

    // Task -> Project relationship
    modelBuilder.Entity<TaskItem>()
        .HasOne<Project>()
        .WithMany()
        .HasForeignKey(t => t.ProjectId)
        .OnDelete(DeleteBehavior.Cascade);

    // Task -> User relationship (nullable)
    modelBuilder.Entity<TaskItem>()
        .HasOne<User>()
        .WithMany()
        .HasForeignKey(t => t.AssignedUserId)
        .OnDelete(DeleteBehavior.SetNull);

    // RefreshToken -> User relationship
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

### ?? Delete Behaviors

| Relationship | On Parent Delete | Behavior |
|--------------|------------------|----------|
| User ? RefreshTokens | User deleted | **CASCADE** - Delete all tokens |
| User ? ProjectMembers | User deleted | **CASCADE** - Remove memberships |
| Project ? Tasks | Project deleted | **CASCADE** - Delete all tasks |
| User ? Tasks (Assigned) | User deleted | **SET NULL** - Unassign tasks |

---

## Technologies

### ??? Technology Stack

#### **Core Framework**

| Technology | Version | Purpose |
|------------|---------|---------|
| **C#** | 13.0 | Programming Language |
| **.NET** | 9.0 | Runtime Framework |
| **ASP.NET Core Web API** | 9.0 | API Framework |
| **Entity Framework Core** | 9.0.0 | ORM |
| **SQL Server** | LocalDB | Database (dev) |

#### **Security & Authentication**

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.0.0 | JWT Authentication |
| `Microsoft.IdentityModel.Tokens` | - | Token Validation |
| `System.IdentityModel.Tokens.Jwt` | - | JWT Handling |
| `Microsoft.AspNetCore.Identity` | - | Password Hashing |

#### **Logging & Monitoring**

| Package | Version | Purpose |
|---------|---------|---------|
| `Serilog.AspNetCore` | 8.0.3 | Structured Logging |
| `Serilog.Sinks.Console` | 6.0.0 | Console Output |
| `Serilog.Sinks.File` | 6.0.0 | File Output |
| `Serilog.Enrichers.Environment` | 3.0.1 | Environment Info |
| `Serilog.Enrichers.Thread` | 4.0.0 | Thread ID |

#### **API Documentation**

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.OpenApi` | 9.0.11 | OpenAPI Support |
| `Swashbuckle.AspNetCore` | 7.2.0 | Swagger UI |

#### **Testing**

| Package | Version | Purpose |
|---------|---------|---------|
| `xUnit` | 2.9.2 | Test Framework |
| `xunit.runner.visualstudio` | 2.8.2 | VS Test Runner |
| `FluentAssertions` | 7.0.0 | Assertions |
| `Moq` | 4.20.72 | Mocking |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.0.0 | In-Memory DB |
| `Microsoft.Extensions.Configuration` | 9.0.0 | Config Testing |

#### **Development Tools**

| Tool | Purpose |
|------|---------|
| Visual Studio 2022 | IDE |
| SQL Server Management Studio | Database Management |
| Postman | API Testing |
| Git | Version Control |
| Swagger UI | Interactive API Docs |

---

## Getting Started

### ?? Prerequisites

**Required:**
- ? Visual Studio 2022 (or later)
- ? .NET 9 SDK
- ? SQL Server (LocalDB included with VS)

**Optional:**
- Postman (API testing)
- Git (version control)

### ?? Installation

#### **1. Clone Repository**

```bash
git clone https://github.com/robbevanhalst-dev/dotnet-project-management-platform.git
cd dotnet-project-management-platform
```

#### **2. Restore Dependencies**

```bash
dotnet restore
```

**Or in Visual Studio:**
- Right-click Solution ? Restore NuGet Packages

#### **3. Update Database**

**Command Line:**
```bash
dotnet ef database update \
  --project src/ProjectManagement.Infrastructure \
  --startup-project src/ProjectManagement.Api/ProjectManagement.Api
```

**Package Manager Console (VS):**
```powershell
Update-Database
```

**This creates:**
- Database: `ProjectManagementDb`
- Tables: Users, RefreshTokens, Projects, ProjectMembers, Tasks
- Indexes & Constraints

#### **4. Configure Settings**

**appsettings.json** (already configured):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectManagementDb;Trusted_Connection=true"
  },
  "Jwt": {
    "Key": "SUPER_SECRET_KEY_FOR_JWT_TOKEN_SIGNING_Min32Chars_Security!",
    "Issuer": "ProjectManagementApi",
    "Audience": "ProjectManagementApiUsers",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

**?? Production:** Change JWT Key to a secure random value!

#### **5. Run Application**

**Command Line:**
```bash
cd src/ProjectManagement.Api/ProjectManagement.Api
dotnet run
```

**Visual Studio:**
1. Set `ProjectManagement.Api` as Startup Project
2. Press `F5` or click ?? Run

**Application starts:**
```
[10:00:00 INF] Starting ProjectManagement API
[10:00:01 INF] Now listening on: https://localhost:7264
[10:00:01 INF] Now listening on: http://localhost:5161
[10:00:01 INF] Application started
```

#### **6. Access Swagger UI**

Open browser:
```
https://localhost:7264/swagger
```

**Swagger shows:**
- All API endpoints
- Request/Response schemas
- Authentication (?? Authorize button)
- Try it out functionality

### ?? Quick Test

#### **1. Register User**

```bash
curl -X POST https://localhost:7264/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@test.com",
    "password": "Admin123",
    "role": "Admin"
  }'
```

#### **2. Authenticate**

```bash
curl -X POST https://localhost:7264/api/auth/authenticate \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@test.com",
    "password": "Admin123"
  }' \
  -c cookies.txt
```

**Save the accessToken from response!**

#### **3. Get Projects**

```bash
curl https://localhost:7264/api/projects?pageNumber=1&pageSize=10 \
  -H "Authorization: Bearer {YOUR_ACCESS_TOKEN}"
```

#### **4. Test Error Handling**

```bash
curl https://localhost:7264/api/projects/test-error
```

---

## Testing

### ?? Unit Tests

#### **Test Coverage**

**AuthService Tests:** 22 tests
```
? Register Tests (7)
   ?? Valid data creates user
   ?? Duplicate email returns false
   ?? Manager role assignment
   ?? Admin role assignment
   ?? Default role assignment
   ?? Password hashing verification
   ?? Multiple users creation

? Login Tests (4)
   ?? Valid credentials return token
   ?? Invalid email returns null
   ?? Wrong password returns null
   ?? Case-sensitive email handling

? JWT Token Tests (7)
   ?? Generates valid JWT token
   ?? Contains User ID claim
   ?? Contains Email claim
   ?? Contains Role claim
   ?? Has expiration time
   ?? Correct role for all user types
   ?? Multiple logins generate valid tokens

? Configuration Tests (1)
   ?? Short JWT key validation

? Edge Cases (3)
   ?? Empty password handling
   ?? Multiple concurrent operations
   ?? Token lifetime validation
```

#### **Test Framework**

```csharp
public class AuthServiceTests : IDisposable
{
    private readonly ProjectManagementDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // In-memory database
        var options = new DbContextOptionsBuilder<ProjectManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ProjectManagementDbContext(options);

        // Test configuration
        var config = new Dictionary<string, string>
        {
            {"Jwt:Key", "TEST_KEY_MIN_32_CHARS_FOR_SECURITY_PURPOSES_HERE"},
            {"Jwt:Issuer", "ProjectManagementApi"},
            {"Jwt:Audience", "ProjectManagementApiUsers"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config!)
            .Build();

        _authService = new AuthService(_context, _configuration);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

#### **Example Test**

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
    user.Role.Should().Be("User");
    user.PasswordHash.Should().NotBeNullOrEmpty();
}
```

#### **Run Tests**

**All tests:**
```bash
dotnet test
```

**Specific test class:**
```bash
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

**With verbosity:**
```bash
dotnet test --verbosity detailed
```

**Test Results:**
```
Test summary:
- Total: 22
- Passed: 22 ?
- Failed: 0
- Skipped: 0
- Duration: ~8 seconds
```

---

## Client Implementation

### ?? Client Examples

#### **JavaScript/TypeScript**

```typescript
class AuthService {
    private accessToken: string | null = null;

    async authenticate(email: string, password: string) {
        const response = await fetch('/api/auth/authenticate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password }),
            credentials: 'include' // Important for cookies!
        });

        if (!response.ok) {
            throw new Error('Authentication failed');
        }

        const data = await response.json();
        this.accessToken = data.accessToken;
        
        return data;
    }

    async apiCall(endpoint: string, options: RequestInit = {}) {
        const response = await this.makeRequest(endpoint, options);

        // Auto-refresh on 401
        if (response.status === 401) {
            const refreshed = await this.refreshToken();
            
            if (refreshed) {
                return await this.makeRequest(endpoint, options);
            }
            
            // Redirect to login
            window.location.href = '/login';
        }

        return response;
    }

    private async makeRequest(endpoint: string, options: RequestInit) {
        return await fetch(endpoint, {
            ...options,
            headers: {
                ...options.headers,
                'Authorization': `Bearer ${this.accessToken}`
            }
        });
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
            
            return false;
        } catch {
            return false;
        }
    }

    async logout() {
        await fetch('/api/auth/logout', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.accessToken}`
            },
            credentials: 'include'
        });

        this.accessToken = null;
    }
}

// Usage
const auth = new AuthService();

// Login
await auth.authenticate('user@example.com', 'Password123');

// API call with auto-refresh
const response = await auth.apiCall('/api/projects');
const projects = await response.json();

// Logout
await auth.logout();
```

#### **C# HttpClient**

```csharp
public class ApiClient
{
    private string? _accessToken;
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookies;

    public ApiClient(string baseUrl)
    {
        _cookies = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies,
            UseCookies = true
        };
        
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public async Task<bool> AuthenticateAsync(string email, string password)
    {
        var request = new { email, password };
        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/authenticate", 
            request
        );

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content
                .ReadFromJsonAsync<AuthenticateResponse>();
            
            _accessToken = result?.AccessToken;
            return true;
        }

        return false;
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var request = CreateRequest(HttpMethod.Get, endpoint);
        var response = await _httpClient.SendAsync(request);

        // Auto-refresh on 401
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (await RefreshTokenAsync())
            {
                request = CreateRequest(HttpMethod.Get, endpoint);
                response = await _httpClient.SendAsync(request);
            }
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint, 
        TRequest data)
    {
        var request = CreateRequest(HttpMethod.Post, endpoint);
        request.Content = JsonContent.Create(data);
        
        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (await RefreshTokenAsync())
            {
                request = CreateRequest(HttpMethod.Post, endpoint);
                request.Content = JsonContent.Create(data);
                response = await _httpClient.SendAsync(request);
            }
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    private async Task<bool> RefreshTokenAsync()
    {
        var response = await _httpClient.PostAsync(
            "/api/auth/refresh-token", 
            null
        );

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content
                .ReadFromJsonAsync<AuthenticateResponse>();
            
            _accessToken = result?.AccessToken;
            return true;
        }

        return false;
    }

    public async Task LogoutAsync()
    {
        var request = CreateRequest(HttpMethod.Post, "/api/auth/logout");
        await _httpClient.SendAsync(request);
        _accessToken = null;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, endpoint);
        
        if (!string.IsNullOrEmpty(_accessToken))
        {
            request.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", _accessToken);
        }
        
        return request;
    }
}

// Usage
var client = new ApiClient("https://localhost:7264");

// Login
await client.AuthenticateAsync("user@example.com", "Password123");

// Get data with auto-refresh
var projects = await client.GetAsync<List<Project>>("/api/projects");

// Post data
var newProject = new CreateProjectDto { Name = "New Project" };
var created = await client.PostAsync<CreateProjectDto, Project>(
    "/api/projects", 
    newProject
);

// Logout
await client.LogoutAsync();
```

#### **React/Next.js Example**

```typescript
// lib/api.ts
export class ApiClient {
    private static accessToken: string | null = null;

    static async authenticate(email: string, password: string) {
        const response = await fetch('/api/auth/authenticate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password }),
            credentials: 'include'
        });

        const data = await response.json();
        this.accessToken = data.accessToken;
        
        return data;
    }

    static async get<T>(endpoint: string): Promise<T> {
        return await this.request<T>(endpoint, { method: 'GET' });
    }

    static async post<T>(endpoint: string, data: any): Promise<T> {
        return await this.request<T>(endpoint, {
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    private static async request<T>(
        endpoint: string, 
        options: RequestInit
    ): Promise<T> {
        let response = await fetch(endpoint, {
            ...options,
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.accessToken}`,
                ...options.headers
            }
        });

        if (response.status === 401) {
            const refreshed = await this.refresh();
            if (refreshed) {
                response = await fetch(endpoint, {
                    ...options,
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${this.accessToken}`,
                        ...options.headers
                    }
                });
            }
        }

        return await response.json();
    }

    private static async refresh(): Promise<boolean> {
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
        } catch {
            return false;
        }
        
        return false;
    }
}

// components/ProjectList.tsx
import { useEffect, useState } from 'react';
import { ApiClient } from '@/lib/api';

export function ProjectList() {
    const [projects, setProjects] = useState([]);

    useEffect(() => {
        ApiClient.get('/api/projects?pageNumber=1&pageSize=10')
            .then(setProjects);
    }, []);

    return (
        <div>
            {projects.map(project => (
                <div key={project.id}>{project.name}</div>
            ))}
        </div>
    );
}
```

---

## Configuration

### ?? Application Settings

#### **appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectManagementDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "SUPER_SECRET_KEY_FOR_JWT_TOKEN_SIGNING_Min32Chars_Security!",
    "Issuer": "ProjectManagementApi",
    "Audience": "ProjectManagementApiUsers",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithThreadId" ]
  },
  "AllowedHosts": "*"
}
```

#### **appsettings.Development.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectManagementDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### **Environment Variables**

**Override configuration via environment variables:**

```bash
# JWT Settings
export Jwt__Key="YourProductionSecretKey32CharsMin"
export Jwt__AccessTokenExpirationMinutes=30
export Jwt__RefreshTokenExpirationDays=14

# Database
export ConnectionStrings__DefaultConnection="Server=prod-server;Database=ProdDb;..."

# Serilog
export Serilog__MinimumLevel__Default="Warning"
```

**Double underscore (`__`) replaces colon (`:`) in JSON path**

#### **Production Configuration**

**Recommendations:**

1. **JWT Key:**
   ```bash
   # Generate secure random key (PowerShell)
   [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
   
   # Store in environment variable or Azure Key Vault
   ```

2. **Database:**
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=prod-server.database.windows.net;Database=ProdDb;User Id=admin;Password=***;Encrypt=True"
   }
   ```

3. **Logging:**
   ```json
   "Serilog": {
     "MinimumLevel": { "Default": "Warning" },
     "WriteTo": [
       { "Name": "ApplicationInsights" },
       { "Name": "Seq", "Args": { "serverUrl": "https://seq.yourcompany.com" } }
     ]
   }
   ```

4. **CORS:**
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

## Best Practices

### ? Security Best Practices

#### **1. Token Management**

**? Do:**
```csharp
// Short-lived access tokens
options.AccessTokenExpirationMinutes = 15;

// Rotate refresh tokens
var newRefreshToken = RotateRefreshToken(oldToken, ipAddress);

// Store in HTTP-only cookies
Response.Cookies.Append("refreshToken", token, new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict
});
```

**? Don't:**
```csharp
// Long-lived access tokens
options.AccessTokenExpirationMinutes = 1440; // 24 hours - BAD!

// Reuse refresh tokens
return oldRefreshToken; // No rotation - BAD!

// Store in localStorage (JavaScript accessible)
localStorage.setItem('token', refreshToken); // XSS risk!
```

#### **2. Password Security**

**? Do:**
```csharp
// Use ASP.NET Identity PasswordHasher
var hasher = new PasswordHasher<User>();
user.PasswordHash = hasher.HashPassword(user, password);

// Verify with constant-time comparison
var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
```

**? Don't:**
```csharp
// Plain text passwords
user.Password = password; // NEVER!

// Weak hashing
user.PasswordHash = MD5.ComputeHash(password); // WEAK!
```

#### **3. Logging**

**? Do:**
```csharp
// Structured logging
_logger.LogInformation("User {UserId} logged in from {IpAddress}", 
    userId, ipAddress);

// Log security events
_logger.LogWarning("Failed login attempt for {Email}", email);
```

**? Don't:**
```csharp
// Log sensitive data
_logger.LogInformation($"User logged in with password {password}"); // NEVER!

// String concatenation
_logger.LogInformation("User " + userId + " logged in"); // BAD for structured logging
```

### ?? Architecture Best Practices

#### **1. Separation of Concerns**

**? Do:**
```
API Layer      ? HTTP concerns (Controllers, Middleware)
Application    ? Business logic (Services, DTOs)
Domain         ? Core entities (No dependencies)
Infrastructure ? Data access (EF Core, Database)
```

**? Don't:**
```csharp
// Business logic in controllers
[HttpPost]
public IActionResult Register(RegisterDto dto)
{
    // Database access in controller - BAD!
    var user = new User { Email = dto.Email };
    _context.Users.Add(user);
    _context.SaveChanges();
}
```

#### **2. Dependency Injection**

**? Do:**
```csharp
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }
}
```

**? Don't:**
```csharp
// Direct instantiation
public class AuthController : ControllerBase
{
    private readonly AuthService _authService = new AuthService(); // BAD!
}
```

### ?? Code Quality Best Practices

#### **1. Async/Await**

**? Do:**
```csharp
public async Task<IActionResult> GetProjectsAsync()
{
    var projects = await _context.Projects.ToListAsync();
    return Ok(projects);
}
```

**? Don't:**
```csharp
// Blocking async calls
public IActionResult GetProjects()
{
    var projects = _context.Projects.ToListAsync().Result; // BLOCKING!
    return Ok(projects);
}
```

#### **2. Error Handling**

**? Do:**
```csharp
try
{
    await _authService.LoginAsync(dto);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Login failed for {Email}", dto.Email);
    throw; // Re-throw for global middleware
}
```

**? Don't:**
```csharp
// Swallow exceptions
try
{
    await _authService.LoginAsync(dto);
}
catch
{
    // Silent failure - BAD!
}
```

---

## Troubleshooting

### ?? Common Issues

#### **Issue: "Refresh token not provided"**

**Symptoms:**
```json
{
  "message": "Refresh token is required"
}
```

**Causes:**
- Cookie not sent with request
- CORS settings block credentials
- Browser security settings

**Solutions:**

**1. Check client-side:**
```javascript
// Add credentials: 'include'
fetch('/api/auth/refresh-token', {
    method: 'POST',
    credentials: 'include' // Important!
});
```

**2. Configure CORS:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://your-frontend.com")
              .AllowCredentials()  // Required for cookies!
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

**3. Check cookie settings:**
```csharp
// For development (HTTP)
var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Secure = false,  // Allow HTTP in dev
    SameSite = SameSiteMode.Lax  // Less strict for dev
};
```

---

#### **Issue: "Invalid or expired refresh token"**

**Symptoms:**
```json
{
  "message": "Invalid or expired refresh token"
}
```

**Causes:**
- Token has expired (>7 days old)
- Token has been revoked
- Token doesn't exist in database
- Database connection issues

**Solutions:**

**1. Check expiration:**
```sql
SELECT Token, ExpiresAt, RevokedAt, IsActive
FROM RefreshTokens
WHERE Token = 'your-token'
```

**2. Check database:**
```csharp
var token = await _context.RefreshTokens
    .Include(rt => rt.User)
    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

if (token == null)
    _logger.LogWarning("Refresh token not found in database");
else if (token.IsExpired)
    _logger.LogWarning("Refresh token expired at {ExpiresAt}", token.ExpiresAt);
else if (token.IsRevoked)
    _logger.LogWarning("Refresh token was revoked at {RevokedAt}", token.RevokedAt);
```

**3. Force re-authentication:**
```typescript
if (response.status === 401) {
    // Redirect to login
    window.location.href = '/login';
}
```

---

#### **Issue: "Cookies not working"**

**Symptoms:**
- Refresh token cookie not set
- Cookie not sent with requests
- CORS errors

**Causes:**
- SameSite=Strict with cross-origin requests
- HTTPS required but using HTTP
- Browser blocks third-party cookies

**Solutions:**

**1. Development (same origin):**
```csharp
var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Secure = false,  // Allow HTTP
    SameSite = SameSiteMode.Lax,
    Domain = "localhost"
};
```

**2. Production (cross-origin):**
```csharp
var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Secure = true,  // HTTPS required
    SameSite = SameSiteMode.None,  // Allow cross-origin
    Domain = ".yourcompany.com"
};
```

**3. CORS configuration:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCredentials", policy =>
    {
        policy.WithOrigins("https://frontend.yourcompany.com")
              .AllowCredentials()  // Important!
              .WithHeaders("Authorization", "Content-Type")
              .WithMethods("GET", "POST");
    });
});

app.UseCors("AllowCredentials");
```

---

#### **Issue: "JWT token validation fails"**

**Symptoms:**
```
401 Unauthorized
IDX10223: Lifetime validation failed
```

**Causes:**
- Token expired
- Clock skew between systems
- Invalid signing key
- Incorrect issuer/audience

**Solutions:**

**1. Check token expiration:**
```csharp
var handler = new JwtSecurityTokenHandler();
var token = handler.ReadJwtToken(accessToken);

Console.WriteLine($"Expires: {token.ValidTo}");
Console.WriteLine($"Now: {DateTime.UtcNow}");
Console.WriteLine($"Expired: {token.ValidTo < DateTime.UtcNow}");
```

**2. Allow clock skew:**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)  // Allow 5 min clock skew
        };
    });
```

**3. Verify settings:**
```csharp
// Ensure these match between token generation and validation
ValidIssuer = "ProjectManagementApi",
ValidAudience = "ProjectManagementApiUsers",
IssuerSigningKey = new SymmetricSecurityKey(key)
```

---

#### **Issue: "Database migration fails"**

**Symptoms:**
```
Unable to connect to database
Migration 'AddRefreshTokens' already applied
```

**Causes:**
- Database server not running
- Connection string incorrect
- Migration already applied

**Solutions:**

**1. Check database:**
```bash
# List databases
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT name FROM sys.databases"

# Check if database exists
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT 1 FROM sys.databases WHERE name = 'ProjectManagementDb'"
```

**2. Reset database:**
```bash
# Drop database
dotnet ef database drop --force

# Create and migrate
dotnet ef database update
```

**3. Check migrations:**
```bash
# List migrations
dotnet ef migrations list

# Remove last migration
dotnet ef migrations remove

# Add migration again
dotnet ef migrations add AddRefreshTokens
```

---

#### **Issue: "Logging not working"**

**Symptoms:**
- No log files created
- Console logs not showing
- Missing log entries

**Causes:**
- Serilog not configured
- Wrong log level
- File permissions

**Solutions:**

**1. Check Serilog configuration:**
```csharp
// In Program.cs
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();  // Important!
```

**2. Check log level:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"  // Not "Warning"
    }
  }
}
```

**3. Check file permissions:**
```bash
# Windows: Ensure write access to Logs folder
icacls Logs /grant Users:(OI)(CI)F

# Or create logs folder manually
mkdir Logs
```

**4. Test logging:**
```csharp
_logger.LogInformation("TEST LOG MESSAGE");
_logger.LogWarning("TEST WARNING");
_logger.LogError("TEST ERROR");
```

---

### ?? Support

**Documentation:**
- RBAC_GUIDE.md
- REFRESH_TOKEN_GUIDE.md
- MIDDLEWARE_LOGGING_PAGINATION_GUIDE.md
- README_AuthServiceTests.md

**GitHub Issues:**
https://github.com/robbevanhalst-dev/dotnet-project-management-platform/issues

**Author:**
Robbe Vanhalst  
GitHub: https://github.com/robbevanhalst-dev

---

## License

MIT License - See LICENSE file for details

---

## Acknowledgments

- ASP.NET Core Documentation
- Entity Framework Core Documentation
- Serilog Community
- JWT.io
- Clean Architecture by Robert C. Martin
- Microsoft Security Best Practices

---

**Last Updated:** January 2025  
**Version:** 1.0  
**Status:** ? Production Ready

---

**? If this documentation helped you, please star the repository!**

