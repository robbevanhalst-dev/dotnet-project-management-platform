# Project Management Platform API

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![Tests](https://img.shields.io/badge/tests-72%20passing-brightgreen)]()
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> Production-ready ASP.NET Core 9.0 Web API with JWT authentication, refresh tokens, RBAC, and Clean Architecture.

---

## ?? About This Project

I built this **Project Management API** as a comprehensive portfolio piece to demonstrate **production-ready .NET development skills**. 

This project showcases modern enterprise patterns and best practices that go beyond typical tutorial projects:

? **Clean Architecture** with proper layer separation  
? **JWT Authentication** with refresh token rotation  
? **Role-Based Access Control** (3 role hierarchy)  
? **Comprehensive Testing** with 72 unit tests  
? **Professional Documentation** (10+ docs)  
? **Security Best Practices** (token rotation, password hashing, XSS protection)  

**What makes this different:**
- Not a simple CRUD app - implements real-world security patterns
- Complete documentation showing understanding beyond code
- Test-driven approach with high coverage
- Enterprise architecture suitable for scaling

**Target Audience:** Development teams needing a secure, scalable task management backend.

---

## ?? Key Features

### ?? Security First
- **JWT + Refresh Tokens** (15min access / 7-day refresh)
- **Token Rotation** (automatic on refresh - prevents replay attacks)
- **HTTP-only Cookies** (XSS protection)
- **PBKDF2 Password Hashing** (via ASP.NET Identity)
- **IP Address Tracking** (audit trail)
- **Role-Based Authorization** (3 levels: User, Manager, Admin)

### ??? Clean Architecture
- **4-Layer Separation** (Domain ? Application ? Infrastructure ? API)
- **Dependency Inversion** (Domain has zero dependencies)
- **Interface Segregation** (IAuthService, IProjectService, etc.)
- **Repository Pattern** (via EF Core DbContext)
- **DTOs** (no entity exposure to API)

### ?? Production Quality
- **Structured Logging** (Serilog with file + console outputs)
- **Global Exception Handling** (custom middleware)
- **Pagination & Search** (all list endpoints)
- **Input Validation** (data annotations on all DTOs)
- **Swagger/OpenAPI** (interactive documentation)
- **72 Unit Tests** (FluentAssertions + in-memory DB)


---

## ??? Tech Stack

**Core Framework:**
- .NET 9.0
- C# 13.0
- ASP.NET Core 9.0

**Data & Persistence:**
- Entity Framework Core 9.0
- SQL Server (LocalDB for development)

**Security:**
- JWT Bearer Authentication
- ASP.NET Core Identity (PasswordHasher)
- Microsoft.IdentityModel.Tokens

**Logging & Monitoring:**
- Serilog 8.0
- Serilog.Sinks.Console
- Serilog.Sinks.File

**Testing:**
- xUnit 2.9
- FluentAssertions
- In-Memory Database

**Documentation:**
- Swagger/OpenAPI
- 10+ Markdown documentation files

---

## ?? Quick Start


## ?? Quick Start

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server LocalDB (included with Visual Studio)
- Git

### Installation

```bash
# 1. Clone the repository
git clone https://github.com/robbevanhalst-dev/dotnet-project-management-platform.git
cd dotnet-project-management-platform

# 2. Set up secrets (see Configuration Guide)
copy src/ProjectManagement.Api/ProjectManagement.Api/appsettings.Example.json src/ProjectManagement.Api/ProjectManagement.Api/appsettings.Development.json

# 3. Restore & run
dotnet restore
dotnet ef database update --project src/ProjectManagement.Infrastructure --startup-project src/ProjectManagement.Api/ProjectManagement.Api
dotnet run --project src/ProjectManagement.Api/ProjectManagement.Api
```

**Access the API:**
- Swagger UI: https://localhost:7264/swagger
- API Base: https://localhost:7264/api

?? **For detailed setup instructions, see [Getting Started Guide](docs/GETTING_STARTED.md)**

---

## ?? API Endpoints

### Quick Overview

**Authentication:** Register ? Authenticate ? Get JWT ? Use with Bearer token

**Core Endpoints:**
- `/api/auth/*` - Authentication & authorization
- `/api/projects/*` - Project management (CRUD + members)
- `/api/tasks/*` - Task management (CRUD + assignment)
- `/api/users/*` - User management (profile + admin)

**Authorization Levels:**
- ?? **User** - View own data, update assigned tasks
- ?? **Manager** - Create projects/tasks, manage teams
- ?? **Admin** - Full system access

?? **For complete endpoint documentation, see [API Reference](docs/API_REFERENCE.md)**

---

## ??? Architecture

**Clean Architecture** (4-layer separation):

```
API ? Application ? Domain
  ?         ?
Infrastructure ?
```

**Key Principles:**
- ? Domain has zero dependencies
- ? Application defines interfaces, Infrastructure implements
- ? Dependency Inversion via DI

?? **For detailed architecture documentation, see [Architecture Guide](docs/ARCHITECTURE.md)**

## ?? Security

**JWT Authentication** with **Refresh Token Rotation**

**Flow:** Register ? Login ? Access Token (15min) + Refresh Token (7 days) ? Auto-rotation

**Key Features:**
- ?? Token Rotation (anti-replay)
- ?? HTTP-Only Cookies (XSS protection)
- ?? PBKDF2 Password Hashing
- ?? IP Tracking & Audit Trail
- ?? 3-Tier RBAC (User/Manager/Admin)

?? **For complete security documentation, see [Security Guide](docs/SECURITY.md)**

## ? Testing

**72 Unit Tests** with **100% pass rate** - AuthService (22) • ProjectService (18) • TaskService (20) • UserService (12)

```bash
dotnet test  # ? 72/72 passing in ~6 seconds
```

?? **For detailed testing guide, see [Testing Documentation](docs/TESTING.md)**


---

## ?? What I Learned

Building this project was an intensive learning experience that took me beyond tutorials into production-ready development:

### Technical Skills
- **JWT Security Pattern** - Implementing refresh token rotation was challenging but taught me how to prevent replay attacks
- **Clean Architecture** - Understanding the "why" behind layer separation, not just the "how"
- **EF Core Migrations** - Managing database schema evolution and relationships
- **Unit Testing** - Writing testable code by designing with interfaces first
- **Async/Await** - Proper async patterns throughout the entire stack

### Challenges Overcome
1. **Refresh Token Implementation** - Initially struggled with token rotation logic, learned about security best practices
2. **Role-Based Authorization** - Understanding the difference between authentication and authorization
3. **Global Exception Handling** - Creating consistent error responses across the API
4. **Test Isolation** - Learning to use in-memory databases and proper test cleanup

### If I Built This Again
- Start with integration tests from day 1 (not just unit tests)
- Implement resource-based authorization (users can only edit their own projects)
- Add soft delete pattern (audit trail for deleted data)
- Use MediatR for CQRS pattern (better separation of commands/queries)

### Next Steps for This Project
- [ ] Deploy to Azure with CI/CD pipeline
- [ ] Add Docker containerization
- [ ] Implement code coverage reporting
- [ ] Add API versioning
- [ ] Create Postman collection for testing

---

## ?? Documentation

Complete documentation suite (10+ documents):

| Document | Description |
|----------|-------------|
| **[Documentation Overview](docs/DOCUMENTATION_OVERVIEW.md)** | Complete documentation index |
| **[Getting Started](docs/GETTING_STARTED.md)** | Installation & quick start guide |
| **[API Reference](docs/API_REFERENCE.md)** | Complete endpoint documentation |
| **[Security Guide](docs/SECURITY.md)** | Authentication & authorization details |
| **[RBAC Guide](docs/RBAC_GUIDE.md)** | Role-based access control patterns |
| **[Architecture](docs/ARCHITECTURE.md)** | Clean Architecture deep dive |
| **[Database](docs/DATABASE.md)** | Schema design & migrations |
| **[Configuration](docs/CONFIGURATION.md)** | Settings & environment setup |
| **[Testing](docs/TESTING.md)** | Unit tests & testing strategy |
| **[Troubleshooting](docs/TROUBLESHOOTING.md)** | Common issues & solutions |

---

## ?? Use Cases

**This project is ideal for:**

? **Portfolio Showcase** - Demonstrates production-ready .NET skills  
? **Job Interviews** - Complete talking points on architecture, security, testing  
? **Learning Reference** - Study real-world implementation of Clean Architecture  
? **Starter Template** - Foundation for building SaaS applications  

**What it demonstrates:**
- Enterprise-grade API design patterns
- Security-first development approach
- Test-driven development methodology
- Professional documentation practices
- Modern .NET ecosystem knowledge

---

## ?? Configuration

### Environment Setup

**Development (appsettings.Development.json):**
```json
{
  "Jwt": {
    "Key": "YOUR_DEVELOPMENT_SECRET_KEY_MIN_32_CHARS",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectManagementDb;Trusted_Connection=true"
  }
}
```

**Production:**
- Use environment variables or Azure Key Vault for secrets
- Never commit `appsettings.Development.json` to Git
- See `appsettings.Example.json` for template

### Important Security Notes
?? **Do NOT commit secrets to Git**  
? Use `appsettings.Example.json` as template  
? Copy to `appsettings.Development.json` and add your secrets  
? Add `appsettings.Development.json` to `.gitignore`

---


## ?? Contributing

Contributions, issues, and feature requests are welcome!

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

**Areas for contribution:**
- Integration tests
- Docker containerization
- API versioning
- Resource-based authorization
- Performance optimizations

---

## ?? License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

## ????? Author

**Robbe Vanhalst**  
*Junior .NET Developer | Belgium*

I'm a recently graduated .NET developer passionate about clean code, software architecture, and security. This project represents my commitment to professional development and enterprise-grade software practices.

[![GitHub](https://img.shields.io/badge/GitHub-robbevanhalst--dev-181717?logo=github)](https://github.com/robbevanhalst-dev)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-Connect-0077B5?logo=linkedin)](https://linkedin.com/in/your-profile)

**Looking for opportunities in:**
- Backend .NET Development
- API Development
- Clean Architecture Projects
- Enterprise Software Development

---

## ?? Project Stats

![Language](https://img.shields.io/badge/Language-C%23_13-239120?logo=csharp)
![Framework](https://img.shields.io/badge/Framework-.NET_9-512BD4?logo=dotnet)
![Tests](https://img.shields.io/badge/Tests-72_passing-brightgreen)
![License](https://img.shields.io/badge/License-MIT-green.svg)

**Project Metrics:**
- **72 Unit Tests** - 100% passing
- **10+ Documentation Files** - Complete guides
- **4 Architecture Layers** - Clean separation
- **3 Role Levels** - RBAC implementation
- **~2,000 lines of code** - Production quality

---

## ?? Acknowledgments

- Clean Architecture principles by Robert C. Martin
- ASP.NET Core documentation by Microsoft
- JWT best practices from OWASP
- Testing patterns from the xUnit community

---

## ? Support

If you find this project useful or it helped you learn something new:

- ? **Star this repository**
- ?? **Report issues** if you find any
- ?? **Suggest features** via GitHub issues
- ?? **Share with others** learning .NET

---

**?? Last Updated:** January 2025  
**?? Version:** 1.0.0  
**?? Status:** Active Development

---

*Built with ?? and lots of ? by Robbe Vanhalst*
