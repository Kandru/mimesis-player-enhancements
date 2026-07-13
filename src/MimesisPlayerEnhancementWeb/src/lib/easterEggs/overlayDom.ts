import type { EasterEggOverlay } from './types';

const FX_ID = 'easter-egg-fx';

const PARTICLE_COUNTS: Record<EasterEggOverlay, number> = {
  snow: 36,
  stars: 28,
  hearts: 14,
  confetti: 22,
  dots: 24,
};

const CONFETTI_COLORS = ['#e74c3c', '#3498db', '#f1c40f', '#2ecc71', '#9b59b6', '#e67e22'];

function rand(min: number, max: number) {
  return min + Math.random() * (max - min);
}

function animationFor(overlay: EasterEggOverlay, duration: number, delay: number): string {
  switch (overlay) {
    case 'snow':
      return `egg-particle-fall ${duration}s linear ${delay}s infinite`;
    case 'confetti':
      return `egg-particle-fall-spin ${duration}s linear ${delay}s infinite`;
    case 'stars':
    case 'dots':
      return `egg-particle-twinkle ${duration}s ease-in-out ${delay}s infinite`;
    case 'hearts':
      return `egg-particle-float ${duration}s ease-in-out ${delay}s infinite`;
  }
}

function buildParticles(el: HTMLDivElement, overlay: EasterEggOverlay) {
  el.replaceChildren();
  const count = PARTICLE_COUNTS[overlay];

  for (let i = 0; i < count; i++) {
    const particle = document.createElement('span');
    particle.className = `easter-egg-fx-particle easter-egg-fx-particle--${overlay}`;
    particle.style.left = `${rand(0, 100)}%`;

    const delay = rand(0, 8);
    const duration = rand(8, 16);
    particle.style.animation = animationFor(overlay, duration, delay);

    if (overlay === 'stars' || overlay === 'dots') {
      particle.style.top = `${rand(0, 100)}%`;
    }

    if (overlay === 'confetti') {
      particle.style.backgroundColor = CONFETTI_COLORS[i % CONFETTI_COLORS.length];
    }

    if (overlay === 'hearts') {
      particle.textContent = '♥';
      particle.style.top = `${rand(0, 90)}%`;
    }

    el.appendChild(particle);
  }
}

export function applyOverlayToDom(overlay: EasterEggOverlay | null | undefined) {
  let el = document.getElementById(FX_ID) as HTMLDivElement | null;

  if (!overlay) {
    el?.remove();
    return;
  }

  if (!el) {
    el = document.createElement('div');
    el.id = FX_ID;
    el.className = 'easter-egg-fx';
    el.setAttribute('aria-hidden', 'true');
    document.body.appendChild(el);
  }

  if (el.dataset.overlay !== overlay) {
    el.dataset.overlay = overlay;
  }
  buildParticles(el, overlay);
}
