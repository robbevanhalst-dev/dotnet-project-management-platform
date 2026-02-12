# Project Management Platform API

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![Tests](https://img.shields.io/badge/tests-72%20passing-brightgreen)]()
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> Production-ready ASP.NET Core 9.0 Web API with JWT authentication, refresh tokens, RBAC, and Clean Architecture.

## ?? Overview

Professional SaaS-style project & task management API demonstrating enterprise .NET development practices.

**Key Features:**
- ?? JWT + Refresh Tokens (15min/7days)
- ?? Role-Based Access Control (User/Manager/Admin)
- ??? Clean Architecture (4 layers)
- ?? Serilog logging + Global error handling
- ? 72 unit tests (100% pass rate)
- ?? Swagger/OpenAPI documentation

## ??? Tech Stack

**Core:** .NET 9 • C# 13 • ASP.NET Core 9.0  
**Data:** EF Core 9.0 • SQL Server (LocalDB)  
**Security:** JWT Bearer • ASP.NET Identity  
**Logging:** Serilog 8.0  
**Testing:** xUnit 2.9 • FluentAssertions

## ?? Quick Start

```bash
# Clone repository
git clone https://github.com/robbevanhalst-dev/dotnet-project-management-platform.git
cd dotnet-project-management-platform

# Restore packages
dotnet restore

# Update database
dotnet ef database update --project src/ProjectManagement.Infrastructure --startup-project src/ProjectManagement.Api/ProjectManagement.Api

# Run application
dotnet run --project src/ProjectManagement.Api/ProjectManagement.Api

# Open Swagger UI
https://localhost:7264/swagger
```

## ?? API Endpoints

### Authentication
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | - | Register user |
| POST | `/api/auth/authenticate` | - | Login (JWT + refresh token) |
| POST | `/api/auth/refresh-token` | - | Refresh access token |
| POST | `/api/auth/logout` | User | Logout |

### Projects
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/projects` | User | Get paginated projects |
| GET | `/api/projects/{id}` | User | Get project by ID |
| POST | `/api/projects` | Manager+ | Create project |
| PUT | `/api/projects/{id}` | Manager+ | Update project |
| DELETE | `/api/projects/{id}` | Admin | Delete project |
| POST | `/api/projects/{projectId}/members/{userId}` | Manager+ | Add member |
| DELETE | `/api/projects/{projectId}/members/{userId}` | Manager+ | Remove member |

### Tasks
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/tasks` | User | Get my tasks |
| GET | `/api/tasks/all` | Admin | Get all tasks |
| POST | `/api/tasks` | Manager+ | Create task |
| PUT | `/api/tasks/{id}` | User | Update task |
| DELETE | `/api/tasks/{id}` | Manager+ | Delete task |

### Users
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/users/profile` | User | Get my profile |
| GET | `/api/users` | Admin | Get all users |
| PUT | `/api/users/{id}/role` | Admin | Update user role |

**Pagination:** `?pageNumber=1&pageSize=10&searchTerm=keyword&sortBy=name&sortDescending=false`

## ??? Architecture

```
???????????????????????????????????????????
?  API Layer (Controllers, Middleware)    ?
???????????????????????????????????????????
?  Application (Services, DTOs)           ?
???????????????????????????????????????????
?  Domain (Entities, Constants)           ?
???????????????????????????????????????????
?  Infrastructure (EF Core, Database)     ?
???????????????????????????????????????????
```

**Principles:** SOLID • Dependency Injection • Clean Architecture • Repository Pattern

## ?? Security

### Authentication Flow
```
1. Login ? Access Token (15min) + Refresh Token (7 days, HTTP-only cookie)
2. API Requests ? Authorization: Bearer {accessToken}
3. Token expires ? POST /refresh-token ? New tokens
4. Logout ? Revoke refresh token
```

### Security Features
- ? Token rotation (anti-replay attacks)
- ? HTTP-only cookies (XSS protection)
- ? PBKDF2 password hashing
- ? IP address tracking
- ? Resource-based authorization

### Role Hierarchy
```
Admin    ? Full access (user management, all operations)
Manager  ? Project/task management, view all
User     ? View/update own data only
```

## ?? Testing

**72 Unit Tests** (100% passing):
- ? AuthService (22 tests)
- ? ProjectService (18 tests)
- ? TaskService (20 tests)
- ? UserService (12 tests)

```bash
# Run all tests
dotnet test

# Detailed output
dotnet test --verbosity detailed
```

**Results:** 72/72 passing ? (~6 seconds)

## ?? Documentation

| Document | Description |
|----------|-------------|
| **[Documentation Overview](docs/DOCUMENTATION_OVERVIEW.md)** | Complete documentation index |
| **[Getting Started](docs/GETTING_STARTED.md)** | Installation & quick start |
| **[API Reference](docs/API_REFERENCE.md)** | Complete endpoint documentation |
| **[Security Guide](docs/SECURITY.md)** | Authentication & authorization |
| **[RBAC Guide](docs/RBAC_GUIDE.md)** | Role-based access control |
| **[Architecture](docs/ARCHITECTURE.md)** | Clean Architecture design |
| **[Database](docs/DATABASE.md)** | Schema & migrations |
| **[Configuration](docs/CONFIGURATION.md)** | Settings & configuration |
| **[Testing](docs/TESTING.md)** | Unit tests & coverage |
| **[Troubleshooting](docs/TROUBLESHOOTING.md)** | Common issues & solutions |

## ?? Use Cases

**Perfect for:**
- ?? Portfolio projects
- ?? Job interviews
- ?? Learning .NET best practices
- ?? Production starter template

**What it demonstrates:**
- RESTful API design
- JWT + Refresh tokens
- Clean Architecture
- SOLID principles
- Unit testing
- Error handling
- Structured logging

## ?? Configuration

**appsettings.json:**
```json
{
  "Jwt": {
    "Key": "YOUR_SECRET_KEY_MIN_32_CHARS",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectManagementDb;Trusted_Connection=true"
  }
}
```

## ?? Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## ?? License

MIT License - see [LICENSE](LICENSE) file.

## ?? Author

**Robbe Vanhalst**  
Junior .NET Backend Developer

[![GitHub](https://img.shields.io/badge/GitHub-robbevanhalst--dev-181717?logo=github)](https://github.com/robbevanhalst-dev)

## ?? Project Statistics

![Tests](https://img.shields.io/badge/Tests-72%20passing-brightgreen)
![Coverage](https://img.shields.io/badge/Coverage-~95%25-brightgreen)
![Build](https://img.shields.io/badge/Build-passing-brightgreen)

---

**? If you find this project useful, please star the repository!**

*Last Updated: January 2025*
