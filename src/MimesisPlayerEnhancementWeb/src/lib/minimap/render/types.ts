export type PoiRenderContext = {
  label?: string;
  tooltip: string;
};

export type PoiRenderer = {
  render: (ctx: PoiRenderContext) => string;
};
