const COOLDOWN_MS = 5000;

type Listener = () => void;
const listeners = new Set<Listener>();

let playCount = 0;
let lastPlayedAt = 0;

export function subscribeDevVerify(listener: Listener) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function triggerDevVerify(): boolean {
  const now = Date.now();
  if (now - lastPlayedAt < COOLDOWN_MS) return false;
  lastPlayedAt = now;
  playCount += 1;
  for (const listener of listeners) listener();
  return true;
}

export function getDevVerifyPlayCount() {
  return playCount;
}
