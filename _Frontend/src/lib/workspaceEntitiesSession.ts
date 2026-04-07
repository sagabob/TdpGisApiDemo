import type { GisConnectionDto } from '@/types/gisWorkspace';

const STORAGE_KEY = 'tdp_gis_workspace_entities';

function isGisConnectionList(value: unknown): value is GisConnectionDto[] {
  return Array.isArray(value) && value.every((item) => item != null && typeof item === 'object' && 'id' in item);
}

export function readWorkspaceEntitiesFromSession(): GisConnectionDto[] | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (raw == null || raw === '') return null;
    const parsed: unknown = JSON.parse(raw);
    if (!isGisConnectionList(parsed)) return null;
    return parsed;
  } catch {
    return null;
  }
}

export function writeWorkspaceEntitiesToSession(entities: GisConnectionDto[]): void {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(entities));
  } catch {
    // Private mode or quota; app still works from memory.
  }
}

export function clearWorkspaceEntitiesSession(): void {
  try {
    sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    /* ignore */
  }
}
