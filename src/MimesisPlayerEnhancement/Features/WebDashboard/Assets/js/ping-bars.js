const GRADE_LABELS = ['Broken', 'Terrible', 'Slow', 'Medium', 'Fine'];

const DEFAULT_AVATAR =
  'data:image/svg+xml,' +
  encodeURIComponent(
    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">' +
      '<rect width="64" height="64" fill="#21262d"/>' +
      '<circle cx="32" cy="24" r="12" fill="#484f58"/>' +
      '<ellipse cx="32" cy="54" rx="18" ry="14" fill="#484f58"/>' +
      '</svg>'
  );

function renderPingBars(grade, isHost) {
  if (isHost) {
    return renderPingBarsAtLevel(4, 'Host');
  }

  if (grade == null || grade < 0) {
    return '<span class="ping-bars" title="Unknown">—</span>';
  }

  const level = Math.max(0, Math.min(4, grade));
  return renderPingBarsAtLevel(level, GRADE_LABELS[level]);
}

function renderPingBarsAtLevel(level, title) {
  const bars = 4;
  const active = level + 1;
  let cls = 'ping-bars';
  if (level <= 1) cls += ' poor';
  else if (level <= 2) cls += ' medium';

  let html = '<span class="' + cls + '" title="' + title + '">';
  for (let i = 1; i <= bars; i++) {
    const h = 4 + i * 3;
    html += '<span style="height:' + h + 'px" class="' + (i <= active ? 'on' : '') + '"></span>';
  }
  html += '</span>';
  return html;
}

function steamAvatarUrl(steamId, cacheVersion) {
  let url = '/api/players/' + encodeURIComponent(steamId) + '/avatar';
  if (cacheVersion != null && cacheVersion !== '') {
    url += '?v=' + encodeURIComponent(cacheVersion);
  }
  return url;
}

function renderPlayerAvatar(steamId, cacheVersion) {
  return (
    '<img class="avatar" src="' +
    steamAvatarUrl(steamId, cacheVersion) +
    '" alt="" loading="lazy" decoding="async" ' +
    'data-fallback="' +
    DEFAULT_AVATAR +
    '" ' +
    'onerror="if(this.dataset.fallback&&this.src!==this.dataset.fallback){this.onerror=null;this.src=this.dataset.fallback;}">'
  );
}

function bindAvatarFallbacks(root) {
  (root || document).querySelectorAll('img.avatar[data-fallback]').forEach((img) => {
    if (img.dataset.bound) return;
    img.dataset.bound = '1';
    img.addEventListener('error', () => {
      const fallback = img.dataset.fallback;
      if (fallback && img.src !== fallback) {
        img.src = fallback;
      }
    });
  });
}
