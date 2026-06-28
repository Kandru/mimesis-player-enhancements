const MinimapRenderer = {
  VIEW_SIZE: 1000,

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

  tileCenter(tile) {
    return {
      x: (tile.x + tile.w * 0.5) * this.VIEW_SIZE,
      y: (tile.z + tile.h * 0.5) * this.VIEW_SIZE,
    };
  },

  render(svgEl, data) {
    if (!svgEl || !data) return;

    const ns = 'http://www.w3.org/2000/svg';
    while (svgEl.firstChild) {
      svgEl.removeChild(svgEl.firstChild);
    }

    const tilesById = new Map();
    (data.tiles || []).forEach((tile) => tilesById.set(tile.id, tile));

    const connections = document.createElementNS(ns, 'g');
    connections.setAttribute('class', 'minimap-connections');
    (data.connections || []).forEach((conn) => {
      const from = tilesById.get(conn.from);
      const to = tilesById.get(conn.to);
      if (!from || !to) return;

      const a = this.tileCenter(from);
      const b = this.tileCenter(to);
      const line = document.createElementNS(ns, 'line');
      line.setAttribute('x1', String(a.x));
      line.setAttribute('y1', String(a.y));
      line.setAttribute('x2', String(b.x));
      line.setAttribute('y2', String(b.y));
      connections.appendChild(line);
    });
    svgEl.appendChild(connections);

    const tiles = document.createElementNS(ns, 'g');
    tiles.setAttribute('class', 'minimap-tiles');
    (data.tiles || []).forEach((tile) => {
      const rect = document.createElementNS(ns, 'rect');
      rect.setAttribute('x', String(tile.x * this.VIEW_SIZE));
      rect.setAttribute('y', String(tile.z * this.VIEW_SIZE));
      rect.setAttribute('width', String(Math.max(tile.w * this.VIEW_SIZE, 4)));
      rect.setAttribute('height', String(Math.max(tile.h * this.VIEW_SIZE, 4)));
      rect.setAttribute('rx', '6');
      rect.setAttribute('class', tile.isMainPath ? 'minimap-tile main-path' : 'minimap-tile branch');
      rect.setAttribute('data-label', tile.label || '');

      const title = document.createElementNS(ns, 'title');
      title.textContent = tile.label || 'Room';
      rect.appendChild(title);
      tiles.appendChild(rect);

      if (tile.label && tile.w * this.VIEW_SIZE > 36 && tile.h * this.VIEW_SIZE > 18) {
        const label = document.createElementNS(ns, 'text');
        const center = this.tileCenter(tile);
        label.setAttribute('x', String(center.x));
        label.setAttribute('y', String(center.y));
        label.setAttribute('class', 'minimap-tile-label');
        label.textContent = this.shortLabel(tile.label);
        tiles.appendChild(label);
      }
    });
    svgEl.appendChild(tiles);

    if (data.train) {
      const train = document.createElementNS(ns, 'g');
      train.setAttribute('class', 'minimap-train');
      train.setAttribute('transform', this.markerTransform(data.train));
      train.innerHTML =
        '<rect x="-14" y="-8" width="28" height="16" rx="3"></rect>' +
        '<polygon points="14,-6 22,0 14,6"></polygon>';
      const trainTitle = document.createElementNS(ns, 'title');
      trainTitle.textContent = 'Train';
      train.appendChild(trainTitle);
      svgEl.appendChild(train);
    }

    const markers = document.createElementNS(ns, 'g');
    markers.setAttribute('class', 'minimap-markers');
    (data.markers || []).forEach((marker) => {
      const group = document.createElementNS(ns, 'g');
      group.setAttribute('class', this.markerClass(marker));
      group.setAttribute('transform', this.markerTransform(marker));

      const dot = document.createElementNS(ns, 'circle');
      dot.setAttribute('r', '10');
      group.appendChild(dot);

      const heading = document.createElementNS(ns, 'polygon');
      heading.setAttribute('points', '0,-16 6,-6 -6,-6');
      heading.setAttribute('class', 'minimap-heading');
      group.appendChild(heading);

      const title = document.createElementNS(ns, 'title');
      const room = marker.roomName ? ' · ' + marker.roomName : '';
      const status = marker.isAlive ? '' : ' (dead)';
      title.textContent = (marker.displayName || marker.steamId) + room + status;
      group.appendChild(title);

      markers.appendChild(group);
    });
    svgEl.appendChild(markers);
  },

  markerTransform(marker) {
    const x = marker.x * this.VIEW_SIZE;
    const y = marker.z * this.VIEW_SIZE;
    return 'translate(' + x + ' ' + y + ') rotate(' + (marker.yaw || 0) + ')';
  },

  markerClass(marker) {
    let cls = 'minimap-marker';
    if (!marker.isAlive) cls += ' dead';
    if (marker.isLocal) cls += ' local';
    if (marker.isHost) cls += ' host';
    return cls;
  },

  shortLabel(label) {
    if (!label) return '';
    return label.length > 14 ? label.slice(0, 12) + '…' : label;
  },
};
