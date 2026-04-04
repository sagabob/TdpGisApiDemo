# TDP GIS Map Application (Frontend)

A sleek, responsive, and highly interactive Geographic Information System (GIS) application built with React, Vite, and Mapbox GL. This frontend interface allows users to seamlessly search for locations, plot dynamic map markers, and view geospatial entity data.

## 🚀 Features
- **Interactive Mapbox Integration:** Leveraging `react-map-gl` for hardware-accelerated 2D/3D map rendering.
- **Real-time Location Search:** Instant geocoding and querying utilizing a debounced search bar.
- **Race Condition Prevention:** Implements `AbortController` in Axios requests to cancel lingering requests and ensure data consistency.
- **Custom Map Elements:** Dynamic visual pins and popups displaying details about the queried localities.
- **React Context API:** Uses lightweight global state management for clean component drilling on map position and geospatial selections.

## 🛠️ Technology Stack
- **Framework:** [React 19](https://react.dev/) + [TypeScript](https://www.typescriptlang.org/)
- **Build Tool:** [Vite](https://vitejs.dev/)
- **Styling:** [Tailwind CSS v4](https://tailwindcss.com/)
- **Map Engine:** [Mapbox GL JS](https://www.mapbox.com/mapbox-gl-js) + [React Map GL](https://visgl.github.io/react-map-gl/)
- **Utilities:** [Axios](https://axios-http.com/) (HTTP), [Lodash](https://lodash.com/) (Debouncing)

## ⚙️ Prerequisites
Ensure that your development environment includes:
- **Node.js** (v18.0.0 or higher recommended)
- **Git**

## 🔧 Installation & Setup

1. **Install Dependencies:**
   Navigate into the project directory and install the necessary npm packages:
   ```bash
   npm install
   ```

2. **Configure Environment Variables:**
   This project relies on Mapbox. You MUST provide a Mapbox Access Token for the map to render. 
   Create a `.env` file in the root of the frontend folder and add your key prefixed with `VITE_` so the client can read it:

   ```env
   VITE_MAPBOX_ACCESS_TOKEN=pk.your_mapbox_access_token_here
   ```

3. **Start the Development Server:**
   Boot up the Vite build engine (make sure to restart the server if you modify your `.env` file):
   ```bash
   npm run dev
   ```

4. **Navigate to the App:**
   Open [http://localhost:5173/](http://localhost:5173/) in your browser.

## 🏗️ Building for Production
To bundle the application via TypeScript transpilation and Vite:
```bash
npm run build
```
The optimized files will be generated securely inside the `/dist` directory.
