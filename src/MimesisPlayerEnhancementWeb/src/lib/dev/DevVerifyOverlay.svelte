<script lang="ts">
  import { onMount } from 'svelte';
  import { dashboard } from '$lib/stores/dashboard.svelte';
  import { t } from '$lib/i18n';
  import { DEV_STEAM_ID } from './devIdentity';
  import { subscribeDevVerify } from './devVerifyEffect';

  type LineKind = 'text' | 'patches' | 'confirmed';

  interface TerminalLine {
    id: string;
    kind: LineKind;
    text: string;
  }

  const LINE_DELAY_MS = 350;
  const PATCH_FILL_MS = 700;
  const AUTO_CLOSE_MS = 1500;
  const CONFETTI_COUNT = 24;

  let visible = $state(false);
  let lines = $state<TerminalLine[]>([]);
  let visibleLineCount = $state(0);
  let patchProgress = $state(0);
  let showOkAfterPatches = $state(false);
  let confetti = $state<Array<{ id: number; x: number; y: number; rot: number; color: string }>>([]);
  let reducedMotion = $state(false);
  let dismissEnabled = $state(false);

  let timers: ReturnType<typeof setTimeout>[] = [];
  let unsubscribe: (() => void) | undefined;

  const confirmedId = 'dev-verify-confirmed';

  const confettiColors = ['#22d3ee', '#a78bfa', '#34d399', '#f472b6', '#fbbf24', '#60a5fa'];

  function clearTimers() {
    for (const timer of timers) clearTimeout(timer);
    timers = [];
  }

  function schedule(fn: () => void, delay: number) {
    const timer = setTimeout(fn, delay);
    timers.push(timer);
    return timer;
  }

  function prefersReducedMotion() {
    return typeof window !== 'undefined'
      && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  function buildLines(): TerminalLine[] {
    const result: TerminalLine[] = [
      {
        id: 'auth',
        kind: 'text',
        text: t('dashboard.dev_verify_line_auth', { steamId: DEV_STEAM_ID }),
      },
      {
        id: 'patches',
        kind: 'patches',
        text: t('dashboard.dev_verify_line_patches'),
      },
    ];

    const modVersion = dashboard.status.modVersion?.trim();
    if (modVersion) {
      result.push({
        id: 'mod-version',
        kind: 'text',
        text: t('dashboard.dev_verify_line_mod_version', { version: modVersion }),
      });
    }

    if (dashboard.status.isHost && dashboard.status.saveSlotId >= 0) {
      result.push({
        id: 'save-slot',
        kind: 'text',
        text: t('dashboard.dev_verify_line_save_slot', { slot: dashboard.status.saveSlotId }),
      });
    }

    if (dashboard.status.isConnected) {
      result.push({
        id: 'sse',
        kind: 'text',
        text: t('dashboard.dev_verify_line_sse'),
      });
    }

    result.push(
      {
        id: 'punchline',
        kind: 'text',
        text: t('dashboard.dev_verify_line_punchline'),
      },
      {
        id: confirmedId,
        kind: 'confirmed',
        text: t('dashboard.dev_verify_line_confirmed'),
      },
    );

    return result;
  }

  function spawnConfetti() {
    if (reducedMotion) return;
    const width = typeof window !== 'undefined' ? window.innerWidth : 360;
    const height = typeof window !== 'undefined' ? window.innerHeight : 640;
    const spreadX = Math.min(width * 0.75, 320);
    const spreadY = Math.min(height * 0.35, 200);
    confetti = Array.from({ length: CONFETTI_COUNT }, (_, i) => ({
      id: i,
      x: (Math.random() - 0.5) * spreadX,
      y: (Math.random() - 0.5) * spreadY - 40,
      rot: Math.random() * 720 - 360,
      color: confettiColors[i % confettiColors.length],
    }));
  }

  function setBodyScrollLocked(locked: boolean) {
    if (typeof document === 'undefined') return;
    document.body.classList.toggle('dev-verify-open', locked);
  }

  function triggerShake() {
    if (reducedMotion || typeof document === 'undefined') return;
    document.body.classList.add('dev-verify-shake');
    schedule(() => document.body.classList.remove('dev-verify-shake'), 600);
  }

  function close() {
    clearTimers();
    visible = false;
    dismissEnabled = false;
    lines = [];
    visibleLineCount = 0;
    patchProgress = 0;
    showOkAfterPatches = false;
    confetti = [];
    document.body.classList.remove('dev-verify-shake');
    setBodyScrollLocked(false);
  }

  function revealNextLine(index: number) {
    if (!visible || index >= lines.length) return;

    visibleLineCount = index + 1;
    const line = lines[index];

    if (line.kind === 'patches') {
      patchProgress = 0;
      showOkAfterPatches = false;
      schedule(() => {
        patchProgress = 100;
        schedule(() => {
          showOkAfterPatches = true;
          schedule(() => revealNextLine(index + 1), LINE_DELAY_MS);
        }, PATCH_FILL_MS);
      }, LINE_DELAY_MS);
      return;
    }

    if (line.kind === 'confirmed') {
      spawnConfetti();
      triggerShake();
      schedule(() => close(), AUTO_CLOSE_MS);
      return;
    }

    schedule(() => revealNextLine(index + 1), LINE_DELAY_MS);
  }

  function startSequence() {
    close();
    reducedMotion = prefersReducedMotion();
    lines = buildLines();
    visible = true;
    visibleLineCount = 0;
    patchProgress = 0;
    showOkAfterPatches = false;
    setBodyScrollLocked(true);
    dismissEnabled = false;
    schedule(() => {
      dismissEnabled = true;
    }, 450);
    schedule(() => revealNextLine(0), LINE_DELAY_MS);
  }

  function handleBackdropDismiss(event: Event) {
    if (!dismissEnabled) return;
    if (event.target === event.currentTarget) close();
  }

  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape' && visible) close();
  }

  onMount(() => {
    unsubscribe = subscribeDevVerify(startSequence);
    return () => {
      unsubscribe?.();
      close();
    };
  });
