# Security Guide

Authentication, authorization, and security features.

## Authentication Flow

```
1. User logs in ? Access Token (15min) + Refresh Token (7 days, HTTP-only cookie)
2. User makes API requests ? Authorization: Bearer {accessToken}
3. Access token expires ? POST /refresh-token ? New tokens (old revoked)
4. User logs out ? Revoke refresh token
```

## JWT Access Tokens

**Properties:**
- **Lifetime:** 15 minutes (configurable)
- **Storage:** Client-side (memory, not localStorage)
- **Format:** Bearer token in Authorization header
- **Contents:** UserId, Email, Role claims

**Usage:**
```http
GET /api/projects
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Refresh Tokens

**Properties:**
- **Lifetime:** 7 days (configurable)
- **Storage:** HTTP-only cookie (XSS protection)
- **Rotation:** New token on each refresh (anti-replay)
- **Tracking:** IP address logging

**Security Features:**
- Automatic rotation prevents replay attacks
- Stored in database for validation
- Revoked on logout
- IP address tracking for audit

## Authorization

### Role Hierarchy

This project implements a **3-tier Role-Based Access Control (RBAC)** system:

```
???????????
?  Admin  ? ? Full system access (user management, all CRUD operations)
???????????
? Manager ? ? Project/task creation, team management, view all data
???????????
?  User   ? ? View own data, update assigned tasks
???????????
```

**Role Constants:**
```csharp
// ProjectManagement.Domain.Constants.Roles
public const string Admin = "Admin";
public const string Manager = "Manager";
public const string User = "User";
```

### Authorization Attributes

```csharp
[Authorize]                           // Any authenticated user
[Authorize(Roles = "Admin")]          // Admin only
[Authorize(Roles = "Manager,Admin")]  // Manager OR Admin
[Authorize(Policy = "AdminOnly")]     // Policy-based
[AllowAnonymous]                      // Public endpoint
```

### Authorization Policies

Configured in `Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    options.AddPolicy("RequireEmail", policy => 
        policy.RequireClaim(ClaimTypes.Email));
});
```

### API Endpoints by Role

#### Authentication (Public)
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/auth/register` | POST | - | Register user |
| `/api/auth/authenticate` | POST | - | Login (JWT + refresh token) |
| `/api/auth/refresh-token` | POST | - | Refresh access token |
| `/api/auth/logout` | POST | User | Logout |

#### Projects
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/projects` | GET | User | Get projects (paginated) |
| `/api/projects/{id}` | GET | User | Get project by ID |
| `/api/projects` | POST | Manager+ | Create project |
| `/api/projects/{id}` | PUT | Manager+ | Update project |
| `/api/projects/{id}` | DELETE | Admin | Delete project |
| `/api/projects/{projectId}/members/{userId}` | POST | Manager+ | Add member |
| `/api/projects/{projectId}/members/{userId}` | DELETE | Manager+ | Remove member |

#### Tasks
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/tasks` | GET | User | Get my tasks |
| `/api/tasks/all` | GET | Admin | Get all tasks |
| `/api/tasks/{id}` | GET | User | Get task by ID |
| `/api/tasks` | POST | Manager+ | Create task |
| `/api/tasks/{id}` | PUT | User | Update task |
| `/api/tasks/{id}` | DELETE | Manager+ | Delete task |

#### Users
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/users/profile` | GET | User | Get my profile |
| `/api/users` | GET | Admin | Get all users |
| `/api/users/{id}` | GET | Manager+ | Get user by ID |
| `/api/users/{id}/role` | PUT | Admin | Update role |

### Resource-Based Authorization

Users can only access:
- ? Own profile
- ? Projects they're member of
- ? Tasks assigned to them or in their projects

Example:
```csharp
// Check if user can access task
var canAccess = await _taskService.CanUserAccessTaskAsync(taskId, userId, userRole);
if (!canAccess)
    return Forbid();
