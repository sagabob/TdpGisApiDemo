import Map, { Marker, NavigationControl, Popup, ScaleControl, type MapRef } from 'react-map-gl/mapbox';
import 'mapbox-gl/dist/mapbox-gl.css';
import { mapboxAccessToken, selectedPinColor } from '@/config/gis-config';
import { useCallback, useContext, useRef } from 'react';
import SearchContext from '@/contexts/SearchContext';
import Pin from '@/components/maps/Pin';

export const GisMap = () => {
    const { loadedGeoData, selectedGeo, setSelectedGeo, initialPosition, setPosition } = useContext(SearchContext);
    const mapRef = useRef<MapRef>(null);

    const handleLoad = useCallback(() => {
        // Container can be 0×0 on first layout; force Mapbox to match the final flex/absolute box.
        requestAnimationFrame(() => {
            mapRef.current?.resize();
        });
    }, []);

    return (
        <div className="h-full min-h-0 w-full">
        <Map
            ref={mapRef}
            {...initialPosition}
            mapboxAccessToken={mapboxAccessToken}
            style={{ width: "100%", height: "100%" }}
            mapStyle="mapbox://styles/mapbox/streets-v9"
            onLoad={handleLoad}
            onMove={evt => setPosition(evt.viewState)}
        >
            {loadedGeoData !== null && loadedGeoData.results !== undefined && Array.isArray(loadedGeoData.results) && loadedGeoData.results.map((item) =>
            (
                <Marker
                    key={item.Id}
                    longitude={Number(item.geometry.coordinates[0][0])}
                    latitude={Number(item.geometry.coordinates[0][1])}
                    onClick={e => {
                        // If we let the click event propagates to the map, it will immediately close the popup
                        // with `closeOnClick: true`
                        e.originalEvent.stopPropagation();
                        setSelectedGeo(item);

                    }}
                >
                    <Pin size={20} />
                </Marker>
            ))}
            {selectedGeo && (
                <Marker
                    key={"selected" + selectedGeo.Id}
                    longitude={Number(selectedGeo.geometry.coordinates[0][0])}
                    latitude={Number(selectedGeo.geometry.coordinates[0][1])}
                    onClick={e => {
                        // If we let the click event propagates to the map, it will immediately close the popup
                        // with `closeOnClick: true`
                        e.originalEvent.stopPropagation();

                    }}
                >
                    <Pin size={30} color={selectedPinColor} />
                </Marker>)
            }

            {selectedGeo && (
                <Popup
                    key={selectedGeo.Id}
                    anchor="bottom"
                    offset={[0, -14]}
                    longitude={Number(selectedGeo.geometry.coordinates[0][0])}
                    latitude={Number(selectedGeo.geometry.coordinates[0][1])}
                    onClose={() => setSelectedGeo(null)}

                >
                    <div>
                        <h5 className="font-semibold text-sm text-slate-800 m-0">{selectedGeo.placeName}</h5>
                        {selectedGeo.locality ? (
                            <p className="text-xs text-slate-500 m-0 mt-1">{selectedGeo.locality}</p>
                        ) : null}
                        {selectedGeo.sourceEntityLabel ? (
                            <p className="text-[11px] font-medium uppercase tracking-wide text-slate-400 m-0 mt-1.5">
                                {selectedGeo.sourceEntityLabel}
                            </p>
                        ) : null}
                    </div>

                </Popup>)
            }
            <NavigationControl />
            <ScaleControl />
        </Map>
        </div>
    );
}