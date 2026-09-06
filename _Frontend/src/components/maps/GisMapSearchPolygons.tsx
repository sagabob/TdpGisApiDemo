/**
 * Renders polygon / multipolygon search footprints as Mapbox fill + outline layers
 * under the center-point DOM markers. One GeoJSON Source for all hits that have `areaGeometry`.
 */
import { memo, useEffect, useMemo } from 'react';
import type { MapLayerMouseEvent } from 'mapbox-gl';
import { Layer, Source, useMap } from 'react-map-gl/mapbox';
import type { GeoFeature } from '@/contexts/SearchContext';
import { selectedPinColor } from '@/config/gis-config';
import { pinColorForSourceEntityId } from '@/lib/entityPinColor';

const SOURCE_ID = 'gis-search-polygons';
const FILL_LAYER_ID = 'gis-search-polygons-fill';
const LINE_LAYER_ID = 'gis-search-polygons-line';

type Props = {
  results: GeoFeature[];
  selectedId: string | number | null;
  onSelect: (feature: GeoFeature) => void;
};

function GisMapSearchPolygonsInner({ results, selectedId, onSelect }: Props) {
  const mapApi = useMap();
  const map = mapApi?.current;

  const byId = useMemo(() => {
    const m = new Map<string, GeoFeature>();
    for (const item of results) {
      if (item.areaGeometry) m.set(String(item.Id), item);
    }
    return m;
  }, [results]);

  const geojson = useMemo(() => {
    const features = results
      .filter((item) => item.areaGeometry)
      .map((item) => {
        const selected = selectedId !== null && item.Id === selectedId;
        return {
          type: 'Feature' as const,
          id: String(item.Id),
          properties: {
            id: String(item.Id),
            selected: selected ? 1 : 0,
            color: selected ? selectedPinColor : pinColorForSourceEntityId(item.sourceEntityId),
          },
          geometry: item.areaGeometry!,
        };
      });
    return { type: 'FeatureCollection' as const, features };
  }, [results, selectedId]);

  useEffect(() => {
    if (!map || byId.size === 0) return;

    const onClick = (e: MapLayerMouseEvent) => {
      const propId = e.features?.[0]?.properties?.id;
      if (propId == null) return;
      const feature = byId.get(String(propId));
      if (!feature) return;
      e.originalEvent.stopPropagation();
      onSelect(feature);
    };

    const onEnter = () => {
      map.getCanvas().style.cursor = 'pointer';
    };
    const onLeave = () => {
      map.getCanvas().style.cursor = '';
    };

    map.on('click', FILL_LAYER_ID, onClick);
    map.on('mouseenter', FILL_LAYER_ID, onEnter);
    map.on('mouseleave', FILL_LAYER_ID, onLeave);

    return () => {
      map.off('click', FILL_LAYER_ID, onClick);
      map.off('mouseenter', FILL_LAYER_ID, onEnter);
      map.off('mouseleave', FILL_LAYER_ID, onLeave);
      map.getCanvas().style.cursor = '';
    };
  }, [map, byId, onSelect]);

  if (geojson.features.length === 0) return null;

  return (
    <Source id={SOURCE_ID} type="geojson" data={geojson}>
      <Layer
        id={FILL_LAYER_ID}
        type="fill"
        paint={{
          'fill-color': ['get', 'color'],
          'fill-opacity': ['case', ['==', ['get', 'selected'], 1], 0.4, 0.18],
        }}
      />
      <Layer
        id={LINE_LAYER_ID}
        type="line"
        paint={{
          'line-color': ['get', 'color'],
          'line-width': ['case', ['==', ['get', 'selected'], 1], 2.5, 1.5],
          'line-opacity': ['case', ['==', ['get', 'selected'], 1], 0.95, 0.7],
        }}
      />
    </Source>
  );
}

export const GisMapSearchPolygons = memo(GisMapSearchPolygonsInner);
