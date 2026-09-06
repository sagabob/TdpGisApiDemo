import type { AreaGeometry, GeoFeature } from '@/contexts/SearchContext';
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

function stripSridPrefix(wkt: string): string {
  return wkt.replace(/^SRID\s*=\s*\d+\s*;\s*/i, '').trim();
}

function parseWktRing(ringText: string): number[][] | null {
  const pairs = ringText.match(
    /[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\s+[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?/g,
  );
  if (!pairs || pairs.length < 3) return null;
  const ring: number[][] = [];
  for (const pair of pairs) {
    const parts = pair.trim().split(/\s+/);
    const lng = Number(parts[0]);
    const lat = Number(parts[1]);
    if (!Number.isFinite(lng) || !Number.isFinite(lat)) return null;
    ring.push([lng, lat]);
  }
  const first = ring[0];
  const last = ring[ring.length - 1];
  if (first[0] !== last[0] || first[1] !== last[1]) {
    ring.push([first[0], first[1]]);
  }
  return ring.length >= 4 ? ring : null;
}

function parseWktPolygonBody(body: string): number[][][] | null {
  const raw = body.trim();
  const groups: string[] = [];
  const groupRe = /\(([^()]*)\)/g;
  let m: RegExpExecArray | null;
  while ((m = groupRe.exec(raw)) !== null) {
    groups.push(m[1]);
  }
  const rings: number[][][] = [];
  for (const text of groups) {
    const ring = parseWktRing(text);
    if (ring) rings.push(ring);
  }
  return rings.length > 0 ? rings : null;
}

/**
 * Parse WKT POLYGON / MULTIPOLYGON into GeoJSON coordinates.
 * Supports typical PostGIS text like `POLYGON((lng lat, ...))`.
 */
function parseWktAreaGeometry(wkt: string): AreaGeometry | null {
  const s = stripSridPrefix(wkt);
  const multi = /^MULTIPOLYGON\s*\(/i.exec(s);
  if (multi) {
    const polygons: number[][][][] = [];
    const inner = s.slice(multi[0].length - 1);
    let depth = 0;
    let start = -1;
    for (let i = 0; i < inner.length; i++) {
      const ch = inner[i];
      if (ch === '(') {
        depth++;
        if (depth === 2 && start < 0) start = i;
      } else if (ch === ')') {
        if (depth === 2 && start >= 0) {
          const chunk = inner.slice(start, i + 1);
          const rings = parseWktPolygonBody(chunk);
          if (rings) polygons.push(rings);
          start = -1;
        }
        depth--;
      }
    }
    return polygons.length > 0 ? { type: 'MultiPolygon', coordinates: polygons } : null;
  }

  const poly = /^POLYGON\s*\(/i.exec(s);
  if (poly) {
    const body = s.slice(poly[0].length - 1);
    const rings = parseWktPolygonBody(body);
    return rings ? { type: 'Polygon', coordinates: rings } : null;
  }

  return null;
}

function isNumericPair(v: unknown): v is [number, number] {
  return Array.isArray(v) && typeof v[0] === 'number' && typeof v[1] === 'number';
}

function looksLikeRing(v: unknown): v is number[][] {
  return Array.isArray(v) && v.length >= 3 && isNumericPair(v[0]);
}

function looksLikePolygonCoords(v: unknown): v is number[][][] {
  return Array.isArray(v) && v.length > 0 && looksLikeRing(v[0]);
}

function looksLikeMultiPolygonCoords(v: unknown): v is number[][][][] {
  return Array.isArray(v) && v.length > 0 && looksLikePolygonCoords(v[0]);
}

function parseGeoJsonAreaGeometry(value: unknown): AreaGeometry | null {
  if (!value || typeof value !== 'object') return null;
  const g = value as { type?: string; coordinates?: unknown };
  const t = g.type?.toLowerCase();
  if (t === 'polygon' && looksLikePolygonCoords(g.coordinates)) {
    return { type: 'Polygon', coordinates: g.coordinates };
  }
  if (t === 'multipolygon' && looksLikeMultiPolygonCoords(g.coordinates)) {
    return { type: 'MultiPolygon', coordinates: g.coordinates };
  }
  if (!t && looksLikeMultiPolygonCoords(g.coordinates)) {
    return { type: 'MultiPolygon', coordinates: g.coordinates };
  }
  if (!t && looksLikePolygonCoords(g.coordinates)) {
    return { type: 'Polygon', coordinates: g.coordinates };
  }
  return null;
}

function toAreaGeometry(value: unknown): AreaGeometry | null {
  if (typeof value === 'string') {
    return parseWktAreaGeometry(value);
  }
  return parseGeoJsonAreaGeometry(value);
}

const AREA_PROPERTY_NAMES = new Set([
  'geom',
  'geometry',
  'shape',
  'wkt',
  'the_geom',
  'wkb_geometry',
  'geog',
  'geography',
]);

function extractAreaGeometry(
  row: Record<string, unknown>,
  entity: GisConnectionDto,
): AreaGeometry | null {
  const candidates: unknown[] = [];

  for (const m of entity.propertyMappings) {
    const name = m.propertyName.toLowerCase();
    if (name === 'center_point') continue;
    if (AREA_PROPERTY_NAMES.has(name) || name.includes('geom') || name.includes('polygon')) {
      if (row[m.propertyLabel] !== undefined) candidates.push(row[m.propertyLabel]);
    }
  }

  for (const key of Object.keys(row)) {
    if (AREA_PROPERTY_NAMES.has(key.toLowerCase())) {
      candidates.push(row[key]);
    }
  }

  for (const m of entity.propertyMappings) {
    if (m.columnType !== 1) continue;
    if (m.propertyName.toLowerCase() === 'center_point') continue;
    if (row[m.propertyLabel] !== undefined) candidates.push(row[m.propertyLabel]);
  }

  for (const v of candidates) {
    const area = toAreaGeometry(v);
    if (area) return area;
  }
  return null;
}

/**
 * Geometry / pin fields must not appear in dropdown or popup text (`placeName` / `locality`).
 * Matches common property names and WKT payloads like `POINT(...)` / `POLYGON(...)`.
 */
const HIDDEN_DISPLAY_PROPERTY_NAMES = new Set([
  'center_point',
  'centerpoint',
  'geom',
  'geometry',
  'shape',
  'wkt',
  'the_geom',
  'wkb_geometry',
  'geog',
  'geography',
]);

function isHiddenDisplayPropertyKey(key: string): boolean {
  return HIDDEN_DISPLAY_PROPERTY_NAMES.has(key.trim().toLowerCase());
}

function looksLikeGeometryDisplayValue(value: string): boolean {
  return /\b(POINT|POLYGON|LINESTRING|MULTIPOINT|MULTIPOLYGON|MULTILINESTRING|GEOMETRYCOLLECTION)\b/i.test(
    value,
  );
}

function isDisplayableStringField(key: string, value: unknown, excludeValue?: string): value is string {
  if (typeof value !== 'string' || value.length === 0) return false;
  if (isHiddenDisplayPropertyKey(key)) return false;
  if (looksLikeGeometryDisplayValue(value)) return false;
  if (excludeValue !== undefined && value === excludeValue) return false;
  return true;
}

/**
 * Maps FastEndpoints search payload (`collections` = JSON rows keyed by property labels)
 * into the shape expected by `GisMap` / dropdown (GeoJSON-like geometry with legacy coordinate access).
 *
 * Polygon / MultiPolygon entities: pin from `center_point` when present; `areaGeometry` from `geom` when parseable.
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

  // Also hide mapped labels for geometry property names (label may differ from propertyName).
  const hiddenLabels = new Set<string>();
  for (const m of entity.propertyMappings) {
    if (isHiddenDisplayPropertyKey(m.propertyName) || isHiddenDisplayPropertyKey(m.propertyLabel)) {
      hiddenLabels.add(m.propertyLabel);
    }
  }

  const polygonEntity = isPolygonEntity(entity.geometryType);
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

    const placeName =
      queryLabel &&
      row[queryLabel] != null &&
      !isHiddenDisplayPropertyKey(queryLabel) &&
      !hiddenLabels.has(queryLabel) &&
      !looksLikeGeometryDisplayValue(String(row[queryLabel]))
        ? String(row[queryLabel])
        : String(
            Object.entries(row).find(
              ([key, v]) => !hiddenLabels.has(key) && isDisplayableStringField(key, v),
            )?.[1] ?? 'Result',
          );

    const otherString =
      Object.entries(row).find(
        ([key, v]) =>
          key !== queryLabel &&
          !hiddenLabels.has(key) &&
          isDisplayableStringField(key, v, placeName),
      )?.[1] ?? '';

    const areaGeometry = polygonEntity ? extractAreaGeometry(row, entity) ?? undefined : undefined;

    out.push({
      Id: `${sourceEntityId}:${idBase}`,
      placeName,
      locality: typeof otherString === 'string' ? otherString : '',
      geometry: geometryForMap(ll[0], ll[1]),
      geometryKind: polygonEntity ? 'polygon' : 'point',
      ...(areaGeometry ? { areaGeometry } : {}),
      sourceEntityId,
      sourceEntityLabel: entity.entityLabel?.trim() || entity.name?.trim() || entity.entity,
    });
  });

  return out;
}
