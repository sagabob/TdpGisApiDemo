import { createContext, type Dispatch, type SetStateAction } from "react";

export type Position = {
    longitude: number;
    latitude: number;
    zoom: number;
}

export type GeoFeature = {
    Id: string | number;
    placeName: string;
    locality: string;
    geometry: {
        type: string;
        coordinates: number[][]; // Usually [[longitude, latitude], ...] dependent on the shape
    };
    [key: string]: any;
}

export type GeoDataResult = {
    results?: GeoFeature[];
    [key: string]: any;
}

export interface SearchContextType {
    initialPosition: Position;
    setPosition: Dispatch<SetStateAction<Position>>;
    searchValue: string;
    setSearchValue: Dispatch<SetStateAction<string>>;
    loadedGeoData: GeoDataResult | null;
    getGeoData: Dispatch<SetStateAction<GeoDataResult | null>>;
    selectedGeo: GeoFeature | null;
    setSelectedGeo: Dispatch<SetStateAction<GeoFeature | null>>;
}

const SearchContext = createContext<SearchContextType>({} as SearchContextType);

export const SearchContextProvider = SearchContext.Provider;
export default SearchContext;