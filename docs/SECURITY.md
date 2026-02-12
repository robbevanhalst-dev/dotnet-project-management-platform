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

### Roles

```
Admin    ? Full system access
Manager  ? Project/task management
User     ? Own data only
```

### Authorization Attributes

```csharp
[Authorize]                           // Any authenticated user
[Authorize(Roles = "Admin")]          // Admin only
[Authorize(Roles = "Manager,Admin")]  // Manager OR Admin
[Authorize(Policy = "AdminOnly")]     // Policy-based
[AllowAnonymous]                      // Public endpoint
```

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

## Security Best Practices

### ? DO

- Use short-lived access tokens (15 minutes)
- Rotate refresh tokens on each use
- Store refresh tokens in HTTP-only cookies
- Use HTTPS in production
- Validate all user inputs
- Log security events
- Hash passwords (PBKDF2)

### ? DON'T

- Store tokens in localStorage (XSS risk)
- Use long-lived access tokens (>1 hour)
- Log passwords or sensitive data
- Reuse refresh tokens
- Expose internal errors to clients

## Password Security

**Requirements:**
- Minimum 6 characters
- Must contain letters and numbers

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
- [RBAC Guide](RBAC_GUIDE.md) - Detailed authorization guide
- [Configuration](CONFIGURATION.md) - Security configuration
