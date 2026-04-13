import { useContext, useEffect } from 'react';
import axios from 'axios';
import { gisApiClient } from '@/api/gisClient';
import SearchContext from '@/contexts/SearchContext';
import { workspaceEntitySearchUrl } from '@/config/gis-config';
import {
  mapWorkspaceSearchCollectionsToGeoFeatures,
  type WorkspaceSearchApiResponse,
} from '@/lib/mapWorkspaceSearchResults';
import type { GeoFeature } from '@/contexts/SearchContext';

/**
 * When search text is long enough and entities are selected, loads place search results
 * from `/api/gis/workspace-entity-search` (one request per entity) and writes merged `results`
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
    // One controller per effect run: cancels stale requests as inputs change quickly.
    const controller = new AbortController();

    const run = async () => {
      try {
        if (selectedEntityIds.length === 0 || !workspaceEntities?.length) {
          getGeoData(null);
          return;
        }
        const phrase = searchValue.trim();
        // Query each selected entity independently so one transient failure
        // does not hide successful results from other entities.
        const batches = await Promise.allSettled(
          selectedEntityIds.map(async (entityId) => {
            const entity = workspaceEntities.find((e) => e.id === entityId);
            if (!entity) return [] as GeoFeature[];

            const res = await gisApiClient.get<WorkspaceSearchApiResponse>(workspaceEntitySearchUrl, {
              signal: controller.signal,
              params: {
                entityId,
                q: phrase,
                ...(entity.workspaceId ? { workspaceId: entity.workspaceId } : {}),
              },
            });

            return mapWorkspaceSearchCollectionsToGeoFeatures(res.data, entity, entityId);
          }),
        );

        // Keep only fulfilled batches; rejected batches are ignored and logged by axios path.
        const results = batches.flatMap((batch) => (batch.status === 'fulfilled' ? batch.value : []));
        getGeoData({ results });
      } catch (error) {
        if (!axios.isCancel(error)) {
          console.error('Error fetching geo data', error);
        }
      }
    };

    if (searchValue.trim().length >= 3 && selectedEntityIds.length > 0) {
      void run();
    } else {
      // Hide stale dropdown content when query is too short or no entity is selected.
      getGeoData(null);
    }

    // Clear map selection whenever search term or entity filters change.
    setSelectedGeo(null);

    return () => {
      // Abort in-flight calls from the previous run to avoid out-of-order updates.
      controller.abort();
    };
  }, [searchValue, getGeoData, setSelectedGeo, selectedEntityIds, workspaceEntities]);
}
