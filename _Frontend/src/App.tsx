import { useState } from 'react';
import { GisMap } from '@/components/maps/GisMap';
import { SearchContextProvider } from '@/contexts/SearchContext';
import { defaultPosition } from '@/config/gis-config';
import SearchBar from '@/components/layouts/SearchBar';

function App() {
  const [initialPosition, setPosition] = useState(defaultPosition)
  const [searchValue, setSearchValue] = useState("")
  const [loadedGeoData, getGeoData] = useState<any>(null)
  const [selectedGeo, setSelectedGeo] = useState<any>(null)

  return (
    <SearchContextProvider value={{ initialPosition, setPosition, searchValue, setSearchValue, loadedGeoData, getGeoData, selectedGeo, setSelectedGeo }}>
      <SearchBar />
      <GisMap />
    </SearchContextProvider>
  );
}

export default App
