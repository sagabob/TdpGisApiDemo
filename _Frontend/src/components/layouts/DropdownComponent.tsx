import { useEffect, useContext } from 'react'
import SearchContextConsumer, { type GeoFeature } from '@/contexts/SearchContext';
import { searchGeoTypeUrl } from '@/config/gis-config';
import axios from "axios";
import searchIcon from '@/assets/images/features/point-of-interest.svg'

export const DropdownComponent = () => {
    const { searchValue, loadedGeoData, getGeoData, selectedGeo, setSelectedGeo, setPosition, initialPosition } = useContext(SearchContextConsumer);

    useEffect(() => {
        const controller = new AbortController();

        const geGeoDataAsync = async () => {
            try {
                const request_url = `${searchGeoTypeUrl}/QueryPlaceName/${searchValue}/5`
                let res = await axios.get(request_url, { signal: controller.signal });
                let { data } = res;
                getGeoData(data);
            } catch(error) {
                if (!axios.isCancel(error)) {
                    console.error("Error fetching geo data", error);
                }
            }
        }

        if (searchValue.trim().length >= 3) {
            geGeoDataAsync();
        } else {
            getGeoData(null); //Empty the dropdownlist
        }

        setSelectedGeo(null); //Reset the selected geofeature
        
        // Cleanup function prevents memory leaks and stale closure race conditions
        return () => {
            controller.abort();
        };

    }, [searchValue, getGeoData, setSelectedGeo]);

    const updatingSelectedSearchResult = (selected: GeoFeature) => {
        setSelectedGeo(selected)
        setPosition({ ...initialPosition, longitude: Number(selected.geometry.coordinates[0][0]), latitude: Number(selected.geometry.coordinates[0][1]) })
    };

    const baseClassListItem = "w-full px-2 py-2 rounded-lg border-solid border-2 border-transparent border-b-slate-100 flex cursor-pointer hover:bg-slate-50 transition-colors"
    const baseClassListItemSelected = "w-full px-2 py-2 rounded-lg border-solid border-2 border-blue-500 bg-blue-50 flex cursor-pointer transition-colors"
    
    return (
        <div className="w-full bg-white shadow-xl rounded-md border border-slate-200 z-50 absolute top-full left-0 mt-2 max-h-[60vh] overflow-y-auto">
            {loadedGeoData !== null && loadedGeoData.results !== undefined && Array.isArray(loadedGeoData.results) && loadedGeoData.results.map((item: GeoFeature) =>
            (<article key={String(item.Id)}
                className={selectedGeo !== null && item.Id === selectedGeo.Id ? baseClassListItemSelected : baseClassListItem} onClick={() => updatingSelectedSearchResult(item)}>
                <img src={searchIcon} alt="" width="40" height="40" className="flex-none rounded-md bg-slate-100 p-1" />
                <div className="ml-3 flex flex-col justify-center" >
                    <h5 className="font-semibold text-sm text-slate-800 m-0">{item.placeName}</h5>
                    <p className="text-xs text-slate-500 m-0">{item.locality}</p>
                </div>
            </article>)
            )}
        </div>
    )
}
