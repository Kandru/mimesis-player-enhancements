function parseBool(value) {
  if (value === true) return true;
  if (value === false) return false;
  const text = String(value ?? '').trim().toLowerCase();
  return text === 'true' || text === '1' || text === 'yes' || text === 'on';
}

function formatFloatSettingValue(raw) {
  const n = Math.round(parseFloat(raw) * 100) / 100;
  if (!Number.isFinite(n)) return String(raw ?? '');
  const fixed = n.toFixed(2);
  if (fixed.endsWith('00')) return n.toFixed(1);
  if (fixed.endsWith('0')) return fixed.slice(0, -1);
  return fixed;
}

function clampSettingValue(entry, rawValue) {
  let nextValue = String(rawValue ?? '');
  if (entry.type !== 'Int32' && entry.type !== 'Single') {
    return nextValue;
  }

  const n = entry.type === 'Int32'
    ? parseInt(nextValue, 10)
    : parseFloat(nextValue);
  if (!Number.isFinite(n)) {
    return nextValue;
  }

  let clamped = n;
  if (entry.minValue != null && entry.minValue !== '') {
    const min = entry.type === 'Int32'
      ? parseInt(entry.minValue, 10)
      : parseFloat(entry.minValue);
    if (Number.isFinite(min)) {
      clamped = Math.max(clamped, min);
    }
  }
  if (entry.maxValue != null && entry.maxValue !== '') {
    const max = entry.type === 'Int32'
      ? parseInt(entry.maxValue, 10)
      : parseFloat(entry.maxValue);
    if (Number.isFinite(max)) {
      clamped = Math.min(clamped, max);
    }
  }

  if (entry.type === 'Single') {
    return formatFloatSettingValue(String(clamped));
  }

  return String(Math.trunc(clamped));
}

function formatSettingValue(entry) {
  const value = entry.value;
  if (entry.type === 'Boolean') {
    const onLabel = window.DashboardI18n
      ? window.DashboardI18n.t('dashboard.on')
      : 'On';
    const offLabel = window.DashboardI18n
      ? window.DashboardI18n.t('dashboard.off')
      : 'Off';
    return parseBool(value) ? onLabel : offLabel;
  }
  if (value == null || value === '') return '—';
  return String(value);
}
