import type { GeoFeature } from '@/contexts/SearchContext';
import type { GisConnectionDto } from '@/types/gisWorkspace';

export type WorkspaceSearchApiResponse = {
  searchedPhrase?: string;
  entityId?: string;
  collections?: unknown[];
};

function labelForPropertyName(entity: GisConnectionDto, propertyName: string): string | undefined {
  return entity.propertyMappings.find((m) => m.propertyName === propertyName)?.propertyLabel;
}

/** Supports Point and nested Polygon/MultiPolygon-style coordinates. */
function extractLngLat(geometry: unknown): [number, number] | null {
  if (!geometry || typeof geometry !== 'object') return null;
  const g = geometry as { type?: string; coordinates?: unknown };
  const c = g.coordinates;
  if (!Array.isArray(c) || c.length === 0) return null;

  if (g.type === 'Point' && typeof c[0] === 'number' && typeof c[1] === 'number') {
    return [c[0], c[1]];
  }

  // GeoJSON Polygon/MultiPolygon coordinates are nested arrays.
  // Walk down the first branch until we hit a numeric lng/lat pair.
  let cur: unknown = c;
  for (let depth = 0; depth < 8; depth++) {
    if (!Array.isArray(cur) || cur.length === 0) return null;
    const first = cur[0];
    if (Array.isArray(first)) {
      if (typeof first[0] === 'number' && typeof first[1] === 'number') {
        return [first[0], first[1]];
      }
      cur = first;
      continue;
    }
    return null;
  }
  return null;
}

function geometryForMap(lng: number, lat: number): GeoFeature['geometry'] {
  // Legacy layout: `coordinates[0][0]` / `[0][1]` are lng/lat (see `GisMap`).
  return {
    type: 'Point',
    coordinates: [[lng, lat]],
  };
}

/**
 * Maps FastEndpoints search payload (`collections` = JSON rows keyed by property labels)
 * into the shape expected by `GisMap` / dropdown (GeoJSON-like geometry with legacy coordinate access).
 */
export function mapWorkspaceSearchCollectionsToGeoFeatures(
  data: WorkspaceSearchApiResponse,
  entity: GisConnectionDto,
  sourceEntityId: string,
): GeoFeature[] {
  const rows = data.collections;
  if (!Array.isArray(rows)) return [];

  const queryLabel = labelForPropertyName(entity, entity.queryField);
  const idLabel =
    labelForPropertyName(entity, '_id') ??
    entity.propertyMappings.find((m) => m.propertyName.toLowerCase() === 'id')?.propertyLabel;

  const out: GeoFeature[] = [];

  rows.forEach((raw, index) => {
    if (!raw || typeof raw !== 'object') return;
    const row = raw as Record<string, unknown>;

    let geometryValue: unknown;
    // Prefer fields declared as "Object" in property mappings, because
    // those are expected to contain geometry payloads from Mongo.
    for (const m of entity.propertyMappings) {
      if (m.columnType !== 1) continue;
      const v = row[m.propertyLabel];
      if (extractLngLat(v) !== null) {
        geometryValue = v;
        break;
      }
    }
    if (geometryValue === undefined) {
      // Fallback for older/partial mappings: scan all row values.
      for (const v of Object.values(row)) {
        if (extractLngLat(v) !== null) {
          geometryValue = v;
          break;
        }
      }
    }

    const ll = extractLngLat(geometryValue);
    if (!ll) return;

    const idBase =
      idLabel && row[idLabel] != null && row[idLabel] !== ''
        ? String(row[idLabel])
        : `row-${index}`;
    // "queryField" label is the best display title; fallback to first string in row.
    const placeName =
      queryLabel && row[queryLabel] != null
        ? String(row[queryLabel])
        : String(Object.values(row).find((v) => typeof v === 'string') ?? 'Result');

    const otherString =
      Object.entries(row).find(
        ([key, v]) =>
          typeof v === 'string' && key !== queryLabel && v !== placeName && String(v).length > 0,
      )?.[1] ?? '';

    out.push({
      Id: `${sourceEntityId}:${idBase}`,
      placeName,
      locality: typeof otherString === 'string' ? otherString : '',
      geometry: geometryForMap(ll[0], ll[1]),
      sourceEntityId,
      sourceEntityLabel: entity.entityLabel?.trim() || entity.name?.trim() || entity.entity,
    });
  });

  return out;
}