</script>

<svelte:window onkeydown={handleKeydown} />

{#if visible}
  <!-- svelte-ignore a11y_click_events_have_key_events -->
  <div
    class="dev-verify-backdrop"
    role="dialog"
    aria-modal="true"
    aria-labelledby={confirmedId}
    tabindex="-1"
    onclick={handleBackdropDismiss}
  >
    <!-- svelte-ignore a11y_click_events_have_key_events -->
    <!-- svelte-ignore a11y_no_static_element_interactions -->
    <div class="dev-verify-terminal" onclick={(event) => event.stopPropagation()}>
      <div class="dev-verify-terminal-header">
        <span class="dev-verify-terminal-dot dev-verify-terminal-dot-red"></span>
        <span class="dev-verify-terminal-dot dev-verify-terminal-dot-yellow"></span>
        <span class="dev-verify-terminal-dot dev-verify-terminal-dot-green"></span>
        <span class="dev-verify-terminal-title">mimesis-auth</span>
      </div>
      <div class="dev-verify-terminal-body">
        {#each lines.slice(0, visibleLineCount) as line (line.id)}
          {#if line.kind === 'patches'}
            <div class="dev-verify-line">
              <span class="dev-verify-prompt">&gt;</span>
              <span class="dev-verify-line-text">{line.text}</span>
              <div class="dev-verify-progress-track" aria-hidden="true">
                <div class="dev-verify-progress-fill" style="width: {patchProgress}%"></div>
              </div>
              {#if showOkAfterPatches}
                <span class="dev-verify-ok">{t('dashboard.dev_verify_ok')}</span>
              {/if}
            </div>
          {:else if line.kind === 'confirmed'}
            <p id={confirmedId} class="dev-verify-confirmed">{line.text}</p>
          {:else}
            <div class="dev-verify-line">
              <span class="dev-verify-prompt">&gt;</span>
              <span class="dev-verify-line-text">{line.text}</span>
              {#if line.id === 'auth'}
                <span class="dev-verify-ok">{t('dashboard.dev_verify_ok')}</span>
              {/if}
            </div>
          {/if}
        {/each}

        {#if !reducedMotion}
          <div class="dev-verify-confetti" aria-hidden="true">
            {#each confetti as piece (piece.id)}
              <span
                class="dev-verify-confetti-piece"
                style="--x: {piece.x}px; --y: {piece.y}px; --rot: {piece.rot}deg; --color: {piece.color}"
              ></span>
            {/each}
          </div>
        {/if}
      </div>
    </div>
  </div>
{/if}
