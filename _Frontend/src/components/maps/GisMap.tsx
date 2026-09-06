/**
 * Map shell: owns the Mapbox instance, camera sync strategy, and composes result pins + selection UI.
 *
 * Why `onMoveEnd` (not `onMove`): updating React context on every pan frame would re-render the whole
 * provider subtree (search bar, entity filters, this map) dozens of times per second. We only need
 * the latest camera in context for actions that read it (e.g. flying to a search hit); syncing on
 * gesture end keeps the UI responsive without thrashing React.
 *
 * Why `resize()` on load: the map often mounts before flex layout has given the container its final
 * size; calling `resize()` after paint fixes a 0×0 canvas until the next window resize.
 */
import Map, {
  NavigationControl,
  ScaleControl,
  type MapRef,
  type ViewStateChangeEvent,
} from 'react-map-gl/mapbox';
import 'mapbox-gl/dist/mapbox-gl.css';
import { mapboxAccessToken } from '@/config/gis-config';
import { useCallback, useContext, useEffect, useMemo, useRef } from 'react';
import SearchContext from '@/contexts/SearchContext';
import { GisMapSearchMarkers } from '@/components/maps/GisMapSearchMarkers';
import { GisMapSearchPolygons } from '@/components/maps/GisMapSearchPolygons';
import { GisMapSelectedOverlay } from '@/components/maps/GisMapSelectedOverlay';

export const GisMap = () => {
  const { loadedGeoData, selectedGeo, setSelectedGeo, initialPosition, setPosition } = useContext(SearchContext);
  const mapRef = useRef<MapRef>(null);

  // Normalize to a real array so marker list always receives `[]` instead of `undefined`.
  const results = useMemo(() => {
    const r = loadedGeoData?.results;
    return Array.isArray(r) ? r : [];
  }, [loadedGeoData?.results]);

  const handleLoad = useCallback(() => {
    // Defer to the next frame so layout (flex/absolute) has committed before measuring the container.
    requestAnimationFrame(() => {
      mapRef.current?.resize();
    });
  }, []);

  useEffect(() => {
    const map = mapRef.current;
    if (!map) return;

    // Keep map interactions smooth by letting Mapbox own the camera during pan/zoom.
    // We only "snap" camera from React state for external actions (e.g. selecting a search hit).
    const center = map.getCenter();
    const curLng = center.lng;
    const curLat = center.lat;
    const curZoom = map.getZoom();
    const curBearing = map.getBearing();
    const curPitch = map.getPitch();
    const eps = 1e-6;
    const changed =
      Math.abs(curLng - initialPosition.longitude) > eps ||
      Math.abs(curLat - initialPosition.latitude) > eps ||
      Math.abs(curZoom - initialPosition.zoom) > eps ||
      Math.abs(curBearing - initialPosition.bearing) > eps ||
      Math.abs(curPitch - initialPosition.pitch) > eps;

    if (!changed) return;
    map.easeTo({
      center: [initialPosition.longitude, initialPosition.latitude],
      zoom: initialPosition.zoom,
      bearing: initialPosition.bearing,
      pitch: initialPosition.pitch,
      duration: 250,
    });
  }, [initialPosition]);

  /** Sync camera to React only when movement stops — avoids re-rendering the tree on every pan frame. */
  const handleMoveEnd = useCallback(
    (evt: ViewStateChangeEvent) => {
      // Full `viewState` preserves bearing/pitch/padding so the next render does not reset tilt/rotation.
      setPosition(evt.viewState);
    },
    [setPosition],
  );

  const clearSelectedGeo = useCallback(() => {
    setSelectedGeo(null);
  }, [setSelectedGeo]);

  return (
    <div className="h-full min-h-0 w-full">
      <Map
        ref={mapRef}
        initialViewState={initialPosition}
        mapboxAccessToken={mapboxAccessToken}
        style={{ width: '100%', height: '100%' }}
        mapStyle="mapbox://styles/mapbox/streets-v9"
        onLoad={handleLoad}
        onMoveEnd={handleMoveEnd}
      >
        {/* Polygon footprints under pins — see `GisMapSearchPolygons`. */}
        <GisMapSearchPolygons
          results={results}
          selectedId={selectedGeo?.Id ?? null}
          onSelect={setSelectedGeo}
        />
        {/* Hit markers are split out and memoized — see `GisMapSearchMarkers`. */}
        <GisMapSearchMarkers
          results={results}
          selectedId={selectedGeo?.Id ?? null}
          onSelect={setSelectedGeo}
        />
        {/* Selected feature: larger pin + popup; kept separate so list markers can stay memoized. */}
        {selectedGeo ? (
          <GisMapSelectedOverlay feature={selectedGeo} onClose={clearSelectedGeo} />
        ) : null}
        <NavigationControl />
        <ScaleControl />
      </Map>
    </div>
  );
};
