# TDP GIS Frontend

A modern React-based Geographic Information System (GIS) web application built with TypeScript, Vite, and Mapbox GL for interactive mapping, spatial data visualization, and geospatial analysis.

## Overview

The TDP GIS Frontend provides an intuitive user interface for interacting with geospatial data. Users can search geographic features, view them on interactive maps, manage workspace entities, and perform spatial queries through a responsive and user-friendly interface.

## Technology Stack

- **Framework**: React 19.2.4
- **Language**: TypeScript 5.9.3
- **Build Tool**: Vite 8.0.1
- **Mapping**: Mapbox GL 3.21.0 + React Map GL 8.1.0
- **HTTP Client**: Axios 1.14.0
- **Styling**: Tailwind CSS 4.2.2
- **Linting**: ESLint 9.39.4
- **Code Formatting**: Prettier 3.8.1
- **Utilities**: Lodash 4.18.1

## Prerequisites

- Node.js 18.0 or later
- npm 9.0 or later (or yarn/pnpm)
- Mapbox GL access token
- Backend API running (see Backend README)

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd TdpGisApiDemo/_Frontend
```

### 2. Install Dependencies

```bash
npm install
```

### 3. Environment Configuration

Create a `.env.local` file in the project root:

```env
VITE_API_URL=http://localhost:7000
VITE_MAPBOX_TOKEN=your-mapbox-access-token
VITE_AUTH_TENANT_ID=your-azure-tenant-id
VITE_AUTH_CLIENT_ID=your-azure-client-id
```

### 4. Start Development Server

```bash
npm run dev
```

The application will start at `http://localhost:5173` (or the next available port).

## Available Scripts

### Development

```bash
# Start development server with hot reload
npm run dev

# Access the app at http://localhost:5173
```

### Building

```bash
# Type check and build for production
npm run build

# Output is in the dist/ directory
```

### Preview

```bash
# Preview the production build locally
npm run preview
```

### Code Quality

```bash
# Run ESLint to check code quality
npm run lint

# Fix ESLint issues
npm run lint -- --fix
```

## Project Structure

```
src/
├── App.tsx                 # Main application component
├── main.tsx               # Application entry point
├── index.css              # Global styles
├── api/                   # API integration
│   ├── auth/             # Authentication API calls
│   ├── gis/              # GIS API calls
│   ├── security/         # Security API endpoints
│   └── utils/            # API utilities and interceptors
├── components/           # Reusable React components
│   ├── layouts/          # Layout components (Header, Sidebar, etc.)
│   ├── maps/             # Map-related components
│   └── ...               # Other UI components
├── config/               # Configuration files
│   └── gis-config.ts     # GIS-specific configuration
├── contexts/             # React Context for state management
│   └── SearchContext.tsx # Search state context
├── hooks/                # Custom React hooks
│   └── useWorkspaceEntities.tsx  # Workspace data management
├── lib/                  # Utility libraries and helpers
├── types/                # TypeScript type definitions
└── assets/               # Static assets (images, icons)

public/                    # Static files
```

## Key Features

- ✅ Interactive Mapbox GL map visualization
- ✅ Geographic search and feature discovery
- ✅ Workspace entity management
- ✅ Azure AD authentication integration
- ✅ Real-time search functionality
- ✅ Responsive design for mobile and desktop
- ✅ Type-safe TypeScript codebase
- ✅ Modern React Hooks architecture
- ✅ Tailwind CSS for utility-first styling
- ✅ ESLint and Prettier for code quality
- ✅ BFF/server-proxy pattern for secure token handling
- ✅ Support for anonymous and authenticated users
- ✅ Workspace-aware entity filtering

## Configuration

### Mapbox GL Setup

