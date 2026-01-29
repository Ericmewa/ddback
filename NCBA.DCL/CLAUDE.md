# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NCBA.DCL - A .NET 8 Web API for managing document checklists and deferrals in a loan processing workflow. This is a translation of the Express.js backend to use Entity Framework Core with MySQL (easily switchable to other SQL databases).

## Commands

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run

# Run with hot reload
dotnet watch run

# Create a migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Rollback to previous migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# Run tests (when implemented)
dotnet test
```

## Environment Configuration

Required in `appsettings.json`:
- `ConnectionStrings:DefaultConnection` - MySQL connection string
- `JwtSettings:Secret` - JWT signing key (minimum 32 characters)
- `JwtSettings:Issuer` - Token issuer
- `JwtSettings:Audience` - Token audience
- `JwtSettings:ExpiryInDays` - Token expiration period

For sensitive data, use User Secrets in development:
```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ncba_dcl;User=root;Password=password;"
```

## Architecture

### Project Structure

```
NCBA.DCL/
├── Controllers/          # API endpoints with route handlers
├── Data/                 # EF Core DbContext
├── Models/               # Entity models (database tables)
├── DTOs/                 # Data Transfer Objects for requests/responses
├── Helpers/              # Utility classes (JWT, password hashing, file upload)
├── Middleware/           # Custom middleware and attributes
├── Services/             # Business logic layer (to be expanded)
├── Validators/           # Input validation (to be expanded)
└── Program.cs            # Application entry point and configuration
```

### User Roles

The system uses role-based access control:
- `Admin` - System administrator with full access
- `RM` - Relationship Manager (manages customer relationships)
- `CoCreator` - Co-creator (creates and processes checklists)
- `CoChecker` - Co-checker (reviews and approves checklists)
- `Customer` - External customer (limited access)

### Core Entities

**User** - User accounts with role-based permissions
- Password hashing with BCrypt (work factor 10)
- Unique email, customerId, rmId
- Soft delete via Active flag

**Checklist** - Main workflow document
- Auto-generated DCL number (DCL-XXXXXX)
- Assignment tracking (CreatedBy, AssignedToRM, AssignedToCoChecker)
- Status flow: Pending → CoCreatorReview → RMReview → CoCheckerReview → Approved/Rejected
- Nested document categories with documents
- Activity logs

**Document** - Individual checklist items
- Belongs to a DocumentCategory
- Multiple status fields (Status, CreatorStatus, CheckerStatus, RmStatus)
- Comments from different roles
- File attachments (CoCreatorFiles)

**Deferral** - Loan deferral requests
- Auto-generated deferral number (DEF-XXXXXX)
- Multi-approver workflow with sequential approval
- Facilities, documents, and approvers as child entities
- Status: Pending → Approved/Rejected

**UserLog** - Audit trail for user actions
**Notification** - User notifications
**ChecklistLog** - Checklist activity logs

### Authentication & Authorization

- **JWT-based authentication** via `Authorization: Bearer <token>` header
- Token expiry: 7 days (configurable)
- Password hashing: BCrypt with salt rounds = 10
- Role-based authorization via `[RoleAuthorize(...)]` attribute
- Claims in JWT: id, email, name, role

### Database Provider

Currently configured for **MySQL** via Pomelo.EntityFrameworkCore.MySql. To switch:

**SQL Server**:
```csharp
options.UseSqlServer(connectionString)
```

**PostgreSQL**:
```csharp
options.UseNpgsql(connectionString)
```

### Key Design Patterns

1. **Repository Pattern** - DbContext acts as repository
2. **DTO Pattern** - Separate request/response models from entities
3. **Async/Await** - All database operations are asynchronous
4. **Dependency Injection** - Services registered in Program.cs
5. **Attribute Routing** - Routes defined via attributes on controllers

### Entity Relationships

- User 1:N Checklist (created, assigned as RM, assigned as CoChecker)
- Checklist 1:N DocumentCategory 1:N Document 1:N CoCreatorFile
- Checklist 1:N ChecklistLog
- User 1:N Deferral
- Deferral 1:N Facility
- Deferral 1:N DeferralDocument
- Deferral 1:N Approver
- User 1:N UserLog (as target or performer)
- User 1:N Notification

### Timestamp Management

Entities with timestamps (User, Checklist, Document, Deferral, Notification):
- `CreatedAt` - Set on creation
- `UpdatedAt` - Automatically updated on save via DbContext override

### File Upload

- Files stored in `uploads/` directory
- Unique filenames: `{timestamp}-{originalname}`
- Static file serving enabled at `/uploads/*`
- Helper: `FileUploadHelper.SaveFileAsync()`

## Code Patterns

### Controller Structure

```csharp
[ApiController]
[Route("api/controllerName")]
[Authorize]  // Require authentication
public class ControllerNameController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ControllerNameController> _logger;

    // Dependency injection
    public ControllerNameController(ApplicationDbContext context, ILogger<ControllerNameController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    [RoleAuthorize(UserRole.Admin)]  // Role-based authorization
    public async Task<IActionResult> Create([FromBody] CreateRequest request)
    {
        try
        {
            // Implementation
            await _context.SaveChangesAsync();
            return StatusCode(201, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error message");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
```

### Getting Current User

```csharp
var userId = Guid.Parse(User.FindFirst("id")?.Value ?? string.Empty);
var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
```

### Eager Loading Related Data

```csharp
var checklist = await _context.Checklists
    .Include(c => c.CreatedBy)
    .Include(c => c.AssignedToRM)
    .Include(c => c.Documents)
        .ThenInclude(dc => dc.DocList)
    .FirstOrDefaultAsync(c => c.Id == id);
```

### Password Operations

```csharp
// Hash password
var hashedPassword = PasswordHasher.HashPassword(password);

// Verify password
var isValid = PasswordHasher.VerifyPassword(enteredPassword, hashedPassword);
```

### JWT Token Generation

```csharp
var token = _tokenGenerator.GenerateToken(user);
```

## API Documentation

- Swagger UI available at `/swagger` in development
- Interactive API testing with bearer token authentication
- All endpoints documented with request/response schemas

## Common Tasks

### Adding a New Controller

1. Create controller in `Controllers/` folder
2. Inherit from `ControllerBase`
3. Add `[ApiController]` and `[Route("api/...")]` attributes
4. Inject `ApplicationDbContext` and `ILogger`
5. Implement endpoints with proper HTTP method attributes

### Adding a New Entity

1. Create model in `Models/` folder
2. Add `DbSet<EntityName>` to `ApplicationDbContext`
3. Configure relationships in `OnModelCreating`
4. Create migration: `dotnet ef migrations add AddEntityName`
5. Apply migration: `dotnet ef database update`

### Adding Role-Based Endpoint

```csharp
[HttpPost("endpoint")]
[RoleAuthorize(UserRole.Admin, UserRole.RM)]
public async Task<IActionResult> EndpointName() { }
```

## Current Status

### ✅ Implemented
- Core models (User, Checklist, Document, Deferral, UserLog, Notification)
- EF Core DbContext with MySQL
- JWT authentication and authorization
- Role-based access control
- Authentication endpoints (register, login)
- User management endpoints
- Checklist CRUD operations
- Document management
- Deferral management
- User logs

### ⚠️ TODO
- RM-specific workflow endpoints (queue, notifications, submit to cocreator)
- Checker-specific workflow endpoints (active DCLs, queue, approve/reject with notifications)
- CoCreator-specific workflow endpoints (submit to RM/CoChecker, reviews)
- File upload endpoints
- Search and filter functionality
- PDF generation for deferrals
- Comprehensive validation with FluentValidation
- Unit and integration tests
- Caching layer (Redis)
- Health check endpoints
- Rate limiting

## Testing

Use Swagger UI or tools like Postman/curl:

```bash
# Login
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"password"}'

# Use token in subsequent requests
curl -X GET https://localhost:5001/api/users \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Migration from Express API

See `ENDPOINT_MAPPING.md` for detailed mapping between Express and .NET endpoints. The .NET implementation follows the same business logic and authentication patterns as the Express version, with improvements in type safety and performance.
