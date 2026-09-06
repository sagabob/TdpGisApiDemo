import type { GeoFeature } from '@/contexts/SearchContext';
import type { GisConnectionDto } from '@/types/gisWorkspace';

export type WorkspaceSearchApiResponse = {
  searchedPhrase?: string;
  entityId?: string;
  collections?: unknown[];
};

/** Mirrors backend `TdpGis.Domain.GeometryType` (numeric JSON). */
const GeometryType = {
  Point: 0,
  MultiPoint: 1,
  LineString: 2,
  MultiLineString: 3,
  Polygon: 4,
  MultiPolygon: 5,
  GeometryCollection: 6,
} as const;

function isPolygonEntity(geometryType: number): boolean {
  return geometryType === GeometryType.Polygon || geometryType === GeometryType.MultiPolygon;
}

function labelForPropertyName(entity: GisConnectionDto, propertyName: string): string | undefined {
  const needle = propertyName.toLowerCase();
  return entity.propertyMappings.find((m) => m.propertyName.toLowerCase() === needle)?.propertyLabel;
}

/**
 * WKT from SQL sources, e.g.
 * - `POINT(172.68895 -43.53754)`
 * - `POLYGON((172.69 -43.53, ...))`
 * Optional `SRID=4326;` prefix is ignored. Uses the first lng/lat pair found.
 */
function extractLngLatFromWkt(wkt: string): [number, number] | null {
  const s = wkt.trim();
  if (!s) return null;
  // Prefer POINT(...) when present (center_point), else first coordinate pair in any WKT.
  const pointMatch = /\bPOINT\s*\(\s*([+-]?\d+(?:\.\d+)?)\s+([+-]?\d+(?:\.\d+)?)\s*\)/i.exec(s);
  if (pointMatch) {
    return [Number(pointMatch[1]), Number(pointMatch[2])];
  }
  const pairMatch = /([+-]?\d+(?:\.\d+)?)\s+([+-]?\d+(?:\.\d+)?)/.exec(s);
  if (!pairMatch) return null;
  // Avoid matching bare numbers that aren't geometry (e.g. ids) — require a geometry keyword.
  if (!/\b(POINT|POLYGON|LINESTRING|MULTIPOINT|MULTIPOLYGON|MULTILINESTRING|GEOMETRYCOLLECTION)\b/i.test(s)) {
    return null;
  }
  return [Number(pairMatch[1]), Number(pairMatch[2])];
}

/** GeoJSON, WKT string, bare [lng, lat], or nested Polygon/MultiPolygon-style coordinates. */
function extractLngLat(geometry: unknown): [number, number] | null {
  if (typeof geometry === 'string') {
    return extractLngLatFromWkt(geometry);
  }
  if (Array.isArray(geometry) && typeof geometry[0] === 'number' && typeof geometry[1] === 'number') {
    return [geometry[0], geometry[1]];
  }
  if (!geometry || typeof geometry !== 'object') return null;
  const g = geometry as { type?: string; coordinates?: unknown };
  const c = g.coordinates;
  if (!Array.isArray(c) || c.length === 0) return null;

  if (
    (g.type === 'Point' || g.type === undefined) &&
    typeof c[0] === 'number' &&
    typeof c[1] === 'number'
  ) {
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

/**
 * Polygon / MultiPolygon pins should sit on `center_point` when the API returns it
 * (mapped label or raw property name), not on the first ring vertex.
 */
function extractCenterPointLngLat(
  row: Record<string, unknown>,
  entity: GisConnectionDto,
): [number, number] | null {
  const centerLabel = labelForPropertyName(entity, 'center_point');
  const candidates: unknown[] = [];
  if (centerLabel && row[centerLabel] !== undefined) candidates.push(row[centerLabel]);
  if (row.center_point !== undefined) candidates.push(row.center_point);
  if (row.centerPoint !== undefined) candidates.push(row.centerPoint);

  for (const v of candidates) {
    const ll = extractLngLat(v);
    if (ll) return ll;
  }
  return null;
}

function extractPinLngLat(row: Record<string, unknown>, entity: GisConnectionDto): [number, number] | null {
  if (isPolygonEntity(entity.geometryType)) {
    const fromCenter = extractCenterPointLngLat(row, entity);
    if (fromCenter) return fromCenter;
  }

  // Prefer mapped geometry-ish fields (GeoJSON Object or WKT string like `geom` / `center_point`).
  for (const m of entity.propertyMappings) {
    if (m.propertyName.toLowerCase() === 'center_point') continue;
    const ll = extractLngLat(row[m.propertyLabel]);
    if (ll) return ll;
  }

  // Fallback for older/partial mappings: scan all row values.
  for (const [key, v] of Object.entries(row)) {
    if (key.toLowerCase() === 'center_point' || key === 'centerPoint') continue;
    const ll = extractLngLat(v);
    if (ll) return ll;
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
 *
 * Polygon / MultiPolygon entities: pin from `center_point` when present; otherwise fall back to geometry.
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

    const ll = extractPinLngLat(row, entity);
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
