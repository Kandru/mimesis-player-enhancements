import type { PoiRenderer } from './types';

const defaultRenderer: PoiRenderer = {
  render: () => '<circle r="6" />',
};

export const poiRenderers: Record<string, PoiRenderer> = {
  vending: {
    render: () =>
      '<rect x="-6" y="-8" width="12" height="16" rx="2" />' +
      '<rect x="-4" y="-5" width="8" height="5" rx="1" class="minimap-poi-screen" />',
  },
  shower: {
    render: () =>
      '<rect x="-5" y="-7" width="10" height="6" rx="1.5" />' +
      '<line x1="-3" y1="0" x2="-3" y2="6" />' +
      '<line x1="0" y1="0" x2="0" y2="7" />' +
      '<line x1="3" y1="0" x2="3" y2="6" />',
  },
  save: {
    render: () =>
      '<rect x="-6" y="-6" width="12" height="12" rx="2" />' +
      '<circle r="2.5" cy="-1" />',
  },
  tram_start: {
    render: () =>
      '<line x1="0" y1="-8" x2="0" y2="8" />' +
      '<rect x="-7" y="-10" width="14" height="4" rx="1.5" />',
  },
};

export function renderPoiSvg(kind: string): string {
  return (poiRenderers[kind] ?? defaultRenderer).render({ tooltip: kind });
}
