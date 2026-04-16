# API Reference

Complete reference for all API endpoints.

## Base URL

```
https://localhost:5001/api
```

## Authentication

All endpoints (except auth) require JWT token:
```
Authorization: Bearer {your-access-token}
```

## Endpoints

### Authentication

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/auth/register` | - | Register user |
| POST | `/auth/authenticate` | - | Login (JWT + refresh token) |
| POST | `/auth/refresh-token` | - | Refresh access token |
| POST | `/auth/logout` | User | Logout & revoke token |

### Projects

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/projects` | User | Get paginated projects |
| GET | `/projects/{id}` | User | Get project by ID |
| POST | `/projects` | Manager+ | Create project |
| PUT | `/projects/{id}` | Manager+ | Update project |
| DELETE | `/projects/{id}` | Admin | Delete project |
| POST | `/projects/{projectId}/members/{userId}` | Manager+ | Add member |
| DELETE | `/projects/{projectId}/members/{userId}` | Manager+ | Remove member |

### Tasks

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/tasks` | User | Get my tasks (paginated) |
| GET | `/tasks/all` | Admin | Get all tasks |
| GET | `/tasks/{id}` | User | Get task by ID |
| POST | `/tasks` | Manager+ | Create task |
| PUT | `/tasks/{id}` | User | Update task |
| DELETE | `/tasks/{id}` | Manager+ | Delete task |
| POST | `/tasks/{taskId}/assign/{userId}` | Manager+ | Assign task |

### Users

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/users/profile` | User | Get my profile |
| GET | `/users` | Admin | Get all users (paginated) |
| GET | `/users/{id}` | Manager+ | Get user by ID |
| PUT | `/users/{id}/role` | Admin | Update user role |
| DELETE | `/users/{id}` | Admin | Delete user |
| GET | `/users/managers` | Manager+ | Get managers |

## Query Parameters

**Pagination:**
```
?pageNumber=1&pageSize=10&searchTerm=keyword&sortBy=name&sortDescending=false
```

## Request Examples

### Register User

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@test.com",
  "password": "Password123",
  "role": "User"
}
```

**Response:**
```
200 OK
```

### Authenticate

```http
POST /api/auth/authenticate
Content-Type: application/json

{
  "email": "admin@test.com",
  "password": "Password123"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "rQP+5vXm...",
  "userId": "guid",
  "email": "admin@test.com",
  "role": "Admin"
}
+ Cookie: refreshToken (HttpOnly)
```

### Create Project

```http
POST /api/projects
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "name": "Website Redesign",
  "description": "Complete website overhaul"
}
```

**Response:**
```json
{
  "id": "guid",
  "name": "Website Redesign",
  "description": "Complete website overhaul",
  "createdAt": "2025-01-01T00:00:00Z",
  "memberCount": 1,
  "taskCount": 0
}
```

### Get Projects (Paginated)

```http
GET /api/projects?pageNumber=1&pageSize=10&searchTerm=website
Authorization: Bearer eyJhbGc...
```

**Response:**
```json
{
  "pagination": {
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3,
    "totalCount": 25,
    "hasPrevious": false,
    "hasNext": true
  },
  "data": [
    {
      "id": "guid",
      "name": "Website Redesign",
      "description": "Complete website overhaul",
      "createdAt": "2025-01-01T00:00:00Z",
      "memberCount": 3,
      "taskCount": 10
    }
  ]
}
```

### Create Task

```http
POST /api/tasks
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "title": "Design Homepage",
  "description": "Create wireframes and mockups",
  "projectId": "guid",
  "assignedUserId": "guid",
  "status": "ToDo",
  "priority": "High",
  "dueDate": "2025-02-01T00:00:00Z"
}
```

## HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | OK - Success |
| 201 | Created - Resource created |
| 400 | Bad Request - Invalid data |
| 401 | Unauthorized - Not logged in / token expired |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource not found |
| 500 | Internal Server Error |

## Error Response Format

```json
{
  "success": false,
  "statusCode": 400,
  "message": "Validation failed",
  "details": "Email is required"
}
```

---

**See also:**
- [Security Guide](SECURITY.md) - Authorization details
- [RBAC Guide](RBAC_GUIDE.md) - Role-based access control
