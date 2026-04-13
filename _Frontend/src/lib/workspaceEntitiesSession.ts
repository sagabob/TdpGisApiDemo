import type { GisConnectionDto } from '@/types/gisWorkspace';

const STORAGE_PREFIX = 'tdp_gis_workspace_entities';

/** Anonymous cache vs signed-in (may include private workspace entities). */
export type WorkspaceEntitiesAuthScope = 'anon' | 'auth';

function keyForScope(scope: WorkspaceEntitiesAuthScope): string {
  return `${STORAGE_PREFIX}_${scope}`;
}

function isGisConnectionList(value: unknown): value is GisConnectionDto[] {
  return Array.isArray(value) && value.every((item) => item != null && typeof item === 'object' && 'id' in item);
}

export function readWorkspaceEntitiesFromSession(scope: WorkspaceEntitiesAuthScope): GisConnectionDto[] | null {
  try {
    const raw = sessionStorage.getItem(keyForScope(scope));
    if (raw == null || raw === '') return null;
    const parsed: unknown = JSON.parse(raw);
    if (!isGisConnectionList(parsed)) return null;
    return parsed;
  } catch {
    return null;
  }
}

export function writeWorkspaceEntitiesToSession(
  entities: GisConnectionDto[],
  scope: WorkspaceEntitiesAuthScope,
): void {
  try {
    sessionStorage.setItem(keyForScope(scope), JSON.stringify(entities));
  } catch {
    // Private mode or quota; app still works from memory.
  }
}

/** Drop cached signed-in list so the next load refetches merged public + private from the BFF. */
export function clearWorkspaceEntitiesAuthCache(): void {
  try {
    sessionStorage.removeItem(keyForScope('auth'));
  } catch {
    /* ignore */
  }
}
