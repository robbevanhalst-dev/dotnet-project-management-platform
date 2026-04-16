# Troubleshooting Guide

Common issues and solutions.

## Authentication Issues

### 401 Unauthorized

**Symptoms:**
- API returns 401 status code
- "Unauthorized" message

**Causes & Solutions:**

**1. Token not provided:**
```javascript
// ? Wrong
fetch('/api/projects');

// ? Correct
fetch('/api/projects', {
    headers: {
        'Authorization': 'Bearer ' + accessToken
    }
});
```

**2. Token expired:**
- Access tokens expire after 15 minutes
- Use refresh token to get new access token

```javascript
async function refreshAccessToken() {
    const response = await fetch('/api/auth/refresh-token', {
        method: 'POST',
        credentials: 'include'  // Important!
    });
    const data = await response.json();
    return data.accessToken;
}
```

**3. Invalid token format:**
```http
? Authorization: eyJhbGc...
? Authorization: Bearer eyJhbGc...
```

### 403 Forbidden

**Symptoms:**
- API returns 403 status code
- User is authenticated but access denied

**Causes & Solutions:**

**1. Insufficient role:**
```http
# Trying to delete project as Manager (requires Admin)
DELETE /api/projects/{id}
Authorization: Bearer {manager-token}

# Response: 403 Forbidden
```

**Solution:** Check required role in [Security Guide (RBAC section)](SECURITY.md)

**2. Resource-based authorization:**
```csharp
// User trying to access someone else's task
GET /api/tasks/{id}

// Check ownership
var task = await _taskService.GetTaskByIdAsync(id);
if (task.AssignedUserId != currentUserId && !User.IsInRole("Admin"))
    return Forbid();
```

## Refresh Token Issues

### "Refresh token not provided"

**Cause:** Cookie not sent with request

**Solution:**
```javascript
// ? Correct - Include credentials
fetch('/api/auth/refresh-token', {
    method: 'POST',
    credentials: 'include'  // Required for cookies
});
```

### "Invalid or expired refresh token"

**Causes:**

**1. Token expired (>7 days):**
```sql
-- Check token expiration
SELECT Token, ExpiresAt, GETDATE() AS Now
FROM RefreshTokens
WHERE Token = 'your-token';
```

**Solution:** User must re-authenticate

**2. Token revoked:**
```sql
-- Check if token was revoked
SELECT Token, RevokedAt, RevokedByIp
FROM RefreshTokens
WHERE Token = 'your-token';
```

**3. Token not in database:**
- User never authenticated
- Database was reset
- Token manually deleted

### Cookies Not Working

**Cause:** CORS or SameSite configuration

**Development Fix:**
```csharp
var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Secure = false,  // Allow HTTP
    SameSite = SameSiteMode.Lax
};
```

**Production Fix:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourfrontend.com")
              .AllowCredentials()  // Required!
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

## Database Issues

### "Cannot open database"

**Check connection:**
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT name FROM sys.databases"
```

**Create LocalDB instance:**
```bash
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

### Migration Fails

**Reset database:**
```bash
# Drop database
dotnet ef database drop --force \
  --project src/ProjectManagement.Infrastructure \
  --startup-project src/ProjectManagement.Api/ProjectManagement.Api

# Recreate with migrations
dotnet ef database update \
  --project src/ProjectManagement.Infrastructure \
  --startup-project src/ProjectManagement.Api/ProjectManagement.Api
```

### "Pending model changes"

**Create migration:**
```bash
dotnet ef migrations add FixChanges \
  --project src/ProjectManagement.Infrastructure \
  --startup-project src/ProjectManagement.Api/ProjectManagement.Api
```

## JWT Token Issues

### "Invalid token"

**Causes:**

**1. JWT Key too short:**
```json
{
  "Jwt": {
    "Key": "short"  // ? Must be ?32 characters
  }
}
```

**Solution:**
```powershell
# Generate secure key
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

**2. Issuer/Audience mismatch:**
```json
// appsettings.json
{
  "Jwt": {
    "Issuer": "ProjectManagementApi",
    "Audience": "ProjectManagementApiUsers"
  }
}
```

Must match Program.cs configuration.

### "Clock skew"

**Allow clock difference:**
```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ClockSkew = TimeSpan.FromMinutes(5)
};
```

## Build/Runtime Issues

### "Cannot find assembly"

**Clean and rebuild:**
```bash
dotnet clean
dotnet build
```

### Port Already in Use

**Change port in launchSettings.json:**
```json
{
  "profiles": {
    "https": {
      "applicationUrl": "https://localhost:7265;http://localhost:5001"
    }
  }
}
```

### SSL Certificate Issues

**Trust development certificate:**
```bash
dotnet dev-certs https --trust
```

## Logging Issues

### No Logs Appearing

**Check Serilog configuration:**
```csharp
// Ensure Serilog is configured before building
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();  // Must be called!
```

### Log File Not Created

**Check permissions:**
```bash
# Ensure write permissions to Logs folder
chmod 755 Logs/
```

## Performance Issues

### Slow Queries

**Enable query logging:**
```csharp
builder.Services.AddDbContext<ProjectManagementDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .EnableSensitiveDataLogging()  // Development only!
           .LogTo(Console.WriteLine);
});
```

### High Memory Usage

**Check connection pooling:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...;Max Pool Size=100;Min Pool Size=5;"
  }
}
```

## Testing Issues

### Tests Failing Randomly

**Ensure test isolation:**
```csharp
// Use unique database per test
.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
```

### Test Timeout

**Increase timeout:**
```csharp
[Fact(Timeout = 10000)]  // 10 seconds
public async Task SlowTest()
{
    // Test implementation
}
```

## Get Help

**Still stuck?**

1. Check [GitHub Issues](https://github.com/robbevanhalst-dev/dotnet-project-management-platform/issues)
2. Review [API Reference](API_REFERENCE.md)
3. Check [Configuration](CONFIGURATION.md)
4. Enable detailed logging
5. Create a new issue with:
   - Error message
   - Steps to reproduce
   - Environment (OS, .NET version)
   - Logs

---

**Quick Links:**
- [Getting Started](GETTING_STARTED.md)
- [Security Guide](SECURITY.md)
- [Database Guide](DATABASE.md)
- [Documentation Overview](DOCUMENTATION_OVERVIEW.md) - All documentation
