import { workspaceEntitiesUrl } from '@/config/gis-config';
import type { GisConnectionDto } from '@/types/gisWorkspace';
import { gisApiClient } from '@/api/gisClient';

/**
 * GET /api/gis/workspace-entities (BFF forwards cookies → TdpGis.Api).
 */
export async function fetchWorkspaceEntities(options?: { signal?: AbortSignal }): Promise<GisConnectionDto[]> {
  const res = await gisApiClient.get<GisConnectionDto[]>(workspaceEntitiesUrl, {
    signal: options?.signal,
  });
  return res.data;
}
