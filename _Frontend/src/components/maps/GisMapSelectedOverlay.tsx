/**
 * Visual emphasis + details for the active hit: larger pin and a popup anchored above the point.
 * Kept separate from `GisMapSearchMarkers` so the bulk of pins can stay memoized; only this
 * subtree re-renders when selection or popup content changes.
 *
 * `anchor="bottom"` + negative Y offset: tip of the popup sits near the pin without covering it.
 */
import { memo } from 'react';
import { Marker, Popup } from 'react-map-gl/mapbox';
import { selectedPinColor } from '@/config/gis-config';
import type { GeoFeature } from '@/contexts/SearchContext';
import Pin from '@/components/maps/Pin';

type Props = {
  feature: GeoFeature;
  onClose: () => void;
};

function GisMapSelectedOverlayInner({ feature, onClose }: Props) {
  const lng = Number(feature.geometry.coordinates[0][0]);
  const lat = Number(feature.geometry.coordinates[0][1]);

  return (
    <>
      <Marker
        key={`selected-${feature.Id}`}
        longitude={lng}
        latitude={lat}
        onClick={(e) => {
          // Same rationale as search markers: avoid map-level click closing the popup immediately.
          e.originalEvent.stopPropagation();
        }}
      >
        <Pin size={30} color={selectedPinColor} />
      </Marker>
      <Popup anchor="bottom" offset={[0, -14]} longitude={lng} latitude={lat} onClose={onClose}>
        <div>
          {/*
            Text fields come from GeoFeature (SearchContext). For workspace search they are filled in
            mapWorkspaceSearchResults.ts: placeName = row[label for entity.queryField]; locality = another
            string column heuristic; sourceEntityLabel = workspace entity name/label — not raw API keys here.
          */}
          <h5 className="m-0 text-sm font-semibold text-slate-800">{feature.placeName}</h5>
          {feature.locality ? <p className="m-0 mt-1 text-xs text-slate-500">{feature.locality}</p> : null}
          {feature.sourceEntityLabel ? (
            <p className="m-0 mt-1.5 text-[11px] font-medium uppercase tracking-wide text-slate-400">
              {feature.sourceEntityLabel}
            </p>
          ) : null}
        </div>
      </Popup>
    </>
  );
}

/**
 * `memo`: skip re-rendering this subtree when the parent (`GisMap`) re-renders but the same feature
 * is still selected and `onClose` is a stable callback — fewer DOM updates for Marker/Popup.
 * (Inline `onClose={() => ...}` in the parent would defeat this; parent uses `useCallback`.)
 */
export const GisMapSelectedOverlay = memo(GisMapSelectedOverlayInner);