1. Create a Mapbox account at [mapbox.com](https://www.mapbox.com)
2. Generate an access token from your account dashboard
3. Add the token to `.env.local`:

```env
VITE_MAPBOX_TOKEN=pk_your_mapbox_token
```

### Azure AD Authentication

1. Register an application in Azure AD
2. Configure redirect URIs:
   - Development: `http://localhost:5173/callback`
   - Production: `https://your-domain.com/callback`
3. Update `.env.local` with your credentials

### API Configuration

The `gis-config.ts` file contains GIS-specific settings:

```typescript
export const defaultPosition = {
  latitude: 0,
  longitude: 0,
  zoom: 12,
};
```

## State Management

### SearchContext

Manages search-related state:

```typescript
{
  GeoDataResult | null     // Search results
  GeoFeature | null        // Selected feature
  string                   // Search query
}
```

### Hooks

- `useWorkspaceEntities()` - Fetch and manage workspace entities
- Custom hooks for API calls and state management

## API Integration

API calls are organized by domain:

- **Auth API** (`api/auth/`) - Authentication and user management
- **GIS API** (`api/gis/`) - Geospatial data and queries
- **Security API** (`api/security/`) - Security and authorization

### API Routes (Frontend BFF)

- `GET /api/gis/workspace-entities` - Fetch all workspace entities
- `GET /api/gis/workspace-entity-search?entityId=...&q=...&workspaceId=...` - Search GIS data

### Example API Call

```typescript
import { searchGeoFeatures } from '@/api/gis';

const results = await searchGeoFeatures({
  query: 'building',
  bounds: { north, south, east, west },
});
```

## Component Architecture

### Map Component

The `GisMap` component wraps Mapbox GL and provides:
- Map initialization and configuration
- Layer management
- Feature interaction handlers
- Responsive design
- Markers, popups, and scale/navigation controls

### Search Component

The `SearchBar` component provides:
- Geographic feature search
- Auto-complete suggestions
- Real-time filtering
- Debounced search with `AbortController` cancellation

### Authentication Component

The `AuthLoginButton` component handles:
- Azure AD login/logout
- Token management
- Protected route access
- Session persistence

## Data Architecture

### Two Data Modes

1. **Anonymous Mode**
   - Public workspace access only
   - No authentication required
   - Limited feature availability

2. **Authenticated Mode**
   - Public + private workspace merged server-side
   - Full feature access
   - User-specific data

### BFF/Server-Proxy Pattern

The application uses a Backend-for-Frontend pattern where:
- Workspace tokens and API tokens never reach the client bundle
- All token management happens server-side
- Frontend communicates through secure API routes

## Development Best Practices

### Code Quality

1. **Type Safety**: Always use TypeScript types, avoid `any`
2. **Linting**: Run `npm run lint` before commits
3. **Formatting**: Code is auto-formatted with Prettier
4. **Comments**: Document complex logic and business rules

### Component Development

```typescript
// Use functional components with hooks
export function MyComponent(): JSX.Element {
  const [state, setState] = useState<string>('');
  
  return (
    <div className="p-4">
      {/* Tailwind CSS classes */}
    </div>
  );
}
```

### API Calls

```typescript
// Use async/await with proper error handling
try {
  const data = await fetchData();
  setState(data);
} catch (error) {
  console.error('Failed to fetch data:', error);
  // Handle error appropriately
}
```

### Search Implementation

```typescript
// Use AbortController for search cancellation
const controller = new AbortController();
const results = await searchEntities(query, {
  signal: controller.signal,
});
```

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

Modern browsers with ES2020+ support.

## Performance Optimization

- Lazy loading of routes and components
- Mapbox layer optimization for large datasets
- Image optimization in assets
- Code splitting with Vite
- Tree shaking for unused code removal
- Debounced search to reduce API calls
- Result attribution by entity for efficient rendering

## Troubleshooting

### Mapbox Token Issues

- Verify token is valid and not expired
- Check token permissions in Mapbox dashboard
- Ensure token is passed correctly to Map component

### API Connection Issues

- Verify backend API is running
- Check `VITE_API_URL` environment variable
- Verify CORS settings on backend
- Check network tab in browser DevTools

### Authentication Failures

- Verify Azure AD credentials in environment
- Check redirect URIs match your deployment
- Ensure tokens are being sent with requests
- Check token expiration

### Build Errors

```bash
# Clear cache and reinstall
rm -rf node_modules package-lock.json
npm install

# Rebuild
npm run build
```

## Deployment

### Build for Production

```bash
npm run build
```

### Environment-Specific Builds

Create `.env.production` for production settings:

```env
VITE_API_URL=https://api.your-domain.com
VITE_MAPBOX_TOKEN=pk_production_token
```

### Hosting Options

- **Vercel**: Optimized for Vite (recommended for serverless routes)
- **Netlify**: Drop-in build process
- **AWS S3 + CloudFront**: Static site hosting
- **Docker**: Containerized deployment

### Vercel Deployment

```bash
vercel
```

For serverless `api/*` routes:

```bash
vercel dev
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Ensure code passes linting (`npm run lint`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

## Documentation

Additional documentation files:

- [Map Camera Sync](docs/map-camera-sync.md) - Camera synchronization between maps

## Support

For issues and questions:
1. Check existing GitHub issues
2. Review component documentation in code
3. Contact the development team

## Live Demo

Check out the live demo: [https://tdp-gis-api-demo.vercel.app/](https://tdp-gis-api-demo.vercel.app/)

## License

[Add your license information here]

## Additional Resources

- [React Documentation](https://react.dev/)
- [TypeScript Documentation](https://www.typescriptlang.org/)
- [Vite Documentation](https://vitejs.dev/)
- [Mapbox GL Documentation](https://docs.mapbox.com/mapbox-gl-js/)
- [React Map GL Documentation](https://visgl.github.io/react-map-gl/)
- [Tailwind CSS Documentation](https://tailwindcss.com/)
- [Axios Documentation](https://axios-http.com/)
- [Lodash Documentation](https://lodash.com/)
- `GET /api/security/enable-gis-api` (client-credentials bootstrap; sets HTTP-only `gis_api_access_token`)
- `GET /api/auth/login`
- `GET /api/auth/callback`
- `GET /api/auth/session`
- `GET /api/auth/logout`

## Auth and Token Flow
- Browser never receives raw GIS API tokens in JSON payloads.
- `GET /api/security/enable-gis-api` fetches Entra client-credentials token and sets HTTP-only cookie `gis_api_access_token`.
- Sign-in flow sets HTTP-only `auth_access_token`.
- GIS upstream bearer resolution order is:
  1. `gis_api_access_token` cookie
  2. `auth_access_token` cookie
  3. `REST_API_BEARER_TOKEN` / `PUBLIC_API_BEARER_TOKEN` env fallback
- GIS routes auto-bootstrap if bearer is missing, then use the freshly issued token for the same request.

## Troubleshooting
- First search returns empty/error then second works:
  - fixed in current handlers by using freshly bootstrapped bearer in the same request.
- `No Entra bearer token available for TdpGis.Api`:
  - verify `AZURE_AD_TENANT_ID`, `AZURE_AD_CLIENT_ID`, `AZURE_AD_CLIENT_SECRET`
  - set `GIS_CLIENT_CREDENTIALS_SCOPE` (or `API_SCOPE`) to your API client-credentials scope.
- Signed in but no private workspace data:
  - verify `PRIVATE_WORKSPACE_ID` and `PRIVATE_WORKSPACE_ACCESS_TOKEN` are set on the BFF runtime.

## Public vs Signed-In Behavior
- Public user:
  - Can load public workspace entities/search.
  - No private workspace merge.
- Signed-in user:
  - Still gets public workspace entities.
  - If `PRIVATE_WORKSPACE_ID` + `PRIVATE_WORKSPACE_ACCESS_TOKEN` are configured, private workspace entities are merged in `api/gis/workspace-entities.ts`.

## Local Runtime Behavior
### `npm run dev` (Vite proxy)
- Proxies `/api/gis/*`, `/api/gis/workspace-entities`, `/api/gis/workspace-entity-search`.
- Useful for UI/dev proxy work.
- Does not execute serverless route files directly.

### `vercel dev` / deployed Vercel
- Executes `api/*` serverless handlers.
- Required for full auth/session/bootstrap flow and private merge behavior tied to HTTP-only cookies.

## Search/Data Flow
- `useWorkspaceEntities`:
  - Ensures GIS bootstrap cookie (`ensureGisApiCookie`) then loads `/api/gis/workspace-entities`.
  - Uses separate session caches for `anon` and `auth`.
- `useWorkspaceGeoSearch` calls `/api/gis/workspace-entity-search` per selected entity.
- Responses are normalized by `mapWorkspaceSearchResults.ts`.
- Dropdown and map consume unified `GeoFeature[]` from context.

## Scripts
- `npm run dev` - run Vite dev server
- `npm run build` - type-check and build production assets
- `npm run preview` - preview built assets
- `npm run lint` - lint project
