import Map, { Marker, NavigationControl, Popup, ScaleControl } from 'react-map-gl/mapbox';
import 'mapbox-gl/dist/mapbox-gl.css';
import { mapboxAccessToken, selectedPinColor } from '@/config/gis-config';
import { useContext } from 'react';
import SearchContext from '@/contexts/SearchContext';
import Pin from '@/components/maps/Pin';

export const GisMap = () => {
    const { loadedGeoData, selectedGeo, setSelectedGeo, initialPosition, setPosition } = useContext(SearchContext);
    return (
        <Map   {...initialPosition}
            mapboxAccessToken={mapboxAccessToken}
            style={{ width: "100%", height: "100%" }}
            mapStyle="mapbox://styles/mapbox/streets-v9"
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
                    color={selectedPinColor}
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
                    anchor="top"
                    longitude={Number(selectedGeo.geometry.coordinates[0][0])}
                    latitude={Number(selectedGeo.geometry.coordinates[0][1])}
                    onClose={() => setSelectedGeo(null)}

                >
                    <div>
                        <h5>{selectedGeo.placeName}</h5>
                        <p>{selectedGeo.locality}</p>
                    </div>

                </Popup>)
            }
            <NavigationControl />
            <ScaleControl />
        </Map>
    );
}