# NCBA.DCL - .NET 8 API

This is a .NET 8 translation of the Express.js DCL (Document Checklist) Backend API. It manages document checklists and deferrals in a loan processing workflow using Entity Framework Core with MySQL.

## Features

- **JWT Authentication** - Secure token-based authentication
- **Role-Based Access Control** - Admin, RM, CoCreator, CoChecker, Customer roles
- **Entity Framework Core** - ORM with MySQL (easily switchable to other SQL databases)
- **File Upload** - Document file management
- **RESTful API** - Standard HTTP methods and status codes
- **Swagger/OpenAPI** - Interactive API documentation

## Prerequisites

- .NET 8 SDK
- MySQL Server 8.0 or higher
- IDE (Visual Studio, VS Code, or Rider)

## Setup Instructions

### 1. Install EF Core Tools (Required for Migrations)

First, check if EF Core tools are installed:

```bash
dotnet tool list -g
```

If `dotnet-ef` is not listed, install it globally:

```bash
dotnet tool install --global dotnet-ef
```

To update to the latest version if already installed:

```bash
dotnet tool update --global dotnet-ef
```

Verify installation:

```bash
dotnet ef --version
```

### 2. Configure Database

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ncba_dcl;User=root;Password=your_password;"
  }
}
```

### 3. Install Dependencies

```bash
dotnet restore
```

### 4. Create Database Migration

```bash
dotnet ef migrations add InitialCreate
```

**Note**: If you get an error about `dotnet-ef` not being recognized, make sure you completed step 1 above and restart your terminal.

### 5. Apply Migration to Database

```bash
dotnet ef database update
```

### 6. Run the Application

Development:
```bash
dotnet run
```

Or watch mode (auto-reload on changes):
```bash
dotnet watch run
```

## Project Structure

```
NCBA.DCL/
├── Controllers/          # API endpoints
│   ├── AuthController.cs
│   ├── UserController.cs
│   ├── ChecklistController.cs
│   ├── DeferralController.cs
│   └── UserLogController.cs
├── Data/                 # Database context
│   └── ApplicationDbContext.cs
├── Models/               # Entity models
│   ├── User.cs
│   ├── Checklist.cs
│   ├── Document.cs
│   ├── Deferral.cs
│   ├── UserLog.cs
│   └── Notification.cs
├── DTOs/                 # Data Transfer Objects
│   └── AuthDTOs.cs
├── Helpers/              # Utility classes
│   ├── JwtTokenGenerator.cs
│   ├── PasswordHasher.cs
│   └── FileUploadHelper.cs
├── Middleware/           # Custom middleware
│   └── RoleAuthorizeAttribute.cs
├── Services/             # Business logic (to be expanded)
├── Validators/           # Input validation (to be expanded)
└── Program.cs            # Application entry point
```

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register admin user
- `POST /api/auth/login` - Login user

### Users
- `GET /api/users` - Get all users
- `POST /api/users` - Create user (admin only)
- `GET /api/users/stats` - Get user statistics
- `PUT /api/users/{id}/active` - Toggle user active status
- `PUT /api/users/{id}/role` - Change user role

### Checklists
- `POST /api/checklist` - Create checklist
- `GET /api/checklist` - Get all checklists
- `GET /api/checklist/{id}` - Get checklist by ID
- `GET /api/checklist/dcl/{dclNo}` - Get checklist by DCL number
- `PUT /api/checklist/{id}` - Update checklist
- `POST /api/checklist/{id}/documents` - Add document to checklist
- `PATCH /api/checklist/{id}/documents/{docId}` - Update document
- `DELETE /api/checklist/{id}/documents/{docId}` - Delete document
- `PATCH /api/checklist/{id}/checklist-status` - Update checklist status
- `GET /api/checklist/{checklistId}/comments` - Get checklist comments

### Deferrals
- `POST /api/deferrals` - Create deferral
- `GET /api/deferrals/pending` - Get pending deferrals
- `GET /api/deferrals/{id}` - Get deferral by ID
- `PUT /api/deferrals/{id}/facilities` - Update facilities
- `POST /api/deferrals/{id}/documents` - Add document
- `DELETE /api/deferrals/{id}/documents/{docId}` - Delete document
- `PUT /api/deferrals/{id}/approvers` - Set approvers
- `PUT /api/deferrals/{id}/approve` - Approve deferral
- `PUT /api/deferrals/{id}/reject` - Reject deferral
- `GET /api/deferrals/{id}/pdf` - Generate PDF (TODO)

### User Logs
- `GET /api/user-logs` - Get all user activity logs

## Database Migration Guide

**Prerequisites**: Ensure EF Core tools are installed (see Setup Instructions step 1)

### Create a new migration
```bash
dotnet ef migrations add <MigrationName>
```

### Apply migrations
```bash
dotnet ef database update
```

### Rollback migration
```bash
dotnet ef database update <PreviousMigrationName>
```

### Remove last migration (if not applied)
```bash
dotnet ef migrations remove
```

### List all migrations
```bash
dotnet ef migrations list
```

### Troubleshooting Migration Issues

If `dotnet ef` commands fail:

1. **Check EF tools installation**:
   ```bash
   dotnet tool list -g
   ```
   If not installed, run:
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **Verify you're in the project directory**:
   ```bash
   cd D:\raph\NCBA.DCL
   ```

3. **Check that the project builds**:
   ```bash
   dotnet build
   ```

4. **Ensure MySQL server is running** before applying migrations

## Switching Database Providers

The application is configured to use MySQL via Pomelo.EntityFrameworkCore.MySql, but can easily switch to other providers:

### SQL Server
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

Update `Program.cs`:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

### PostgreSQL
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

Update `Program.cs`:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
```

## Environment Variables

Create a `.env` file or use User Secrets for sensitive data:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ncba_dcl;User=root;Password=your_password;"
dotnet user-secrets set "JwtSettings:Secret" "your-secret-key"
```

## Testing

### Using Swagger
1. Run the application
2. Navigate to `https://localhost:5001/swagger`
3. Use the interactive UI to test endpoints

### Using curl

Login:
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"password123"}'
```

Get Users (with token):
```bash
curl -X GET https://localhost:5001/api/users \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Development Notes

- All entity models use GUIDs as primary keys
- Timestamps (CreatedAt, UpdatedAt) are automatically managed
- Password hashing uses BCrypt with work factor of 10
- JWT tokens expire after 7 days (configurable in appsettings.json)
- File uploads are stored in the `uploads/` directory

## TODO / Future Enhancements

1. Complete all RM-specific endpoints (queue management, notifications)
2. Complete all Checker-specific endpoints (active DCLs, my queue, reports)
3. Complete all CoCreator-specific endpoints (submit to RM/CoChecker)
4. Implement PDF generation for deferrals (using iTextSharp or PdfSharpCore)
5. Add comprehensive input validation with FluentValidation
6. Implement unit tests with xUnit
7. Add logging with Serilog
8. Implement caching with Redis
9. Add health check endpoints
10. Implement rate limiting

## License

Proprietary - NCBA
