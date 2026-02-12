# Role-Based Access Control (RBAC) Implementatie

## Overzicht

Deze applicatie implementeert Role-Based Access Control met drie rollen:
- **User**: Standaard rol voor reguliere gebruikers
- **Manager**: Rol voor teamleiders/projectmanagers
- **Admin**: Rol voor systeembeheerders

## Beschikbare Rollen

Gedefinieerd in `ProjectManagement.Domain.Constants.Roles`:
```csharp
public const string Admin = "Admin";
public const string Manager = "Manager";
public const string User = "User";
```

## Authorization Methodes

### 1. Role-based Authorization (met Roles attribuut)
```csharp
[Authorize(Roles = "Admin")]
[Authorize(Roles = "Manager,Admin")]
```

### 2. Policy-based Authorization
```csharp
[Authorize(Policy = "AdminOnly")]
[Authorize(Policy = "ManagerOrAdmin")]
```

### 3. Basis Authentication (alle authenticated users)
```csharp
[Authorize]
```

### 4. Publieke Endpoints
```csharp
[AllowAnonymous]
```

## Endpoints Overzicht

### AuthController (Publiek)
- `POST /api/auth/register` - Registreer nieuwe gebruiker (met role parameter)
- `POST /api/auth/login` - Login en ontvang JWT token

### ProjectsController
| Endpoint | Methode | Autorisatie | Beschrijving |
|----------|---------|-------------|--------------|
| `/api/projects` | GET | Authenticated | Alle projecten van gebruiker |
| `/api/projects/{id}` | GET | Authenticated | Specifiek project |
| `/api/projects` | POST | Manager, Admin | Nieuw project aanmaken |
| `/api/projects/{id}` | PUT | Manager, Admin | Project updaten |
| `/api/projects/{id}` | DELETE | Admin | Project verwijderen |
| `/api/projects/public` | GET | Anonymous | Publieke projecten |
| `/api/projects/admin-only` | GET | Admin | Admin data |
| `/api/projects/manager-dashboard` | GET | Manager, Admin | Manager dashboard |

### TasksController
| Endpoint | Methode | Autorisatie | Beschrijving |
|----------|---------|-------------|--------------|
| `/api/tasks/status` | GET | Anonymous | API status |
| `/api/tasks` | GET | Authenticated | Mijn taken |
| `/api/tasks/{id}` | GET | Authenticated | Specifieke taak |
| `/api/tasks` | POST | Manager, Admin | Nieuwe taak aanmaken |
| `/api/tasks/{id}` | PUT | Authenticated | Taak updaten |
| `/api/tasks/{id}` | DELETE | Manager, Admin | Taak verwijderen |
| `/api/tasks/{id}/assign` | POST | Manager, Admin | Taak toewijzen |
| `/api/tasks/all` | GET | Admin | Alle taken |

### UsersController
| Endpoint | Methode | Autorisatie | Beschrijving |
|----------|---------|-------------|--------------|
| `/api/users/profile` | GET | Authenticated | Mijn profiel |
| `/api/users/profile` | PUT | Authenticated | Profiel updaten |
| `/api/users` | GET | Admin | Alle gebruikers |
| `/api/users/{id}` | GET | Manager, Admin | Specifieke gebruiker |
| `/api/users/{id}/role` | PUT | Admin | Gebruikersrol wijzigen |
| `/api/users/{id}` | DELETE | Admin | Gebruiker verwijderen |
| `/api/users/managers` | GET | Manager, Admin | Alle managers |

## Gebruik

### 1. Registreer een gebruiker

**User (standaard)**:
```bash
POST /api/auth/register
{
  "email": "user@example.com",
  "password": "Password123"
}
```

**Manager**:
```bash
POST /api/auth/register
{
  "email": "manager@example.com",
  "password": "Password123",
  "role": "Manager"
}
```

**Admin**:
```bash
POST /api/auth/register
{
  "email": "admin@example.com",
  "password": "Password123",
  "role": "Admin"
}
```

### 2. Login

```bash
POST /api/auth/login
{
  "email": "admin@example.com",
  "password": "Password123"
}
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 3. Gebruik het Token

Voeg het token toe aan de Authorization header:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Swagger UI

De applicatie heeft Swagger UI met JWT ondersteuning:
1. Ga naar `https://localhost:7264/swagger`
2. Klik op **Authorize** knop (rechtsboven)
3. Voer in: `Bearer {your-token}`
4. Klik **Authorize**
5. Nu kun je beschermde endpoints testen

## Claims in JWT Token

Elk JWT token bevat de volgende claims:
- `nameid`: User ID (GUID)
- `email`: User email
- `role`: User role (User, Manager, of Admin)

### Claims ophalen in Controllers

```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = User.FindFirst(ClaimTypes.Email)?.Value;
var role = User.FindFirst(ClaimTypes.Role)?.Value;
var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
```

## Authorization Policies

Geconfigureerd in `Program.cs`:

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

## Response Codes

- **200 OK**: Succesvol
- **401 Unauthorized**: Niet ingelogd of token verlopen
- **403 Forbidden**: Ingelogd maar onvoldoende rechten
- **400 Bad Request**: Ongeldige data
- **404 Not Found**: Resource niet gevonden

## Best Practices

1. **Controller-level Authorization**: Gebruik `[Authorize]` op controller niveau en `[AllowAnonymous]` voor publieke endpoints
2. **Policy-based**: Gebruik policies voor complexere autorisatie logica
3. **Role Constants**: Gebruik de `Roles` constants class om typo's te voorkomen
4. **Claims validatie**: Valideer altijd de claims voordat je ze gebruikt
5. **Token expiratie**: Tokens verlopen na 2 uur (configureerbaar in `AuthService.cs`)

## Testen

### Test met cURL

**Login als Admin:**
```bash
curl -X POST https://localhost:7264/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Password123"}'
```

**Toegang tot beschermd endpoint:**
```bash
curl -X GET https://localhost:7264/api/projects \
  -H "Authorization: Bearer {your-token}"
```

**Test Admin-only endpoint als User (verwacht 403):**
```bash
curl -X DELETE https://localhost:7264/api/projects/{id} \
  -H "Authorization: Bearer {user-token}"
```

## Troubleshooting

### 401 Unauthorized
- Controleer of het token is toegevoegd aan de Authorization header
- Controleer of het token niet is verlopen
- Formaat moet zijn: `Bearer {token}`

### 403 Forbidden
- Gebruiker is geauthenticeerd maar heeft niet de juiste rol
- Controleer de rol claim in het JWT token
- Controleer de `[Authorize]` configuratie op het endpoint

### Token niet geldig
- Controleer of de JWT Key in appsettings.json minimaal 32 karakters is
- Controleer of Issuer en Audience overeenkomen

## Uitbreidingen

Je kunt dit systeem uitbreiden met:
- Custom Authorization Requirements
- Resource-based Authorization
- Multiple roles per user
- Permission-based authorization
- Claims transformation
- External authentication providers