```

### HTTP Status Codes

| Code | Meaning | When |
|------|---------|------|
| 200 | OK | Success |
| 401 | Unauthorized | Not logged in / token expired |
| 403 | Forbidden | Logged in but insufficient permissions |
| 400 | Bad Request | Invalid data |
| 404 | Not Found | Resource not found |

### Testing Authorization

#### Register Users with Different Roles

```bash
# User (default)
POST /api/auth/register
{
  "email": "user@test.com",
  "password": "Password123"
}

# Manager
POST /api/auth/register
{
  "email": "manager@test.com",
  "password": "Password123",
  "role": "Manager"
}

# Admin
POST /api/auth/register
{
  "email": "admin@test.com",
  "password": "Password123",
  "role": "Admin"
}
```

#### Test in Swagger UI

1. Open `https://localhost:5001/swagger`
2. Click **Authorize** button
3. Enter: `Bearer {your-token}`
4. Click **Authorize**
5. Test endpoints with different roles

#### cURL Examples

```bash
# Login as Admin
curl -X POST https://localhost:5001/api/auth/authenticate \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Password123"}'

# Access protected endpoint
curl -X GET https://localhost:5001/api/projects \
  -H "Authorization: Bearer {your-token}"

# Test authorization (expect 403 for User role)
curl -X DELETE https://localhost:5001/api/projects/{id} \
  -H "Authorization: Bearer {user-token}"
```

## Security Best Practices

### DO

- Use short-lived access tokens (15 minutes)
- Rotate refresh tokens on each use
- Store refresh tokens in HTTP-only cookies
- Use HTTPS in production
- Validate all user inputs
- Log security events
- Hash passwords (PBKDF2)

### DON'T

- Store tokens in localStorage (XSS risk)
- Use long-lived access tokens (>1 hour)
- Log passwords or sensitive data
- Reuse refresh tokens
- Expose internal errors to clients

## Password Security

**Requirements:**
- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 number

**Valid Examples:**
- `Password123`
- `MySecure1Pass`
- `Admin2024!`

**Invalid Examples:**
- `pass` (too short)
- `password` (no uppercase, no number)
- `PASSWORD123` (no lowercase)

**Hashing:**
- Algorithm: PBKDF2
- Implementation: ASP.NET Identity PasswordHasher
- Salting: Automatic per password

## Cookie Configuration

**Development:**
```csharp
var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Expires = DateTime.UtcNow.AddDays(7),
    Secure = false,  // Allow HTTP
    SameSite = SameSiteMode.Lax
};
```

**Production:**
```csharp
var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Expires = DateTime.UtcNow.AddDays(7),
    Secure = true,  // HTTPS only
    SameSite = SameSiteMode.Strict,
    Domain = ".yourcompany.com"
};
```

## JWT Claims

Each token contains:

```json
{
  "nameid": "user-guid",
  "email": "user@test.com",
  "role": "Admin",
  "iss": "ProjectManagementApi",
  "aud": "ProjectManagementApiUsers",
  "exp": 1234567890
}
```

**Access in code:**
```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = User.FindFirst(ClaimTypes.Email)?.Value;
var role = User.FindFirst(ClaimTypes.Role)?.Value;
```

## Token Expiration

**Configurable in appsettings.json:**
```json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

## IP Address Tracking

All refresh token operations log IP addresses:
- Token creation
- Token refresh
- Token revocation

**Audit query:**
```sql
SELECT Token, CreatedByIp, RevokedByIp, CreatedAt, RevokedAt
FROM RefreshTokens
WHERE UserId = 'user-guid'
ORDER BY CreatedAt DESC;
```

## CORS Configuration

**Production:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourfrontend.com")
              .AllowCredentials()  // Required for cookies
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

---

**See also:**
- [API Reference](API_REFERENCE.md) - Complete endpoint documentation
- [Configuration](CONFIGURATION.md) - Security configuration
- [Troubleshooting](TROUBLESHOOTING.md) - Common authentication issues
