# PharmaFrenzy Backend

ASP.NET Core Web API for the PharmaFrenzy project.

## Stack

- .NET 6
- ASP.NET Core Web API
- Entity Framework Core
- MySQL
- ASP.NET Identity
- JWT authentication

## Project Structure

- `Api/` - backend source code
- `IdentityApp.sln` - solution file

## Local Setup

1. Install .NET 6 SDK and MySQL.
2. Update `Api/appsettings.Development.json` with your local values.
3. Restore dependencies:

```bash
dotnet restore IdentityApp.sln
```

4. Run migrations and start the API:

```bash
dotnet run --project Api/Api.csproj
```

## Important

- The config files in this repo contain placeholders only.
- Do not commit real database passwords, JWT secrets, or Mailjet API keys.
