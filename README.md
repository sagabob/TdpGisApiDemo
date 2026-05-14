# TDP GIS API Demo

A comprehensive Geographic Information System (GIS) application demonstrating enterprise-grade geospatial data management with a modern full-stack architecture. This project includes a robust .NET backend API and a React-based interactive mapping frontend.

## Project Overview

The TDP GIS API Demo showcases best practices for building scalable GIS applications with:
- Enterprise authentication (Azure AD integration)
- RESTful API with comprehensive documentation
- Interactive mapping and spatial queries
- Multi-database support (SQL Server, MongoDB)
- Containerization and cloud deployment ready

**Live Demo**: [https://tdp-gis-api-demo.vercel.app/](https://tdp-gis-api-demo.vercel.app/)

## Project Structure

```
TdpGisApiDemo/
├── _Backend/                          # .NET Core backend API
│   ├── README.md                      # Backend documentation
│   ├── TdpGis.Api/                    # Main API project
│   ├── TdpGis.Application/            # Business logic
│   ├── TdpGis.Infrastructure/         # Data access & persistence
│   ├── TdpGis.Domain/                 # Domain models
│   ├── TdpGis.Endpoints/              # MVC endpoints
│   ├── TdpGis.LocalApi/               # Local development API
│   ├── TdpGis.AdminApplication/       # Admin functionality
│   ├── tests/                         # Unit and integration tests
│   └── TdpGis.sln                     # Solution file
│
├── _Frontend/                         # React frontend application
│   ├── README.md                      # Frontend documentation
│   ├── src/                           # Source code
│   │   ├── components/                # React components
│   │   ├── api/                       # API integration
│   │   ├── contexts/                  # State management
│   │   ├── hooks/                     # Custom hooks
│   │   ├── lib/                       # Utilities
│   │   └── types/                     # TypeScript types
│   ├── public/                        # Static assets
│   ├── package.json                   # Dependencies
│   └── vite.config.ts                 # Vite configuration
│
└── README.md                          # This file
```

## Quick Start

### Prerequisites

**For Backend:**
- .NET 10.0 SDK or later
- SQL Server or compatible database
- MongoDB (for metadata)
- Visual Studio 2024 or VS Code

**For Frontend:**
- Node.js 18.0 or later
- npm 9.0 or later
- Mapbox GL access token
- Modern web browser

### Backend Setup

1. Navigate to the backend directory:
```bash
cd _Backend
```

2. Follow the [Backend README](_Backend/README.md) for detailed setup instructions

3. Quick commands:
```bash
# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update -p TdpGis.Infrastructure -s TdpGis.Api

# Run the API
dotnet run --project TdpGis.Api
```

The API will be available at `https://localhost:7000`

### Frontend Setup

1. Navigate to the frontend directory:
```bash
cd _Frontend
```

2. Follow the [Frontend README](_Frontend/README.md) for detailed setup instructions

3. Quick commands:
```bash
# Install dependencies
npm install

# Start development server
npm run dev
```

The frontend will be available at `http://localhost:5173`

## Key Features

### Backend
- ✅ RESTful API with FastEndpoints
- ✅ Azure AD authentication (Entra ID)
- ✅ Swagger/OpenAPI documentation
- ✅ Multi-layer security (Bearer tokens + workspace tokens)
- ✅ Entity Framework Core ORM
- ✅ MongoDB integration
- ✅ Health check endpoints
- ✅ CORS support
- ✅ Docker containerization
- ✅ Unit and integration tests

### Frontend
- ✅ Interactive Mapbox GL mapping
- ✅ Real-time geographic search
- ✅ Workspace entity management
- ✅ Azure AD authentication
- ✅ Responsive design (mobile + desktop)
- ✅ TypeScript for type safety
- ✅ Vite for fast builds
- ✅ Tailwind CSS styling
- ✅ BFF/server-proxy pattern

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Frontend (React + Vite)                 │
│  - Interactive maps, search, entity management              │
│  - Mapbox GL visualization                                  │
│  - Azure AD authentication                                  │
└────────────────────┬────────────────────────────────────────┘
                     │ HTTPS/REST
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Backend API (.NET 10 + FastEndpoints)          │
│  - OpenAPI/Swagger documentation                            │
│  - Authentication & Authorization                           │
│  - GIS query endpoints                                      │
└────────────┬─────────────────────────────────────┬──────────┘
             │                                     │
             ▼                                     ▼
    ┌──────────────────┐              ┌──────────────────┐
    │  SQL Database    │              │    MongoDB       │
    │  (EF Core ORM)   │              │   (Metadata)     │
    └──────────────────┘              └──────────────────┘
```

## API Documentation

Once the backend is running, access the API documentation:
- **Swagger UI**: `https://localhost:7000/swagger`
- **OpenAPI JSON**: `https://localhost:7000/swagger/v1/swagger.json`

## Authentication

### Two-Layer Authentication Model

1. **Microsoft Entra ID (Azure AD)**
   - JWT Bearer tokens in `Authorization: Bearer <token>`
   - Requires `TdpGisApi.Access` app role
   - For all authenticated API endpoints

2. **Workspace Access Token**
   - Opaque token in `X-Access-Token` header
   - Validated against the database
   - For GIS-specific operations

## Environment Configuration

### Backend Configuration (appsettings.json)

```json
{
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

### Frontend Configuration (.env.local)

```env
VITE_API_URL=http://localhost:7000
VITE_MAPBOX_TOKEN=your-mapbox-access-token
VITE_AUTH_TENANT_ID=your-azure-tenant-id
VITE_AUTH_CLIENT_ID=your-azure-client-id
```

## Development Workflow

1. **Local Development**
   - Backend: `dotnet run --project TdpGis.Api`
   - Frontend: `npm run dev`
   - Both will enable hot reload/auto-refresh

2. **Code Quality**
   - Backend: Use Visual Studio's built-in tools or run tests
   - Frontend: `npm run lint` and `npm run lint -- --fix`

3. **Testing**
   ```bash
   # Backend
   dotnet test

   # Frontend (if tests configured)
   npm test
   ```

4. **Building**
   ```bash
   # Backend
   dotnet publish -c Release -o ./publish

   # Frontend
   npm run build
   ```

## Docker Deployment

### Build Backend Docker Image

```bash
cd _Backend
docker build -f TdpGis.Api/Dockerfile -t tdp-gis-api:latest .
```

### Run Backend Container

```bash
docker run -p 7000:80 \
  -e AzureAd:TenantId=<tenant-id> \
  -e AzureAd:ClientId=<client-id> \
  -e ConnectionStrings:Database=<connection-string> \
  tdp-gis-api:latest
```

## Deployment Options

### Backend
- **Docker**: Containerized deployment to any container registry
- **Azure App Service**: Fully managed hosting
- **AWS EC2/ECS**: Virtual machines or container orchestration
- **On-premises**: Standard .NET deployment

### Frontend
- **Vercel**: Recommended for Vite apps (includes serverless routes)
- **Netlify**: Alternative static hosting with CI/CD
- **AWS S3 + CloudFront**: Static site with CDN
- **Azure Static Web Apps**: Azure-native solution
- **Docker**: Containerized deployment

## Troubleshooting

### Backend Issues

1. **Database Connection Failed**
   - Verify connection string in `appsettings.json`
   - Ensure database server is running
   - Check firewall rules

2. **Authentication Errors**
   - Verify Azure AD credentials
   - Check app role assignments
   - Validate token claims

### Frontend Issues

1. **Mapbox Map Not Showing**
   - Verify Mapbox token in `.env.local`
   - Check token permissions
   - Inspect browser console for errors

2. **API Connection Failed**
   - Verify backend is running
   - Check `VITE_API_URL` environment variable
   - Verify CORS settings on backend

## Project Statistics

- **Backend**: 4 main projects + 1 admin project + 3 test projects
- **Frontend**: React + TypeScript with modern tooling
- **Database Support**: SQL Server, MongoDB
- **API Documentation**: OpenAPI 3.0 (Swagger)
- **Authentication**: Microsoft Entra ID + Workspace tokens

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Documentation

- [Backend Documentation](_Backend/README.md) - Comprehensive backend setup and API details
- [Frontend Documentation](_Frontend/README.md) - Frontend setup and component guide
- [Map Camera Sync](_Frontend/docs/map-camera-sync.md) - Map camera synchronization details

## Support

For issues and questions:
1. Check existing documentation in the respective README files
2. Review API documentation in Swagger UI
3. Check component source code comments
4. Contact the development team

## License

[Add your license information here]

## Key Technologies

### Backend
- .NET 10
- FastEndpoints
- Entity Framework Core
- MongoDB
- Microsoft Entra ID
- NSwag/Swagger

### Frontend
- React 19
- TypeScript
- Vite
- Mapbox GL
- Tailwind CSS
- Axios
- React Map GL

## Additional Resources

### Backend
- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [Microsoft Entra ID](https://learn.microsoft.com/entra/identity/)

### Frontend
- [React Documentation](https://react.dev/)
- [Vite Documentation](https://vitejs.dev/)
- [Mapbox GL Documentation](https://docs.mapbox.com/mapbox-gl-js/)
- [Tailwind CSS Documentation](https://tailwindcss.com/)

## Getting Help

- **Backend**: See [Backend README](_Backend/README.md)
- **Frontend**: See [Frontend README](_Frontend/README.md)
- **API**: Visit Swagger UI at `https://localhost:7000/swagger` (when running)
- **Issues**: Check GitHub Issues or create a new one

---

**Last Updated**: May 2026  
**Version**: 1.0.0
