import { createContext, type Dispatch, type SetStateAction } from "react";
import type { ViewState } from "react-map-gl/mapbox";
import type { GisConnectionDto } from "@/types/gisWorkspace";

/** Map camera state (aligned with `react-map-gl` so pan/zoom/rotate updates stay consistent). */
type Position = ViewState;

export type GeoFeature = {
    Id: string | number;
    placeName: string;
    locality: string;
    geometry: {
        type: string;
        coordinates: number[][]; // Usually [[longitude, latitude], ...] dependent on the shape
    };
    /** Workspace GIS entity (connection) this hit belongs to — set for workspace search results. */
    sourceEntityId?: string;
    sourceEntityLabel?: string;
    [key: string]: unknown;
}

export type GeoDataResult = {
    results?: GeoFeature[];
    [key: string]: unknown;
}

interface SearchContextType {
    initialPosition: Position;
    setPosition: Dispatch<SetStateAction<Position>>;
    searchValue: string;
    setSearchValue: Dispatch<SetStateAction<string>>;
    loadedGeoData: GeoDataResult | null;
    getGeoData: Dispatch<SetStateAction<GeoDataResult | null>>;
    selectedGeo: GeoFeature | null;
    setSelectedGeo: Dispatch<SetStateAction<GeoFeature | null>>;
    workspaceEntities: GisConnectionDto[] | null;
    workspaceEntitiesLoading: boolean;
    workspaceEntitiesError: string | null;
    /** GIS connection ids the user wants to include in search (subset of workspace entities). */
    selectedEntityIds: string[];
    toggleEntitySelection: (entityId: string) => void;
    setSelectedEntityIds: Dispatch<SetStateAction<string[]>>;
}

const SearchContext = createContext<SearchContextType>({} as SearchContextType);

export const SearchContextProvider = SearchContext.Provider;
export default SearchContext;