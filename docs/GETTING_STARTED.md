# Getting Started

Quick guide to install and run the Project Management Platform API.

## Prerequisites

- .NET 9 SDK
- Visual Studio 2022 or VS Code
- SQL Server (LocalDB)

## Installation

```bash
# 1. Clone repository
git clone https://github.com/robbevanhalst-dev/dotnet-project-management-platform.git
cd dotnet-project-management-platform

# 2. Restore packages
dotnet restore

# 3. Update database
dotnet ef database update --project src/ProjectManagement.Infrastructure --startup-project src/ProjectManagement.Api/ProjectManagement.Api

# 4. Run application
dotnet run --project src/ProjectManagement.Api/ProjectManagement.Api
```

Or in Visual Studio:
1. Open `ProjectManagement.sln`
2. Set `ProjectManagement.Api` as startup project
3. Press `F5`

## Access the API

- **Swagger UI:** https://localhost:5001/swagger
- **API Base:** https://localhost:5001/api

## Quick Test

```bash
# 1. Register Admin
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Admin123","role":"Admin"}'

# 2. Authenticate
curl -X POST https://localhost:5001/api/auth/authenticate \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Admin123"}'

# 3. Get Projects (use accessToken from response)
curl https://localhost:5001/api/projects \
  -H "Authorization: Bearer {YOUR_ACCESS_TOKEN}"
```

## Next Steps

- [API Reference](API_REFERENCE.md) - Explore all endpoints
- [Security Guide](SECURITY.md) - Learn about authentication
- [Configuration](CONFIGURATION.md) - Configure the application
- [Documentation Overview](DOCUMENTATION_OVERVIEW.md) - See all docs

---

**Need help?** See [Troubleshooting](TROUBLESHOOTING.md)
