# Role-Based Access Control (RBAC) Implementation

## Overview
This document describes the role-based access control implementation for the GoodBooks application API.

## Roles

### 1. SystemAdministrators
- **Full administrative access** to all system resources
- Can perform **all CRUD operations** (Create, Read, Update, Delete)
- Access to:
  - User management (create, update, delete users)
  - Role management
  - System configuration
  - All financial operations
  - Tax management (create, update, delete)
  - Account deletion
  - Database initialization and clearing

### 2. GeneralUsers
- **Limited access** for regular users
- Can perform:
  - **Read operations** on most resources
  - **Create/Update operations** for business transactions (sales, purchases)
  - View financial reports and accounts
  - View tax information
- **Cannot perform**:
  - User or role management
  - Delete operations on critical resources
  - System configuration changes
  - Tax rate modifications

## Default Users

### Administrator
- **Email**: `admin@accountgo.ph`
- **Password**: `P@ssword1`
- **Role**: SystemAdministrators
- **Capabilities**: Full system access

### General User (Test)
- **Email**: `user@accountgo.ph`
- **Password**: `P@ssword1`
- **Role**: GeneralUsers
- **Capabilities**: Limited access as described above

## Controller Security

### AdministrationController
- **Authorization**: `[Authorize(Roles = Roles.SystemAdministrator)]`
- **Endpoints**: All require admin role
- **Operations**: 
  - Database setup/clear
  - User management
  - Audit logs
  - Company settings

### AccountController
- **SignIn**: `[AllowAnonymous]` - Open to all
- **AddNewUser**: `[Authorize(Roles = Roles.SystemAdministrator)]` - Admin only

### TaxController
- **Base**: `[Authorize(Roles = Roles.AnyUser)]` - Requires authentication
- **GET operations**: All authenticated users
- **POST/PUT/DELETE operations**: `[Authorize(Roles = Roles.SystemAdministrator)]` - Admin only

### FinancialsController
- **Base**: `[Authorize(Roles = Roles.AnyUser)]` - Requires authentication
- **GET operations**: All authenticated users
- **DELETE operations**: `[Authorize(Roles = Roles.SystemAdministrator)]` - Admin only

## Testing RBAC

### Test as Administrator
1. Login with `admin@accountgo.ph` / `P@ssword1`
2. Try accessing admin endpoints (should succeed):
   - `GET /api/administration/users`
   - `POST /api/account/addnewuser`
   - `DELETE /api/tax/tax/1`

### Test as General User
1. Login with `user@accountgo.ph` / `P@ssword1`
2. Try accessing read endpoints (should succeed):
   - `GET /api/financials/accounts`
   - `GET /api/tax/taxes`
3. Try accessing admin endpoints (should return 403 Forbidden):
   - `GET /api/administration/users`
   - `DELETE /api/tax/tax/1`

## Implementation Files

### New Files Created
- `src/Api/Constants/Roles.cs` - Role constant definitions

### Modified Files
- `src/Api/Controllers/AdministrationController.cs` - Admin-only access
- `src/Api/Controllers/AccountController.cs` - Mixed access levels
- `src/Api/Controllers/TaxController.cs` - Secured write operations
- `src/Api/Controllers/FinancialsController.cs` - Secured delete operations
- `src/Api/Data/Seed/DatabaseSeeder.cs` - Creates both admin and general user

## Security Best Practices Implemented

1. ✅ **Principle of Least Privilege**: Users only get minimum required permissions
2. ✅ **Role-Based Access**: Permissions assigned via roles, not individual users
3. ✅ **Authorization at Controller Level**: Security enforced at API layer
4. ✅ **Separation of Duties**: Admins vs regular users
5. ✅ **Read vs Write Separation**: General users can read but not modify critical data

## Future Enhancements

Consider implementing:
- More granular roles (e.g., AccountingManager, SalesRep, PurchasingAgent)
- Resource-based authorization (users can only edit their own records)
- Audit logging for authorization failures
- Time-based access controls
- IP-based restrictions for admin operations
