import { defaultPinColor } from '@/config/gis-config';

/** Distinct fills that stay visible on typical basemaps. */
const ENTITY_PIN_PALETTE = [
  '#dc2626',
  '#16a34a',
  '#ca8a04',
  '#9333ea',
  '#0891b2',
  '#ea580c',
  '#be185d',
  '#4f46e5',
  '#0d9488',
  '#65a30d',
  '#c026d3',
  '#2563eb',
] as const;

function hashString(s: string): number {
  let h = 0;
  for (let i = 0; i < s.length; i++) {
    h = (Math.imul(31, h) + s.charCodeAt(i)) | 0;
  }
  return Math.abs(h);
}

/**
 * Stable color per workspace entity (search "category") for map pins.
 * Same entity id always maps to the same color across sessions.
 */
export function pinColorForSourceEntityId(sourceEntityId: string | undefined): string {
  if (sourceEntityId == null || sourceEntityId === '') {
    return defaultPinColor;
  }
  const idx = hashString(sourceEntityId) % ENTITY_PIN_PALETTE.length;
  return ENTITY_PIN_PALETTE[idx];
}
