# Configuration Guide

Application configuration, environment variables, and security setup.

## ?? Quick Security Setup

**IMPORTANT:** Never commit secrets to Git!

### Step 1: Copy Example Configuration

```bash
cd src/ProjectManagement.Api/ProjectManagement.Api
copy appsettings.Example.json appsettings.Development.json
```

### Step 2: Generate Secure JWT Key

**PowerShell (Windows):**
```powershell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | ForEach-Object {[char]$_})
```

**Linux/macOS:**
```bash
openssl rand -base64 32
```

### Step 3: Update Configuration

Open `appsettings.Development.json` and replace:
```json
{
  "Jwt": {
    "Key": "YOUR_GENERATED_SECRET_KEY_HERE"
  }
}
```

### Step 4: Verify Gitignore

```bash
git check-ignore src/ProjectManagement.Api/ProjectManagement.Api/appsettings.Development.json
# Should output the path (meaning it's ignored ?)
```

---

## Configuration Files

### appsettings.json (Development)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectManagementDb;Trusted_Connection=true"
  },
  "Jwt": {
    "Key": "YOUR_SECRET_KEY_MIN_32_CHARS",
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
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithThreadId"]
  }
}
```

### appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=ProjectManagementDb;User Id=sa;Password=***;TrustServerCertificate=true"
  },
  "Jwt": {
    "Key": "PRODUCTION_SECRET_KEY_64_CHARS_RECOMMENDED",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 14
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning"
    }
  }
}
```

## Environment Variables

Override configuration via environment variables:

```bash
# Linux/Mac
export Jwt__Key="YourProductionKey32CharsMin"
export Jwt__AccessTokenExpirationMinutes=30
export ConnectionStrings__DefaultConnection="Server=prod;..."

# Windows PowerShell
$env:Jwt__Key="YourProductionKey32CharsMin"
$env:Jwt__AccessTokenExpirationMinutes=30
$env:ConnectionStrings__DefaultConnection="Server=prod;..."

# Windows CMD
set Jwt__Key=YourProductionKey32CharsMin
set Jwt__AccessTokenExpirationMinutes=30
```

## JWT Configuration

### Generate Secure Key

**PowerShell:**
```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

**Output:**
```
rQP+5vXmFa8L3kJ2nH9pW4tY7xZ1bC6dE8fG0hI3jK5=
```

### Key Requirements

- ? Minimum 32 characters (256 bits)
- ? Random, cryptographically secure
- ? Different for each environment
- ? Stored securely (Azure Key Vault, AWS Secrets Manager)

### Token Expiration

**Development:**
- Access Token: 15 minutes
- Refresh Token: 7 days

**Production:**
- Access Token: 30 minutes (balance security/UX)
- Refresh Token: 14 days

## Database Configuration

### Connection Strings

**LocalDB (Development):**
```json
"Server=(localdb)\\mssqllocaldb;Database=ProjectManagementDb;Trusted_Connection=true"
```

**SQL Server (Windows Auth):**
```json
"Server=SERVERNAME;Database=ProjectManagementDb;Trusted_Connection=true"
```

**SQL Server (SQL Auth):**
```json
"Server=SERVERNAME;Database=ProjectManagementDb;User Id=sa;Password=YourPassword;TrustServerCertificate=true"
```

**Azure SQL:**
```json
"Server=tcp:yourserver.database.windows.net,1433;Database=ProjectManagementDb;User ID=username;Password=password;Encrypt=True;"
```

## Logging Configuration

### Log Levels

- **Trace:** Very detailed (performance impact)
- **Debug:** Debugging information
- **Information:** General flow
- **Warning:** Unexpected events
- **Error:** Errors and exceptions
- **Critical:** Critical failures

### File Logging

```json
{
  "Name": "File",
  "Args": {
    "path": "Logs/log-.txt",
    "rollingInterval": "Day",
    "retainedFileCountLimit": 30
  }
}
```

**Rolling Intervals:**
- `Day` - New file each day
- `Hour` - New file each hour
- `Month` - New file each month

### Structured Logging

```csharp
_logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ipAddress);
```

## CORS Configuration

### Development (Allow All)

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### Production (Specific Origins)

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
                "https://yourfrontend.com",
                "https://admin.yourfrontend.com"
              )
              .AllowCredentials()  // Required for cookies
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

## Authorization Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("ManagerOnly", policy => 
        policy.RequireRole("Manager"));
    
    options.AddPolicy("ManagerOrAdmin", policy => 
        policy.RequireRole("Manager", "Admin"));
    
    options.AddPolicy("RequireEmail", policy => 
        policy.RequireClaim(ClaimTypes.Email));
});
```

## Swagger Configuration

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Project Management API",
        Version = "v1",
        Description = "REST API for project and task management"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

## Docker Configuration

**appsettings.Docker.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=ProjectManagementDb;User Id=sa;Password=Your_password123;TrustServerCertificate=true"
  },
  "Jwt": {
    "Key": "${JWT_SECRET_KEY}"
  }
}
```

**docker-compose.yml:**
```yaml
services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Docker
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
```

## Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProjectManagementDbContext>();

app.MapHealthChecks("/health");
```

---

## ?? Security Best Practices

### ? What NOT to Do

- Don't commit `appsettings.Development.json` to Git
- Don't use weak/short secrets (minimum 32 characters)
- Don't share secrets in chat/email
- Don't use the same secret across environments
- Don't store secrets in source code comments

### ? What to Do

- Use strong, randomly generated keys (32+ characters)
- Use different secrets for Dev/Staging/Production
- Use environment variables in production
- Use Azure Key Vault or AWS Secrets Manager for production
- Rotate secrets regularly
- Use User Secrets for local development

### Production Secrets Management

**Azure Key Vault:**
```bash
# Create Key Vault
az keyvault create --name your-keyvault --resource-group your-rg

# Add secret
az keyvault secret set --vault-name your-keyvault --name JwtKey --value "your-secret"
```

**Update Program.cs:**
```csharp
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVaultName"];
    builder.Configuration.AddAzureKeyVault(
        new Uri($"https://{keyVaultName}.vault.azure.net/"),
        new DefaultAzureCredential());
}
```

**Environment Variables (Docker/Linux):**
```bash
export Jwt__Key="your-production-secret"
export ConnectionStrings__DefaultConnection="Server=prod;..."
```

### Security Checklist

Before deploying:

- [ ] `appsettings.Development.json` is in `.gitignore`
- [ ] JWT Key is strong (32+ characters, random)
- [ ] Different secrets for Dev/Staging/Production
- [ ] Secrets stored in Key Vault (production)
- [ ] No secrets in source code or comments
- [ ] Connection strings use environment variables
- [ ] HTTPS enabled (production)
- [ ] CORS configured properly
- [ ] Rate limiting enabled (production)

---

**See also:**
- [SECURITY.md](SECURITY.md) - Authentication & authorization
- [DATABASE.md](DATABASE.md) - Database configuration
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Common configuration issues
