# Express to .NET API Endpoint Mapping

This document maps the Express.js endpoints to their .NET equivalents.

## Authentication Endpoints

| Express (Node.js) | .NET 8 | Notes |
|------------------|---------|-------|
| `POST /api/auth/register` | `POST /api/auth/register` | ✅ Implemented |
| `POST /api/auth/login` | `POST /api/auth/login` | ✅ Implemented |

## User Management Endpoints

| Express (Node.js) | .NET 8 | Notes |
|------------------|---------|-------|
| `POST /api/users` | `POST /api/users` | ✅ Implemented |
| `GET /api/users` | `GET /api/users` | ✅ Implemented |
| `GET /api/users/stats` | `GET /api/users/stats` | ✅ Implemented |
| `PUT /api/users/:id/active` | `PUT /api/users/{id}/active` | ✅ Implemented |
| `PUT /api/users/:id/role` | `PUT /api/users/{id}/role` | ✅ Implemented |

## Checklist Endpoints (CoCreator)

| Express (Node.js) | .NET 8 | Notes |
|------------------|---------|-------|
| `POST /api/checklist` | `POST /api/checklist` | ✅ Implemented |
| `GET /api/checklist` | `GET /api/checklist` | ✅ Implemented (combined cocreatorChecklist) |
| `GET /api/checklist/:id` | `GET /api/checklist/{id}` | ✅ Implemented |
| `GET /api/checklist/dcl/:dclNo` | `GET /api/checklist/dcl/{dclNo}` | ✅ Implemented |
| `PUT /api/checklist/:id` | `PUT /api/checklist/{id}` | ✅ Implemented |
| `GET /api/checklist/:checklistId/comments` | `GET /api/checklist/{checklistId}/comments` | ✅ Implemented |
| `GET /api/cocreatorChecklist/search/customer` | `GET /api/checklist/search/customer` | ⚠️ TODO - Need to implement |
| `GET /api/cocreatorChecklist/creator/:creatorId` | `GET /api/checklist/creator/{creatorId}` | ⚠️ TODO - Need to implement |
| `PUT /api/cocreatorChecklist/:id/co-create` | `PUT /api/checklist/{id}/co-create` | ⚠️ TODO - Co-create review |
| `PUT /api/cocreatorChecklist/:id/co-check` | `PUT /api/checklist/{id}/co-check` | ⚠️ TODO - Co-check approval |
| `PUT /api/cocreatorChecklist/update-document` | `PUT /api/checklist/update-document` | ⚠️ TODO - Admin doc override |
| `PATCH /api/cocreatorChecklist/update-status` | `PATCH /api/checklist/update-status` | ⚠️ TODO - Status update |
| `POST /api/cocreatorChecklist/:id/submit-to-rm` | `POST /api/checklist/{id}/submit-to-rm` | ⚠️ TODO - Submit to RM |
| `POST /api/cocreatorChecklist/:id/submit-to-cochecker` | `POST /api/checklist/{id}/submit-to-cochecker` | ⚠️ TODO - Submit to CoChecker |
| `PATCH /api/cocreatorChecklist/:checklistId/checklist-status` | `PATCH /api/checklist/{checklistId}/checklist-status` | ✅ Implemented |
| `POST /api/cocreatorChecklist/:id/documents` | `POST /api/checklist/{id}/documents` | ✅ Implemented |
| `PATCH /api/cocreatorChecklist/:id/documents/:docId` | `PATCH /api/checklist/{id}/documents/{docId}` | ✅ Implemented |
| `DELETE /api/cocreatorChecklist/:id/documents/:docId` | `DELETE /api/checklist/{id}/documents/{docId}` | ✅ Implemented |
| `POST /api/cocreatorChecklist/:id/documents/:docId/upload` | `POST /api/checklist/{id}/documents/{docId}/upload` | ⚠️ TODO - File upload |
| `POST /api/cocreatorChecklist/:id/upload` | `POST /api/checklist/{id}/upload` | ⚠️ TODO - Multiple file upload |
| `GET /api/cocreatorChecklist/:id/download` | `GET /api/checklist/{id}/download` | ⚠️ TODO - Download checklist |
| `GET /api/cocreatorChecklist/cocreator/active` | `GET /api/checklist/cocreator/active` | ⚠️ TODO - Active checklists |

## RM (Relationship Manager) Endpoints

| Express (Node.js) | .NET 8 | Notes |
|------------------|---------|-------|
| `GET /api/rmChecklist/:rmId/myqueue` | `GET /api/rm/{rmId}/myqueue` | ⚠️ TODO - Create RMController |
| `POST /api/rmChecklist/rm-submit-to-co-creator` | `POST /api/rm/submit-to-cocreator` | ⚠️ TODO - Submit to CoCreator |
| `GET /api/rmChecklist/completed/rm/:rmId` | `GET /api/rm/completed/{rmId}` | ⚠️ TODO - Completed DCLs |
| `DELETE /api/rmChecklist/:id` | `DELETE /api/rm/{id}` | ⚠️ TODO - Delete DCL |
| `GET /api/rmChecklist/:id` | `GET /api/rm/{id}` | ⚠️ TODO - Get by ID |
| `DELETE /api/rmChecklist/:checklistId/document/:documentId` | `DELETE /api/rm/{checklistId}/document/{documentId}` | ⚠️ TODO - Delete doc file |
| `GET /api/rmChecklist/notifications/rm` | `GET /api/rm/notifications` | ⚠️ TODO - Get notifications |
| `PUT /api/rmChecklist/notifications/rm/:notificationId` | `PUT /api/rm/notifications/{notificationId}` | ⚠️ TODO - Mark as read |

## Checker Endpoints

