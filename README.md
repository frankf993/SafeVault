# SafeVault — Secure Authentication Demo

SafeVault is an educational **.NET 10** project that demonstrates how to build a secure Web API for authentication and authorization.

The project covers the main security requirements of the assignment:

- Server-side input validation.
- Parameterized SQL queries to prevent SQL injection.
- XSS mitigation through input allowlists and Razor HTML encoding.
- Secure password hashing with `PasswordHasher<TUser>`.
- Cookie-based authentication.
- Role-based access control (RBAC) with `User` and `Admin` roles.
- NUnit tests for SQL injection, XSS, and password-hashing scenarios.

> **Educational purpose:** This project is intended for coursework. It should not be considered production-ready without the additional controls listed in [Limitations and improvements](#limitations-and-improvements).

## Architecture

```text
SafeVault/
├─ SafeVault.sln
├─ README.md
├─ src/
│  ├─ SafeVault.Api/       # ASP.NET Core Web API (.NET 10)
│  └─ SafeVault.Web/       # Razor Pages client (.NET 10)
└─ tests/
   └─ SafeVault.Tests/     # NUnit tests
```

| Project | Responsibility |
|---|---|
| `SafeVault.Api` | Registration, login, logout, cookies, roles, database access, and protected endpoints |
| `SafeVault.Web` | Razor Pages user interface for registration, login, dashboard, and the admin area |
| `SafeVault.Tests` | Automated tests for validation, malicious payloads, and password hashing |

Application flow:

```text
Browser → SafeVault.Web (Razor Pages) → SafeVault.Api → SQL Server LocalDB
```

## Prerequisites

- Windows 10/11.
- [.NET 10 SDK](https://dotnet.microsoft.com/download).
- SQL Server LocalDB.
- `sqlcmd` is recommended to initialize and query the database from the command line.
- A modern browser.

Check the installed .NET SDK:

```powershell
dotnet --version
```

Check the available SQL Server LocalDB instances:

```powershell
SqlLocalDB info
```

The application uses the following LocalDB instance:

```text
MSSQLLocalDB
```

## Database setup

The API uses SQL Server LocalDB with Windows Integrated Authentication. The connection string is configured in:

```text
src/SafeVault.Api/appsettings.json
```

Expected configuration:

```json
{
  "ConnectionStrings": {
    "SafeVault": "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=SafeVault;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 1. Start LocalDB

Open PowerShell and start the default instance:

```powershell
SqlLocalDB start MSSQLLocalDB
```

If the instance does not exist, create and start it:

```powershell
SqlLocalDB create MSSQLLocalDB
SqlLocalDB start MSSQLLocalDB
```

### 2. Create the SafeVault database

Run this command in PowerShell:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "IF DB_ID(N'SafeVault') IS NULL CREATE DATABASE SafeVault"
```

The command is idempotent: if the database already exists, it does nothing.

Verify that the database was created:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -Q "SELECT name FROM sys.databases WHERE name = N'SafeVault'"
```

Expected output:

```text
name
--------------------------------
SafeVault
```

> Do not create the `dbo.Users` table manually. `SafeVault.Api` creates it automatically on first startup through `SafeVaultDatabase.InitializeAsync`.

### 3. Verify the table after first startup

After starting the API at least once, verify the schema:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d SafeVault -Q "SELECT name FROM sys.tables"
```

Expected output includes:

```text
Users
```

## Installation

Clone the repository:

```powershell
git clone <REPOSITORY-URL>
cd SafeVault
```

Restore packages and build the solution:

```powershell
dotnet restore
dotnet build
```

## Running the application

### 1. Start the Web API

From the repository root:

```powershell
dotnet run --project src\SafeVault.Api
```

The API is configured to run at:

```text
https://localhost:7001
```

If native OpenAPI is enabled, the API document is available in the Development environment at:

```text
https://localhost:7001/openapi/v1.json
```

### 2. Start the Razor Pages client

Open a second terminal from the repository root:

```powershell
dotnet run --project src\SafeVault.Web
```

Open this URL in the browser:

```text
https://localhost:5001
```

## API endpoints

| Method | Endpoint | Authorization | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Anonymous | Registers a new user with the initial `User` role |
| `POST` | `/api/auth/login` | Anonymous | Validates username/password and issues the authentication cookie |
| `POST` | `/api/auth/logout` | Authenticated user | Removes the authentication cookie |
| `GET` | `/api/auth/me` | Authenticated user | Returns the current username and role |
| `GET` | `/api/admin/dashboard` | `Admin` role only | Returns a response from the protected administrative area |

### Example: register a user

```powershell
$body = @{
    username = "francesco"
    email = "francesco@example.com"
    password = "PasswordSicura123!"
} | ConvertTo-Json

Invoke-RestMethod `
  -Uri "https://localhost:7001/api/auth/register" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body `
  -SkipCertificateCheck
```

Expected response:

```json
{
  "message": "User registered."
}
```

## Roles and authorization

Every account created through `/api/auth/register` initially receives this role:

```text
User
```

To test the administrative area, promote an existing user to `Admin`:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -d SafeVault -Q "UPDATE dbo.Users SET Role = N'Admin' WHERE Username = N'francesco'"
```

After changing the role:

1. Log out.
2. Log in again to refresh the claims stored in the authentication cookie.
3. Open the admin area or call `GET /api/admin/dashboard`.

A user with the `User` role receives `403 Forbidden` when attempting to access the administrative endpoint.

## Automated tests

Run the complete test suite from the repository root:

```powershell
dotnet test
```

The NUnit test suite verifies:

- Rejection of usernames containing SQL injection payloads.
- Rejection of email values containing XSS payloads.
- That the plain-text password is not present in the stored hash.
- Successful verification of a correct password.
- Rejection of an incorrect password.

## Manual security checks

### SQL injection

Send a SQL injection payload in the username field:

```powershell
$body = @{
    username = "admin' OR '1'='1"
    email = "attacker@example.com"
    password = "PasswordSicura123!"
} | ConvertTo-Json

Invoke-RestMethod `
  -Uri "https://localhost:7001/api/auth/register" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body `
  -SkipCertificateCheck
```

Expected behavior: `400 Bad Request` with a response similar to:

```json
{
  "error": "Invalid username."
}
```

### Cross-site scripting (XSS)

Send an XSS payload in the email field:

```powershell
$body = @{
    username = "xssuser"
    email = "<script>alert('XSS')</script>"
    password = "PasswordSicura123!"
} | ConvertTo-Json

Invoke-RestMethod `
  -Uri "https://localhost:7001/api/auth/register" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body `
  -SkipCertificateCheck
```

Expected behavior: `400 Bad Request` with a response similar to:

```json
{
  "error": "Invalid email address."
}
```

## Implemented security controls

### Input validation

Inputs are validated server-side before repository access:

- Username: only alphanumeric characters, `_`, `-`, and `.` are accepted; length must be between 3 and 50 characters.
- Email: format validation and a maximum length of 100 characters.
- Password: a minimum length of 12 characters.

### SQL injection prevention

The repository does not concatenate user-provided data into SQL statements. It uses typed SQL parameters:

```csharp
const string sql = """
    SELECT UserId, Username, Email, PasswordHash, Role
    FROM dbo.Users
    WHERE Username = @Username;
    """;

command.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = username;
```

As a result, user input is handled as data and cannot alter the structure of the SQL query.

### XSS prevention

- Data is validated on the server.
- Razor automatically HTML-encodes `@...` expressions.
- The client does not use `innerHTML` to render user-controlled input.

### Password security

- Passwords are never stored as plain text.
- The project uses `PasswordHasher<User>`.
- The database stores only the `PasswordHash` value.

### Authentication and authorization

- Authentication uses HTTP-only cookies.
- The cookie is configured with `SecurePolicy = Always` and `SameSite = None` for the HTTPS cross-origin demo.
- The `AdminOnly` policy requires the `Admin` role.
- `/api/admin/dashboard` is protected with `[Authorize(Policy = "AdminOnly")]`.

## Limitations and improvements

The project is intentionally minimal for educational purposes. A production implementation should add at least:

- CSRF protection for state-changing operations.
- Rate limiting for login and registration endpoints.
- Temporary account lockout after repeated failed sign-in attempts.
- Email confirmation.
- Password reset and recovery flows.
- A stronger password policy and compromised-password checks.
- Secret management via User Secrets, environment variables, or a secret vault.
- Structured logging, auditing, and monitoring.
- Centralized exception handling and standardized error responses.
- Versioned database migrations, for example through EF Core migrations.
- A stricter separation between client and API, such as a BFF pattern or JWT/OIDC for real cross-origin scenarios.

## License

This project was created for educational purposes.
