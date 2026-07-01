const MinimapRenderer = {
  VIEW_SIZE: 1000,
  CONTENT_PADDING: 0.08,
  DOOR_LENGTH: 22,
  // Room walls are stroked at 2px; doors are 3x as thick so they overlap the wall.
  DOOR_STROKE_WIDTH: 6,
  TELEPORTER_RADIUS: 9,

  filterMarkers(markers, focusSteamId, showAll, isHost) {
    const list = markers || [];
    if (showAll) {
      if (!isHost) return [];
      return list.filter((marker) => marker.isAlive);
    }

    const focus = focusSteamId ? String(focusSteamId) : '';
    if (!focus) {
      const local = list.find((marker) => marker.isLocal);
      if (local) return [local];
      return list.length > 0 ? [list[0]] : [];
    }

    const match = list.find((marker) => String(marker.steamId) === focus);
    return match ? [match] : [];
  },

  computeViewport(tiles) {
    if (!tiles || tiles.length === 0) {
      return { scale: 1, offsetX: 0, offsetZ: 0 };
    }

    let minX = Infinity;
    let minZ = Infinity;
    let maxX = -Infinity;
    let maxZ = -Infinity;

    tiles.forEach((tile) => {
      minX = Math.min(minX, tile.x);
      minZ = Math.min(minZ, tile.z);
      maxX = Math.max(maxX, tile.x + tile.w);
      maxZ = Math.max(maxZ, tile.z + tile.h);
    });

    const contentW = Math.max(maxX - minX, 0.001);
    const contentH = Math.max(maxZ - minZ, 0.001);
    const pad = this.CONTENT_PADDING;
    const scale = Math.min((1 - pad * 2) / contentW, (1 - pad * 2) / contentH);
    const scaledW = contentW * scale;
    const scaledH = contentH * scale;
    const offsetX = (1 - scaledW) * 0.5 - minX * scale;
    const offsetZ = (1 - scaledH) * 0.5 - minZ * scale;

    return { scale, offsetX, offsetZ };
  },

  mapPoint(viewport, x, z) {
    const nx = x * viewport.scale + viewport.offsetX;
    const ny = z * viewport.scale + viewport.offsetZ;
    return {
      x: nx * this.VIEW_SIZE,
      y: (1 - ny) * this.VIEW_SIZE,
    };
  },

  displayYaw(yaw) {
    // mapPoint flips world Z; compensate so the heading matches movement on screen.
    return (yaw || 0) + 180;
  },

  mapRect(viewport, x, z, w, h) {
    const cornerA = this.mapPoint(viewport, x, z);
    const cornerB = this.mapPoint(viewport, x + w, z + h);
    return {
      x: Math.min(cornerA.x, cornerB.x),
      y: Math.min(cornerA.y, cornerB.y),
      width: Math.max(Math.abs(cornerB.x - cornerA.x), 4),
      height: Math.max(Math.abs(cornerB.y - cornerA.y), 4),
    };
  },

  tileCenter(viewport, tile) {
    return this.mapPoint(viewport, tile.x + tile.w * 0.5, tile.z + tile.h * 0.5);
  },

  render(svgEl, data) {
    if (!svgEl || !data) return;

    svgEl._minimapLayoutVersion = data.layoutVersion;
    svgEl._minimapActiveAreaId = data.activeAreaId || '';

    const ns = 'http://www.w3.org/2000/svg';
    while (svgEl.firstChild) {
      svgEl.removeChild(svgEl.firstChild);
    }

    if (data.displayMode === 'hidden') {
      svgEl._minimapViewport = null;
      return;
    }

    const tiles = data.tiles || [];
    svgEl._minimapViewport = this.computeViewport(tiles);
    this.drawMapContent(svgEl, data, ns, svgEl._minimapViewport);
    this.drawTrain(svgEl, data, ns, svgEl._minimapViewport);
    this.drawMarkers(svgEl, data, ns, svgEl._minimapViewport);
  },

  updateMarkers(svgEl, data) {
    if (!svgEl || !data || data.displayMode === 'hidden') return;

    const activeAreaId = data.activeAreaId || '';
    if (svgEl._minimapActiveAreaId !== activeAreaId) {
      this.render(svgEl, data);
      return;
    }

    const ns = 'http://www.w3.org/2000/svg';
    svgEl.querySelector('.minimap-markers')?.remove();
    svgEl.querySelector('.minimap-train')?.remove();

    const viewport = svgEl._minimapViewport || this.computeViewport(data.tiles || []);
    this.drawTrain(svgEl, data, ns, viewport);
    this.drawMarkers(svgEl, data, ns, viewport);
  },

  connectionGeometry(viewport, point, tilesById) {
    const fromTile = tilesById.get(point.fromTileId);
    const toTile = tilesById.get(point.toTileId);
    const mid = this.mapPoint(viewport, point.x, point.z);

    let dirX = null;
    let dirY = null;
    if (fromTile && toTile) {
      const fromCenter = this.tileCenter(viewport, fromTile);
      const toCenter = this.tileCenter(viewport, toTile);
      const length = Math.hypot(toCenter.x - fromCenter.x, toCenter.y - fromCenter.y) || 1;
      dirX = (toCenter.x - fromCenter.x) / length;
      dirY = (toCenter.y - fromCenter.y) / length;
    } else if (point.dirX != null && point.dirZ != null) {
      const dirPoint = this.mapPoint(viewport, point.x + point.dirX * 0.01, point.z + point.dirZ * 0.01);
      const length = Math.hypot(dirPoint.x - mid.x, dirPoint.y - mid.y) || 1;
      dirX = (dirPoint.x - mid.x) / length;
      dirY = (dirPoint.y - mid.y) / length;
    }

    return { fromTile, toTile, mid, dirX, dirY };
  },

  createConnectionLine(viewport, point, tilesById, ns) {
    const geo = this.connectionGeometry(viewport, point, tilesById);

    // Cross-area transitions (teleporters, stairs) render as a red diamond.
    if (point.crossArea) {
      const group = document.createElementNS(ns, 'g');
      group.setAttribute('class', 'minimap-teleporter');
      group.setAttribute('transform', 'translate(' + geo.mid.x + ' ' + geo.mid.y + ')');
      const r = this.TELEPORTER_RADIUS;
      const diamond = document.createElementNS(ns, 'polygon');
      diamond.setAttribute('points', '0,-' + r + ' ' + r + ',0 0,' + r + ' -' + r + ',0');
      group.appendChild(diamond);
      const teleTitle = document.createElementNS(ns, 'title');
      teleTitle.textContent = 'Teleporter → ' + (point.targetAreaId || 'other area');
      group.appendChild(teleTitle);
      return group;
    }

    if (geo.dirX == null || geo.dirY == null) {
      return null;
    }

    // Doors lie along the shared wall, thicker than the wall stroke so they
    // overlap it on both sides.
    const wallX = geo.dirX;
    const wallY = geo.dirY;
    const half = this.DOOR_LENGTH * 0.5;
    const line = document.createElementNS(ns, 'line');
    line.setAttribute('x1', String(geo.mid.x - wallX * half));
    line.setAttribute('y1', String(geo.mid.y - wallY * half));
    line.setAttribute('x2', String(geo.mid.x + wallX * half));
    line.setAttribute('y2', String(geo.mid.y + wallY * half));
    line.setAttribute('stroke-width', String(this.DOOR_STROKE_WIDTH));
    line.setAttribute('class', 'minimap-connection');

    const title = document.createElementNS(ns, 'title');
    const fromLabel = geo.fromTile?.label || point.fromTileId || 'Room';
    const toLabel = geo.toTile?.label || point.toTileId || point.targetAreaId || 'Area';
    title.textContent = fromLabel + ' ↔ ' + toLabel;
    line.appendChild(title);

    return line;
  },

  drawMapContent(svgEl, data, ns, viewport) {
    const tiles = data.tiles || [];
    viewport = viewport || this.computeViewport(tiles);
    const tilesById = new Map();
    tiles.forEach((tile) => tilesById.set(tile.id, tile));

    const mapRoot = document.createElementNS(ns, 'g');
    mapRoot.setAttribute('class', 'minimap-map-root');

    const tileLayer = document.createElementNS(ns, 'g');
    tileLayer.setAttribute('class', 'minimap-tiles');
    tiles.forEach((tile) => {
      const rect = this.mapRect(viewport, tile.x, tile.z, tile.w, tile.h);

      const el = document.createElementNS(ns, 'rect');
      el.setAttribute('x', String(rect.x));
      el.setAttribute('y', String(rect.y));
      el.setAttribute('width', String(rect.width));
      el.setAttribute('height', String(rect.height));
      el.setAttribute('rx', '6');
      el.setAttribute('class', tile.isMainPath ? 'minimap-tile main-path' : 'minimap-tile branch');

      const title = document.createElementNS(ns, 'title');
      title.textContent = tile.label || 'Room';
      el.appendChild(title);
      tileLayer.appendChild(el);

      if (tile.label && rect.width > 36 && rect.height > 18) {
        const label = document.createElementNS(ns, 'text');
        const center = this.tileCenter(viewport, tile);
        label.setAttribute('x', String(center.x));
        label.setAttribute('y', String(center.y));
        label.setAttribute('class', 'minimap-tile-label');
        label.textContent = this.shortLabel(tile.label);
        tileLayer.appendChild(label);
      }
    });
    mapRoot.appendChild(tileLayer);

    // Doors/teleporters draw above the tiles so they overlap room wall strokes.
    const connectionPoints = document.createElementNS(ns, 'g');
    connectionPoints.setAttribute('class', 'minimap-connection-points');
    (data.connectionPoints || []).forEach((point) => {
      const line = this.createConnectionLine(viewport, point, tilesById, ns);
      if (line) {
        connectionPoints.appendChild(line);
      }
    });
    mapRoot.appendChild(connectionPoints);
    svgEl.appendChild(mapRoot);
  },

  drawTrain(svgEl, data, ns, viewport) {
    if (!data.train) return;
    const train = document.createElementNS(ns, 'g');
    train.setAttribute('class', 'minimap-train');
    train.setAttribute('transform', this.markerTransform(viewport, data.train));
    train.innerHTML =
      '<rect x="-14" y="-8" width="28" height="16" rx="3"></rect>' +
      '<polygon points="14,-6 22,0 14,6"></polygon>';
    const trainTitle = document.createElementNS(ns, 'title');
    trainTitle.textContent = 'Train';
    train.appendChild(trainTitle);
    svgEl.appendChild(train);
  },

  drawMarkers(svgEl, data, ns, viewport) {
    const tiles = data.tiles || [];
    viewport = viewport || this.computeViewport(tiles);

    const markers = document.createElementNS(ns, 'g');
    markers.setAttribute('class', 'minimap-markers');
    const blindMode = !!data.blindMode;
    (data.markers || []).forEach((marker) => {
      const group = document.createElementNS(ns, 'g');
      group.setAttribute('class', this.markerClass(marker, blindMode));
      group.setAttribute('transform', this.markerPositionTransform(viewport, marker));

      const body = document.createElementNS(ns, 'g');
      body.setAttribute('transform', 'rotate(' + this.displayYaw(marker.yaw) + ')');

      const dot = document.createElementNS(ns, 'circle');
      dot.setAttribute('r', '10');
      body.appendChild(dot);

      const heading = document.createElementNS(ns, 'polygon');
      heading.setAttribute('points', '0,16 6,6 -6,6');
      heading.setAttribute('class', 'minimap-heading');
      body.appendChild(heading);
      group.appendChild(body);

      const label = document.createElementNS(ns, 'text');
      label.setAttribute('x', '0');
      label.setAttribute('y', '24');
      label.setAttribute('class', 'minimap-marker-label');
      label.textContent = this.shortPlayerName(marker.displayName || marker.steamId);
      group.appendChild(label);

      const title = document.createElementNS(ns, 'title');
      const room = marker.roomName ? ' · ' + marker.roomName : '';
      const status = blindMode || marker.isAlive ? '' : ' (dead)';
      title.textContent = (marker.displayName || marker.steamId) + room + status;
      group.appendChild(title);

      markers.appendChild(group);
    });
    svgEl.appendChild(markers);
  },

  markerTransform(viewport, marker) {
    return this.markerPositionTransform(viewport, marker) + ' rotate(' + this.displayYaw(marker.yaw) + ')';
  },

  markerPositionTransform(viewport, marker) {
    const pos = this.mapPoint(viewport, marker.x, marker.z);
    return 'translate(' + pos.x + ' ' + pos.y + ')';
  },

  markerClass(marker, blindMode) {
    let cls = 'minimap-marker';
    if (!blindMode && !marker.isAlive) cls += ' dead';
    if (marker.isLocal) cls += ' local';
    if (marker.isHost) cls += ' host';
    return cls;
  },

  shortLabel(label) {
    if (!label) return '';
    return label.length > 14 ? label.slice(0, 12) + '…' : label;
  },

  shortPlayerName(name) {
    const text = name == null ? '' : String(name).trim();
    if (!text) return '?';
    return text.length > 10 ? text.slice(0, 9) + '…' : text;
  },
};
