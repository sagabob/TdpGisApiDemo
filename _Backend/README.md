# TDP GIS API Backend

A comprehensive Geographic Information System (GIS) API built with ASP.NET Core, designed to manage spatial data, workspace configurations, and GIS queries with enterprise-grade security and scalability.

## Overview

The TDP GIS API backend provides a robust REST API for managing geospatial data and configurations. It features multi-layered authentication, MongoDB integration for metadata, and Entity Framework Core for relational data management.

## Architecture

The backend follows a layered architecture:

- **TdpGis.Api** - HTTP API layer with FastEndpoints, OpenAPI/Swagger documentation, and authentication
- **TdpGis.Endpoints** - Admin UI with MVC controllers and views for administrative functionality
- **TdpGis.LocalApi** - Local development API with database services
- **TdpGis.Application** - Business logic and application services
- **TdpGis.Infrastructure** - Data persistence, configurations, and external integrations
- **TdpGis.Domain** - Core domain models and entities
- **TdpGis.AdminApplication** - Administrative services and configuration models for the admin UI

## Technology Stack

- **Framework**: .NET 10.0
- **API Framework**: FastEndpoints 8.1.0
- **Authentication**: Microsoft Entra ID (Azure AD) with JWT Bearer tokens
- **Database**: Entity Framework Core + MongoDB support
- **Documentation**: NSwag/OpenAPI with Swagger UI
- **Health Checks**: Built-in health check endpoints with EF Core integration
- **Docker**: Multi-stage Docker builds supported

## Prerequisites

- .NET 10.0 SDK or later
- Visual Studio 2024 or Visual Studio Code with C# extensions
- Database connection string (SQL Server or compatible)
- MongoDB connection string (for metadata storage)
- Azure AD application registration (for authentication)

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd TdpGisApiDemo/_Backend
```

### 2. Configuration

Create or update `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "Audience": "your-audience",
    "ApiAccessAppRole": "TdpGisApi.Access"
  },
  "ConnectionStrings": {
    "Database": "Server=localhost;Database=TdpGisDb;Trusted_Connection=true;"
  }
}
```

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Database Setup

Apply Entity Framework Core migrations:

```bash
dotnet ef database update -p TdpGis.Infrastructure -s TdpGis.Api
```

### 5. Run the Applications

#### REST API

```bash
dotnet run --project TdpGis.Api
```

The API will start at `https://localhost:7000` (or the configured port).

#### Admin UI

```bash
dotnet run --project TdpGis.Endpoints
```

The Admin UI will be available at the configured port (typically `https://localhost:7001` or `http://localhost:5000`).

## API Documentation

Once running, access the Swagger UI at:
- **Swagger UI**: `https://localhost:7000/swagger`
- **OpenAPI JSON**: `https://localhost:7000/swagger/v1/swagger.json`

## Authentication

### Two-Layer Authentication Model

1. **Microsoft Entra ID (Azure AD)**
   - JWT Bearer tokens in `Authorization: Bearer <token>`
   - Token must contain the `TdpGisApi.Access` app role
   - Applied to FastEndpoints only (not blocking OpenAPI documentation)

2. **Workspace Access Token**
   - Opaque token in `X-Access-Token` header
   - Validated against the database
   - Used for GIS endpoint access

### Environment Variable for Testing

For integration tests:
```bash
set IntegrationTests:UseMockJwt=true
```

This allows the API to accept mock JWT tokens without Entra validation.

## Project Structure

```
TdpGis.Api/
├── Authentication/        # Auth handlers and policies
├── GisQuery/             # GIS query endpoints and helpers
├── Security/             # Security configurations
├── Program.cs            # Application entry point
└── appsettings*.json     # Configuration files

TdpGis.Application/
├── Abstractions/         # Interfaces and contracts
├── AppModels/           # Application data transfer objects
├── Services/            # Business logic services
└── Common/              # Common utilities

TdpGis.Infrastructure/
├── Configurations/      # EF Core model configurations
├── Mongo/               # MongoDB integration
├── Persistence/         # Data access patterns
└── Migrations/          # EF Core migrations

TdpGis.Domain/
└── GisEntities.cs       # Core domain models
```

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/TdpGis.Api.Tests
dotnet test tests/TdpGis.Application.Tests
dotnet test tests/TdpGis.Infrastructure.Tests
```

### Building

```bash
# Debug build
dotnet build

# Release build
dotnet publish -c Release -o ./publish
```

### Docker

Build a Docker image:

```bash
docker build -f TdpGis.Api/Dockerfile -t tdp-gis-api:latest .
```

Run the container:

```bash
docker run -p 7000:80 \
  -e AzureAd:TenantId=<tenant-id> \
  -e AzureAd:ClientId=<client-id> \
  -e ConnectionStrings:Database=<connection-string> \
  tdp-gis-api:latest
```

## Health Checks

Health check endpoints are available at:
- `/health` - General health status
- `/health/db` - Database connectivity check

## Key Features

- ✅ Enterprise authentication with Azure AD integration
- ✅ RESTful API with FastEndpoints
- ✅ Comprehensive Swagger/OpenAPI documentation
- ✅ Multi-database support (SQL Server, MongoDB)
- ✅ Layered architecture for scalability
- ✅ Built-in health checks
- ✅ CORS support for frontend integration
- ✅ Reverse proxy support (X-Forwarded headers)
- ✅ Unit and integration testing
- ✅ Docker containerization

## Configuration

### Connection Strings

Update `appsettings.json` or use User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:Database" "your-connection-string"
```

### Azure AD Setup

1. Register an application in Azure AD
2. Configure API permissions for the application
3. Create an app role (e.g., `TdpGisApi.Access`)
4. Update `appsettings.json` with your credentials

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Troubleshooting

### Database Connection Issues

- Verify connection string in `appsettings.json`
- Ensure database server is running and accessible
- Check firewall rules for database port access

### Authentication Failures

- Verify Azure AD tenant and client IDs
- Ensure token contains required app roles
- Check token expiration

### CORS Issues

- Configure allowed origins in `AddCors` in `Program.cs`
- Verify request headers are allowed

## Support

For issues and questions:
1. Check existing GitHub issues
2. Review API documentation in Swagger UI
3. Contact the development team

## License

[Add your license information here]

## Additional Resources

- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/ef/core/)
- [Microsoft Entra ID Documentation](https://learn.microsoft.com/entra/identity/)
- [OpenAPI Specification](https://swagger.io/specification/)
