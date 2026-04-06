import { useState } from 'react';
import { GisMap } from '@/components/maps/GisMap';
import { SearchContextProvider, type GeoDataResult, type GeoFeature } from '@/contexts/SearchContext';
import { defaultPosition } from '@/config/gis-config';
import SearchBar from '@/components/layouts/SearchBar';
import { useWorkspaceEntities } from '@/hooks/useWorkspaceEntities';

function App() {
  const [initialPosition, setPosition] = useState(defaultPosition);
  const [searchValue, setSearchValue] = useState('');
  const [loadedGeoData, getGeoData] = useState<GeoDataResult | null>(null);
  const [selectedGeo, setSelectedGeo] = useState<GeoFeature | null>(null);
  const {
    workspaceEntities,
    workspaceEntitiesLoading,
    workspaceEntitiesError,
    selectedEntityIds,
    setSelectedEntityIds,
    toggleEntitySelection,
  } = useWorkspaceEntities();

  return (
    <SearchContextProvider
      value={{
        initialPosition,
        setPosition,
        searchValue,
        setSearchValue,
        loadedGeoData,
        getGeoData,
        selectedGeo,
        setSelectedGeo,
        workspaceEntities,
        workspaceEntitiesLoading,
        workspaceEntitiesError,
        selectedEntityIds,
        toggleEntitySelection,
        setSelectedEntityIds,
      }}
    >
      <div className="flex h-full min-h-0 w-full flex-col">
        <SearchBar />
        <div className="relative min-h-0 w-full flex-1">
          <div className="absolute inset-0 min-h-0">
            <GisMap />
          </div>
        </div>
      </div>
    </SearchContextProvider>
  );
}

export default App;
