import axios from 'axios';
import { workspaceEntitiesUrl } from '@/config/gis-config';
import type { GisConnectionDto } from '@/types/gisWorkspace';

/**
 * GET /api/workspace-entities (Vercel function adds auth and upstream URL server-side).
 */
export async function fetchWorkspaceEntities(options?: { signal?: AbortSignal }): Promise<GisConnectionDto[]> {
    const res = await axios.get<GisConnectionDto[]>(workspaceEntitiesUrl, {
        signal: options?.signal,
    });
    return res.data;
}
