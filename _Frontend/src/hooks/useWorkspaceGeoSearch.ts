import { useContext, useEffect } from 'react';
import axios from 'axios';
import SearchContext from '@/contexts/SearchContext';
import { workspaceEntitySearchUrl } from '@/config/gis-config';
import {
  mapWorkspaceSearchCollectionsToGeoFeatures,
  type WorkspaceSearchApiResponse,
} from '@/lib/mapWorkspaceSearchResults';
import type { GeoFeature } from '@/contexts/SearchContext';

/**
 * When search text is long enough and entities are selected, loads place search results
 * from `/api/workspace-entity-search` (one request per entity) and writes merged `results`
 * into SearchContext via `getGeoData`.
 */
export function useWorkspaceGeoSearch() {
  const {
    searchValue,
    getGeoData,
    setSelectedGeo,
    selectedEntityIds,
    workspaceEntities,
  } = useContext(SearchContext);

  useEffect(() => {
    const controller = new AbortController();

    const run = async () => {
      try {
        if (selectedEntityIds.length === 0 || !workspaceEntities?.length) {
          getGeoData(null);
          return;
        }
        const phrase = searchValue.trim();
        const batches = await Promise.all(
          selectedEntityIds.map(async (entityId) => {
            const entity = workspaceEntities.find((e) => e.id === entityId);
            if (!entity) return [] as GeoFeature[];

            const res = await axios.get<WorkspaceSearchApiResponse>(workspaceEntitySearchUrl, {
              signal: controller.signal,
              params: {
                entityId,
                q: phrase,
              },
            });

            return mapWorkspaceSearchCollectionsToGeoFeatures(res.data, entity, entityId);
          }),
        );

        getGeoData({ results: batches.flat() });
      } catch (error) {
        if (!axios.isCancel(error)) {
          console.error('Error fetching geo data', error);
        }
      }
    };

    if (searchValue.trim().length >= 3 && selectedEntityIds.length > 0) {
      void run();
    } else {
      getGeoData(null);
    }

    setSelectedGeo(null);

    return () => {
      controller.abort();
    };
  }, [searchValue, getGeoData, setSelectedGeo, selectedEntityIds, workspaceEntities]);
}
