# Role-Based Access Control (RBAC) Guide

## Overview

Three-tier authorization system:
- **User** ? Standard role (view own data)
- **Manager** ? Project/task management
- **Admin** ? Full system access

## Roles Definition

```csharp
// ProjectManagement.Domain.Constants.Roles
public const string Admin = "Admin";
public const string Manager = "Manager";
public const string User = "User";
```

## Authorization Methods

```csharp
[Authorize]                           // Any authenticated user
[Authorize(Roles = "Admin")]          // Admin only
[Authorize(Roles = "Manager,Admin")]  // Manager OR Admin
[Authorize(Policy = "AdminOnly")]     // Policy-based
[AllowAnonymous]                      // Public endpoint
```

## API Endpoints by Role

### Authentication (Public)
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/auth/register` | POST | - | Register user |
| `/api/auth/authenticate` | POST | - | Login (JWT + refresh token) |
| `/api/auth/refresh-token` | POST | - | Refresh access token |
| `/api/auth/logout` | POST | User | Logout |

### Projects
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/projects` | GET | User | Get projects (paginated) |
| `/api/projects/{id}` | GET | User | Get project by ID |
| `/api/projects` | POST | Manager+ | Create project |
| `/api/projects/{id}` | PUT | Manager+ | Update project |
| `/api/projects/{id}` | DELETE | Admin | Delete project |
| `/api/projects/{projectId}/members/{userId}` | POST | Manager+ | Add member |
| `/api/projects/{projectId}/members/{userId}` | DELETE | Manager+ | Remove member |

### Tasks
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/tasks` | GET | User | Get my tasks |
| `/api/tasks/all` | GET | Admin | Get all tasks |
| `/api/tasks/{id}` | GET | User | Get task by ID |
| `/api/tasks` | POST | Manager+ | Create task |
| `/api/tasks/{id}` | PUT | User | Update task |
| `/api/tasks/{id}` | DELETE | Manager+ | Delete task |
| `/api/tasks/{taskId}/assign/{userId}` | POST | Manager+ | Assign task |

### Users
| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/api/users/profile` | GET | User | Get my profile |
| `/api/users` | GET | Admin | Get all users |
| `/api/users/{id}` | GET | Manager+ | Get user by ID |
| `/api/users/{id}/role` | PUT | Admin | Update role |
| `/api/users/{id}` | DELETE | Admin | Delete user |
| `/api/users/managers` | GET | Manager+ | Get managers |

## Quick Start

### 1. Register Users

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

### 2. Authenticate

```bash
POST /api/auth/authenticate
{
  "email": "admin@test.com",
  "password": "Password123"
}

# Response
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "rQP+5vXm...",
  "userId": "guid",
  "email": "admin@test.com",
  "role": "Admin"
}
```

### 3. Use Token

```bash
# Add to request headers
Authorization: Bearer eyJhbGc...
```

### 4. Test in Swagger

1. Open `https://localhost:7264/swagger`
2. Click **Authorize** button
3. Enter: `Bearer {your-token}`
4. Click **Authorize**
5. Test endpoints

## JWT Claims

Each token contains:
- `nameid` ? User ID (GUID)
- `email` ? User email
- `role` ? User role (User/Manager/Admin)

**Access claims in code:**
```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = User.FindFirst(ClaimTypes.Email)?.Value;
var role = User.FindFirst(ClaimTypes.Role)?.Value;
var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
```

## Authorization Policies

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

## HTTP Status Codes

| Code | Meaning | When |
|------|---------|------|
| 200 | OK | Success |
| 401 | Unauthorized | Not logged in / token expired |
| 403 | Forbidden | Logged in but insufficient permissions |
| 400 | Bad Request | Invalid data |
| 404 | Not Found | Resource not found |

## Testing

### cURL Examples

```bash
# Login as Admin
curl -X POST https://localhost:7264/api/auth/authenticate \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Password123"}'

# Access protected endpoint
curl -X GET https://localhost:7264/api/projects \
  -H "Authorization: Bearer {your-token}"

# Test authorization (expect 403 for User role)
curl -X DELETE https://localhost:7264/api/projects/{id} \
  -H "Authorization: Bearer {user-token}"
```

## Troubleshooting

**401 Unauthorized:**
- Token missing or expired
- Format must be: `Bearer {token}`

**403 Forbidden:**
- User authenticated but lacks required role
- Check role claim in JWT token

**Invalid Token:**
- JWT Key must be ?32 characters (appsettings.json)
- Issuer and Audience must match

## Best Practices

1. Use `[Authorize]` at controller level, `[AllowAnonymous]` for public endpoints
2. Use policies for complex authorization
3. Use `Roles` constants to avoid typos
4. Always validate claims before use
5. Token expiration: 15 minutes (configurable)

---

**Author:** Robbe Vanhalst  
**Last Updated:** January 2025

**See also:**
- [Security Guide](SECURITY.md) - Detailed security documentation
- [API Reference](API_REFERENCE.md) - Complete endpoint list
- [Getting Started](GETTING_STARTED.md) - Quick start guide