| Express (Node.js) | .NET 8 | Notes |
|------------------|---------|-------|
| `GET /api/checkerChecklist/active-dcls` | `GET /api/checker/active-dcls` | ⚠️ TODO - Create CheckerController |
| `GET /api/checkerChecklist/my-queue/:checkerId` | `GET /api/checker/my-queue/{checkerId}` | ⚠️ TODO - My queue |
| `GET /api/checkerChecklist/completed/:checkerId` | `GET /api/checker/completed/{checkerId}` | ⚠️ TODO - Completed DCLs |
| `GET /api/checkerChecklist/dcl/:id` | `GET /api/checker/dcl/{id}` | ⚠️ TODO - Get DCL |
| `PUT /api/checkerChecklist/dcl/:id` | `PUT /api/checker/dcl/{id}` | ⚠️ TODO - Update DCL status |
| `GET /api/checkerChecklist/my-queue-auto/:checkerId` | `GET /api/checker/my-queue-auto/{checkerId}` | ⚠️ TODO - Auto-move queue |
| `PATCH /api/checkerChecklist/update-status` | `PATCH /api/checker/update-status` | ⚠️ TODO - Update status |
| `GET /api/checkerChecklist/reports/:checkerId` | `GET /api/checker/reports/{checkerId}` | ⚠️ TODO - Reports |
| `PATCH /api/checkerChecklist/approve/:id` | `PATCH /api/checker/approve/{id}` | ⚠️ TODO - Approve with notification |
| `PATCH /api/checkerChecklist/reject/:id` | `PATCH /api/checker/reject/{id}` | ⚠️ TODO - Reject with notification |

## Deferral Endpoints

| Express (Node.js) | .NET 8 | Notes |
|------------------|---------|-------|
| `POST /api/deferrals` | `POST /api/deferrals` | ✅ Implemented |
| `GET /api/deferrals/pending` | `GET /api/deferrals/pending` | ✅ Implemented |
| `GET /api/deferrals/:id` | `GET /api/deferrals/{id}` | ✅ Implemented |
| `PUT /api/deferrals/:id/facilities` | `PUT /api/deferrals/{id}/facilities` | ✅ Implemented |
| `POST /api/deferrals/:id/documents` | `POST /api/deferrals/{id}/documents` | ✅ Implemented |
| `DELETE /api/deferrals/:id/documents/:docId` | `DELETE /api/deferrals/{id}/documents/{docId}` | ✅ Implemented |
| `PUT /api/deferrals/:id/approvers` | `PUT /api/deferrals/{id}/approvers` | ✅ Implemented |
| `DELETE /api/deferrals/:id/approvers/:index` | `DELETE /api/deferrals/{id}/approvers/{index}` | ⚠️ TODO - Remove approver |
| `PUT /api/deferrals/:id/approve` | `PUT /api/deferrals/{id}/approve` | ✅ Implemented |
| `PUT /api/deferrals/:id/reject` | `PUT /api/deferrals/{id}/reject` | ✅ Implemented |
| `GET /api/deferrals/:id/pdf` | `GET /api/deferrals/{id}/pdf` | ⚠️ TODO - PDF generation |

## User Logs Endpoints

| Express (Node.js) | .NET 8 | Notes |
|------------------|---------|-------|
| `GET /api/user-logs` | `GET /api/user-logs` | ✅ Implemented |

## Summary

### ✅ Fully Implemented (Core Endpoints)
- Authentication (register, login)
- User management (CRUD, stats)
- Checklist basic operations (create, read, update, delete)
- Document management (add, update, delete)
- Deferral management (create, read, update, approve, reject)
- User logs

### ⚠️ TODO (Role-Specific & Advanced Features)
- RM-specific endpoints (queue, notifications)
- Checker-specific endpoints (active DCLs, queue, reports, approve/reject)
- CoCreator workflow endpoints (submit to RM/CoChecker, review)
- File upload endpoints
- PDF generation
- Search functionality
- Download functionality

### Implementation Priority

1. **High Priority** - File upload endpoints (needed for document management)
2. **High Priority** - RM workflow endpoints (queue, submit to cocreator)
3. **High Priority** - Checker workflow endpoints (queue, approve/reject)
4. **Medium Priority** - CoCreator workflow endpoints (submit, review)
5. **Medium Priority** - Search and filter endpoints
6. **Low Priority** - PDF generation
7. **Low Priority** - Advanced reporting

## Notes on Translation

1. **Route Parameters**: Express uses `:param` while .NET uses `{param}`
2. **HTTP Methods**: Both frameworks use standard REST verbs (GET, POST, PUT, PATCH, DELETE)
3. **Authentication**: Express uses middleware `protect`, .NET uses `[Authorize]` attribute
4. **Role Authorization**: Express uses `authorizeRoles(...)`, .NET uses `[RoleAuthorize(...)]` custom attribute
5. **Request Body**: Express uses `req.body`, .NET uses `[FromBody]` parameter binding
6. **Path Parameters**: Express uses `req.params`, .NET uses method parameters with `{param}` in route
7. **Query Parameters**: Express uses `req.query`, .NET uses `[FromQuery]` parameter binding
8. **File Upload**: Express uses multer middleware, .NET uses `IFormFile`

## Code Patterns Comparison

### Express Pattern
```javascript
router.post('/:id/documents',
  protect,
  authorizeRoles('cocreator', 'rm'),
  addDocument
);
```

### .NET Pattern
```csharp
[HttpPost("{id}/documents")]
[RoleAuthorize(UserRole.CoCreator, UserRole.RM)]
public async Task<IActionResult> AddDocument(Guid id, [FromBody] AddDocumentRequest request)
{
    // Implementation
}
```
