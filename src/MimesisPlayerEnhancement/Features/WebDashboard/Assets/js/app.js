(function () {
  const view = document.getElementById('view');
  const subtitle = document.getElementById('subtitle');
  const nav = document.getElementById('main-nav');
  const toast = document.getElementById('toast');

  let status = { inSession: false, isHost: false, saveSlotId: -1, modVersion: '', listenUrl: '', snapshotVersion: 0 };
  let statusTimer = null;
  let pageTimer = null;

  function showToast(message) {
    toast.textContent = message;
    toast.classList.add('show');
    setTimeout(() => toast.classList.remove('show'), 3500);
  }

  function parseRoute() {
    const hash = location.hash || '#/waiting';
    const parts = hash.replace(/^#\/?/, '').split('/').filter(Boolean);
    return {
      page: parts[0] || 'waiting',
      steamId: parts[1] || null,
    };
  }

  function setSessionMode(inSession) {
    document.body.classList.toggle('waiting', !inSession);
    document.body.classList.toggle('in-session', inSession);
  }

  function setActiveNav(page) {
    nav.querySelectorAll('a[data-route]').forEach((a) => {
      a.classList.toggle('active', a.dataset.route === page);
    });
    nav.querySelectorAll('.host-only').forEach((el) => {
      el.style.display = status.isHost ? '' : 'none';
    });
    nav.style.display = status.inSession ? '' : 'none';
  }

  function updateSubtitle() {
    if (!status.inSession) {
      subtitle.textContent = status.modVersion ? 'v' + status.modVersion + ' · Waiting for session' : 'Waiting for session';
      return;
    }

    const parts = [];
    if (status.modVersion) parts.push('v' + status.modVersion);
    parts.push(status.isHost ? 'Host' : 'Guest');
    if (status.saveSlotId >= 0) parts.push('Slot ' + status.saveSlotId);
    subtitle.textContent = parts.join(' · ');
  }

  async function refreshStatus() {
    const wasInSession = status.inSession;
    try {
      status = await Api.getStatus();
      setSessionMode(status.inSession);
      updateSubtitle();
      setActiveNav(parseRoute().page);

      const route = parseRoute();
      if (!status.inSession && route.page !== 'waiting') {
        location.hash = '#/waiting';
      } else if (
        status.inSession &&
        (route.page === 'waiting' || !location.hash || location.hash === '#')
      ) {
        location.hash = '#/players';
      }

      return wasInSession !== status.inSession;
    } catch (e) {
      setSessionMode(false);
      subtitle.textContent = 'Cannot reach dashboard API';
      return wasInSession;
    }
  }

  function renderWaiting() {
    view.innerHTML =
      '<div class="waiting-panel">' +
      '<div class="waiting-card">' +
      '<div class="spinner" role="progressbar" aria-label="Waiting for game session"></div>' +
      '<h2>No active session</h2>' +
      '<p>Host or join a game in Mimesis first. This dashboard will connect automatically once a session is running.</p>' +
      '</div>' +
      '</div>';
  }

  function playerBadges(p) {
    let html = '';
    if (p.isHost) html += ' <span class="badge host">Host</span>';
    if (p.isLocal) html += ' <span class="badge local">You</span>';
    if (p.isBanned) html += ' <span class="badge banned">Banned</span>';
    return html;
  }

  function renderPlayerActions(p) {
    if (!status.isHost || p.isLocal) return '';
    return (
      '<div class="player-actions">' +
      '<button class="btn btn-outlined" data-action="kick" data-steam="' + p.steamId + '">Kick</button>' +
      '<button class="btn btn-danger" data-action="ban" data-steam="' + p.steamId + '">Ban</button>' +
      (p.isBanned ? '<button class="btn btn-text" data-action="unban" data-steam="' + p.steamId + '">Unban</button>' : '') +
      '</div>'
    );
  }

  async function renderPlayers() {
    try {
      const data = await Api.getPlayers();
      const players = data.players || [];
      if (players.length === 0) {
        view.innerHTML =
          '<h2 class="page-heading">Players</h2>' +
          '<div class="empty-state"><p>No players connected.</p></div>';
        return;
      }

      let html = '<h2 class="page-heading">Players</h2><div class="players-list">';
      players.forEach((p) => {
        const statsLink = status.isHost
          ? ' <a class="btn-text btn" href="#/player/' + p.steamId + '">Stats</a>'
          : '';
        html +=
          '<article class="surface-card"><div class="player-row">' +
          renderPlayerAvatar(p.steamId, status.snapshotVersion) +
          '<div class="player-meta">' +
          '<strong>' + escapeHtml(p.displayName) + '</strong>' + playerBadges(p) +
          '<br><small>Steam ' + p.steamId + '</small>' +
          '</div>' +
          '<div class="player-status">' +
          renderPingBars(p.networkGrade, p.isHost || (status.isHost && p.isLocal)) +
          statsLink +
          '</div>' +
          renderPlayerActions(p) +
          '</div></article>';
      });
      html += '</div>';
      view.innerHTML = html;
      bindAvatarFallbacks(view);

      view.querySelectorAll('[data-action]').forEach((btn) => {
        btn.addEventListener('click', async () => {
          const steamId = btn.dataset.steam;
          const action = btn.dataset.action;
          if (!confirm('Confirm ' + action + ' for player ' + steamId + '?')) return;
          try {
            const res = await Api.postAction(steamId, action);
            showToast(res.message || 'Done');
            renderPlayers();
          } catch (e) {
            showToast(e.message);
          }
        });
      });
    } catch (e) {
      view.innerHTML =
        '<div class="empty-state"><p>Failed to load players.</p></div>';
    }
  }

  function formatDuration(seconds) {
    if (!seconds) return '0m';
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    return h > 0 ? h + 'h ' + m + 'm' : m + 'm';
  }

  async function renderLeaderboard() {
    if (!status.isHost) {
      view.innerHTML =
        '<div class="empty-state"><p>Leaderboard is only available to the host.</p></div>';
      return;
    }

    try {
      const data = await Api.getLeaderboard();
      const connected = new Set((data.connectedSteamIds || []).map(String));
      const entries = data.entries || [];

      if (entries.length === 0) {
        view.innerHTML =
          '<h2 class="page-heading">Leaderboard</h2>' +
          '<div class="empty-state"><p>No leaderboard data for this save slot yet.</p></div>';
        return;
      }

      let html =
        '<h2 class="page-heading">Leaderboard</h2>' +
        '<p class="table-meta">Updated ' + (data.updatedAtUtc || '—') + '</p>' +
        '<div class="table-wrap"><table><thead><tr>' +
        '<th>Player</th><th>Currency</th><th>Mimics</th><th>Items</th><th>K/D/R</th><th>Time</th>' +
        '</tr></thead><tbody>';

      entries.forEach((e) => {
        const cls = connected.has(String(e.steamId)) ? ' class="connected"' : '';
        html +=
          '<tr' + cls + '>' +
          '<td><a href="#/player/' + e.steamId + '">' + escapeHtml(e.displayName) + '</a></td>' +
          '<td>' + e.currencyEarned + '</td>' +
          '<td>' + e.mimicEncounterCount + '</td>' +
          '<td>' + e.itemCarryCount + '</td>' +
          '<td>' + e.kills + '/' + e.deaths + '/' + e.revives + '</td>' +
          '<td>' + formatDuration(e.totalConnectedSeconds) + '</td>' +
          '</tr>';
      });

      html += '</tbody></table></div>';
      html += '<p class="table-caption">Highlighted rows are currently connected.</p>';
      view.innerHTML = html;
    } catch (e) {
      view.innerHTML =
        '<div class="empty-state"><p>Failed to load leaderboard.</p></div>';
    }
  }

  function renderStatCard(label, value) {
    return '<article class="stat-card"><small>' + label + '</small><strong>' + value + '</strong></article>';
  }

  async function renderPlayerDetail(steamId) {
    if (!status.isHost) {
      view.innerHTML =
        '<div class="empty-state"><p>Player statistics are only available to the host.</p></div>';
      return;
    }

    view.innerHTML =
      '<div class="loading-panel">' +
      '<div class="spinner" role="progressbar" aria-label="Loading statistics"></div>' +
      '</div>';

    try {
      const doc = await Api.getPlayerStats(steamId);
      const g = doc.global || {};
      const gCounters = g.counters || {};
      const cs = doc.currentSession || null;
      const s = (cs && cs.counters) || {};

      let html =
        '<a class="back-link" href="#/leaderboard">&larr; Back to leaderboard</a>' +
        '<div class="player-detail-header">' +
        renderPlayerAvatar(steamId, status.snapshotVersion) +
        '<div>' +
        '<h2 class="page-heading" style="margin:0">' + escapeHtml(resolvePlayerName(doc.displayName, steamId) || steamId) + '</h2>' +
        '<small>Steam ' + steamId + '</small>' +
        '</div></div>';

      html += '<h3 class="section-title">Global</h3><div class="stat-grid">';
      html += renderStatCard('Currency', gCounters.currencyEarned ?? 0);
      html += renderStatCard('Mimic encounters', gCounters.mimicEncounterCount ?? 0);
      html += renderStatCard('Items carried', gCounters.itemCarryCount ?? 0);
      html += renderStatCard('Kills', gCounters.kills ?? 0);
      html += renderStatCard('Deaths', gCounters.deaths ?? 0);
      html += renderStatCard('Revives', gCounters.revives ?? 0);
      html += renderStatCard('Voice events', gCounters.voiceEvents ?? 0);
      html += renderStatCard('Ally damage', gCounters.damageToAlly ?? 0);
      html += renderStatCard('Connected time', formatDuration(gCounters.totalConnectedSeconds ?? 0));
      html += renderStatCard('Sessions', g.sessionsCompleted ?? 0);
      html += '</div>';

      if (cs) {
        html += '<h3 class="section-title">Current session</h3><div class="stat-grid">';
        html += renderStatCard('Currency', s.currencyEarned ?? 0);
        html += renderStatCard('Kills', s.kills ?? 0);
        html += renderStatCard('Deaths', s.deaths ?? 0);
        html += renderStatCard('Revives', s.revives ?? 0);
        html += '</div>';
      }

      view.innerHTML = html;
      bindAvatarFallbacks(view);
    } catch (e) {
      view.innerHTML =
        '<div class="empty-state"><p>' + escapeHtml(e.message) + '</p></div>';
    }
  }

  function escapeHtml(text) {
    const d = document.createElement('div');
    d.textContent = text;
    return d.innerHTML;
  }

  function resolvePlayerName(name, steamId) {
    const id = String(steamId);
    const resolved = name != null ? String(name).trim() : '';
    return resolved && resolved !== id ? resolved : null;
  }

  async function render() {
    const route = parseRoute();
    setActiveNav(route.page);

    if (!status.inSession) {
      renderWaiting();
      return;
    }

    if (route.page === 'waiting') {
      location.hash = '#/players';
      return;
    }

    if (route.page === 'players') {
      await renderPlayers();
    } else if (route.page === 'leaderboard') {
      await renderLeaderboard();
    } else if (route.page === 'player' && route.steamId) {
      await renderPlayerDetail(route.steamId);
    } else {
      location.hash = '#/players';
    }
  }

  function startTimers() {
    if (statusTimer) clearInterval(statusTimer);
    statusTimer = setInterval(async () => {
      const sessionChanged = await refreshStatus();
      if (sessionChanged) await render();
    }, 2000);

    if (pageTimer) clearInterval(pageTimer);
    pageTimer = setInterval(() => {
      const route = parseRoute();
      if (!status.inSession) return;
      if (route.page === 'players') renderPlayers();
      else if (route.page === 'leaderboard') renderLeaderboard();
    }, 3000);
  }

  window.addEventListener('hashchange', () => render());

  refreshStatus().then(async (sessionChanged) => {
    if (status.inSession && (!location.hash || location.hash === '#')) {
      location.hash = '#/players';
    }
    await render();
    startTimers();
  });
})();
