# Map camera: pan / zoom and React state

This describes how `GisMap.tsx` ties **Mapbox GL** camera behavior to **React** (`SearchContext` `initialPosition` / `setPosition`) so panning stays smooth and the rest of the app can still move the map when needed (e.g. selecting a search result).

## Mental model

| Who initiates the move | Who drives the camera | How React learns / updates |
|------------------------|----------------------|----------------------------|
| User (pan / zoom / rotate) | Mapbox (internal transform) | `onMoveEnd` → `setPosition(evt.viewState)` once per gesture |
| App (e.g. pick a search row) | React state (`setPosition`) | `useEffect` → `mapRef.current.easeTo(...)` to match state |

## 1. `initialViewState` — Mapbox owns the camera while dragging

The `<Map>` uses **`initialViewState={initialPosition}`** instead of spreading `{...initialPosition}` on every render.

- **Controlled-style props** (passing `longitude` / `latitude` / `zoom` from React on each update) can fight user input: React asserts one camera while the user drags to another → jank or “rubber band” feel.
- **Initial view state** lets Mapbox hold the **live** camera during gestures. After first paint, pan/zoom runs in Mapbox’s own loop, not reconciled through React props on every frame.

## 2. `onMoveEnd` — sync React only when the gesture finishes

`onMoveEnd` calls **`setPosition(evt.viewState)`** when movement **ends**.

- **`onMove`** fires many times per second during a drag and would update context → re-render search bar, entity filters, markers, etc. on **every** frame.
- **`onMoveEnd`** means: store the latest camera **once** when the user finishes (or equivalent), keeping context aligned without thrashing the React tree.

`viewState` includes **bearing**, **pitch**, and **padding** where applicable, so the stored camera does not silently drop tilt/rotation.

## 3. `useEffect` + `easeTo` — when React moves the map

When something **outside** the map updates camera state (e.g. dropdown selection calls `setPosition({ ... })`), a `useEffect` keyed on **`initialPosition`**:

1. Reads the **current** map camera from `mapRef` (`getCenter`, `getZoom`, `getBearing`, `getPitch`).
2. Compares to **`initialPosition`** with a small epsilon (`1e-6`) to ignore float noise.
3. If different, calls **`map.easeTo({ ... })`** with a short duration (e.g. 250 ms) so the map **smoothly** matches the new target.

So: **user drives Mapbox** during drag; **app drives Mapbox** when state changes on purpose.

## Related files

- `src/components/maps/GisMap.tsx` — camera wiring, `resize()` on load, markers / selection overlay.
- `src/contexts/SearchContext.ts` — `Position` is aligned with `react-map-gl` `ViewState` for bearing/pitch consistency.

## Optional next step for many points

If there are **hundreds** of search pins, DOM `<Marker>` components can still be heavy. A follow-up is to render hits with a Mapbox **`Source` + `Layer`** (GPU) and keep `<Marker>` only for the selected feature or popup.
