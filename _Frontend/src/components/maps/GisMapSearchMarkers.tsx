/**
 * Renders one DOM `Marker` per search hit. Memoized per row so camera/context updates elsewhere
 * do not force every pin to reconcile.
 *
 * `stopPropagation` on click: Mapbox closes popups on map click; stopping the bubble keeps the
 * marker click from being treated as a background map click.
 */
import { memo } from 'react';
import { Marker } from 'react-map-gl/mapbox';
import type { GeoFeature } from '@/contexts/SearchContext';
import Pin from '@/components/maps/Pin';
import { pinColorForSourceEntityId } from '@/lib/entityPinColor';

type MarkerProps = {
  item: GeoFeature;
  onSelect: (feature: GeoFeature) => void;
  suppress: boolean;
};

/** One memoized marker — avoids re-creating every Marker when an unrelated sibling updates. */
const SearchResultMarker = memo(function SearchResultMarker({ item, onSelect, suppress }: MarkerProps) {
  if (suppress) return null;
  const pinColor = pinColorForSourceEntityId(item.sourceEntityId);
  return (
    <Marker
      longitude={Number(item.geometry.coordinates[0][0])}
      latitude={Number(item.geometry.coordinates[0][1])}
      onClick={(e) => {
        e.originalEvent.stopPropagation();
        onSelect(item);
      }}
    >
      <Pin size={20} color={pinColor} />
    </Marker>
  );
});

type ListProps = {
  results: GeoFeature[];
  selectedId: string | number | null;
  onSelect: (feature: GeoFeature) => void;
};

/**
 * Search hit pins — memoized list so parent updates (e.g. camera sync) do not rebuild all Markers.
 * Selected hit uses the larger pin in `GisMap`; we skip the small pin at the same coordinate.
 * For hundreds+ of points, consider a GeoJSON `Source` + `Layer` instead of DOM Markers.
 */
function GisMapSearchMarkersInner({ results, selectedId, onSelect }: ListProps) {
  return (
    <>
      {results.map((item) => (
        <SearchResultMarker
          key={String(item.Id)}
          item={item}
          onSelect={onSelect}
          suppress={selectedId !== null && item.Id === selectedId}
        />
      ))}
    </>
  );
}

export const GisMapSearchMarkers = memo(GisMapSearchMarkersInner);
