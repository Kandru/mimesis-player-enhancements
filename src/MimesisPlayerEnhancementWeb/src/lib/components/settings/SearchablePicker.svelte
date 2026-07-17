<script lang="ts">
  import { t } from '$lib/i18n';
  import { formatCsv, reorderOrdered } from '$lib/listValue';
  import { matchesPickerQuery, type PickerOption } from '$lib/pickerOptions';

  let {
    id,
    options = [],
    value = '',
    values = [],
    multiple = false,
    reorderable = false,
    disabled = false,
    placeholder = '',
    rootClass = 'max-w-md',
    onsave,
  }: {
    id: string;
    options?: PickerOption[];
    value?: string;
    values?: string[];
    multiple?: boolean;
    reorderable?: boolean;
    disabled?: boolean;
    placeholder?: string;
    rootClass?: string;
    onsave: (csv: string) => void;
  } = $props();

  let rootEl = $state<HTMLDivElement | null>(null);
  let inputEl = $state<HTMLInputElement | null>(null);
  let open = $state(false);
  let query = $state('');
  let activeIndex = $state(0);
  let dragIndex = $state<number | null>(null);
  let dragOverIndex = $state<number | null>(null);
  let openUp = $state(false);

  // Max dropdown height (16rem) plus margin; used to decide whether to flip upward.
  const DROPDOWN_SPACE_PX = 272;

  function updateDropDirection() {
    if (!inputEl) {
      openUp = false;
      return;
    }
    const rect = inputEl.getBoundingClientRect();
    const spaceBelow = window.innerHeight - rect.bottom;
    openUp = spaceBelow < DROPDOWN_SPACE_PX && rect.top > spaceBelow;
  }

  const selectedSet = $derived(new Set(multiple ? values : value ? [value] : []));

  const filteredOptions = $derived.by(() => {
    const q = query.trim();
    return options.filter((opt) => {
      if (!matchesPickerQuery(opt, q)) return false;
      if (multiple && selectedSet.has(opt.value)) return false;
      return true;
    });
  });

  type ListItem =
    | { type: 'group'; label: string }
    | { type: 'option'; option: PickerOption; index: number };

  const groupedOptions = $derived.by(() => {
    const groups = new Map<string, PickerOption[]>();
    const ungrouped: PickerOption[] = [];

    for (const opt of filteredOptions) {
      if (opt.group) {
        const list = groups.get(opt.group) ?? [];
        list.push(opt);
        groups.set(opt.group, list);
      } else {
        ungrouped.push(opt);
      }
    }

    const result: ListItem[] = [];
    let optionIndex = 0;
    for (const [label, opts] of groups) {
      result.push({ type: 'group', label });
      for (const option of opts) {
        result.push({ type: 'option', option, index: optionIndex++ });
      }
    }
    for (const option of ungrouped) {
      result.push({ type: 'option', option, index: optionIndex++ });
    }
    return result;
  });

  const selectableOptions = $derived(
    groupedOptions.filter((item) => item.type === 'option'),
  );

  const displayValue = $derived.by(() => {
    if (open || multiple) return query;
    return options.find((opt) => opt.value === value)?.label ?? value;
  });

  const listboxId = $derived(`${id}-listbox`);

  function labelFor(idValue: string) {
    return options.find((opt) => opt.value === idValue)?.label ?? idValue;
  }

  function openDropdown() {
    if (disabled) return;
    updateDropDirection();
    open = true;
    if (!multiple) {
      query = '';
    }
    activeIndex = 0;
  }

  function closeDropdown() {
    open = false;
    query = '';
    activeIndex = 0;
  }

  $effect(() => {
    if (disabled && open) {
      closeDropdown();
    }
  });

  function selectOption(option: PickerOption) {
    if (multiple) {
      onsave(formatCsv([...values, option.value]));
      query = '';
      activeIndex = 0;
      inputEl?.focus();
      return;
    }

    onsave(option.value);
    closeDropdown();
    inputEl?.blur();
  }

  function removeValue(idValue: string) {
    if (!multiple) return;
    onsave(formatCsv(values.filter((v) => v !== idValue)));
  }

  function onBadgeDragStart(index: number) {
    if (!reorderable || disabled) return;
    dragIndex = index;
  }

  function onBadgeDragOver(ev: DragEvent, index: number) {
    if (!reorderable || dragIndex === null) return;
    ev.preventDefault();
    dragOverIndex = index;
  }

  function onBadgeDrop(index: number) {
    if (!reorderable || dragIndex === null) return;
    onsave(formatCsv(reorderOrdered(values, dragIndex, index)));
    dragIndex = null;
    dragOverIndex = null;
  }

  function onBadgeDragEnd() {
    dragIndex = null;
    dragOverIndex = null;
  }

  function onInputInput(ev: Event) {
    const el = ev.currentTarget as HTMLInputElement;
    query = el.value;
    if (!open) updateDropDirection();
    open = true;
    activeIndex = 0;
  }

  function onDocumentPointerDown(ev: PointerEvent) {
    if (!open || !rootEl) return;
    if (!rootEl.contains(ev.target as Node)) {
      closeDropdown();
    }
  }

  function onKeydown(ev: KeyboardEvent) {
    if (disabled) return;

    if (ev.key === 'Escape') {
      if (open) {
        ev.preventDefault();
        closeDropdown();
      }
      return;
    }

    if (!open && (ev.key === 'ArrowDown' || ev.key === 'Enter')) {
      ev.preventDefault();
      openDropdown();
      return;
    }

    if (!open) {
      if (multiple && ev.key === 'Backspace' && query === '' && values.length > 0) {
        ev.preventDefault();
        removeValue(values[values.length - 1]);
      }
      return;
    }

    if (ev.key === 'ArrowDown') {
      ev.preventDefault();
      if (selectableOptions.length === 0) return;
      activeIndex = Math.min(activeIndex + 1, selectableOptions.length - 1);
      return;
    }

    if (ev.key === 'ArrowUp') {
      ev.preventDefault();
      if (selectableOptions.length === 0) return;
      activeIndex = Math.max(activeIndex - 1, 0);
      return;
    }

    if (ev.key === 'Enter') {
      ev.preventDefault();
      const option = selectableOptions[activeIndex]?.option;
      if (option) selectOption(option);
      return;
    }

    if (multiple && ev.key === 'Backspace' && query === '' && values.length > 0) {
      ev.preventDefault();
      removeValue(values[values.length - 1]);
    }
  }

  $effect(() => {
    document.addEventListener('pointerdown', onDocumentPointerDown);
    return () => document.removeEventListener('pointerdown', onDocumentPointerDown);
  });
</script>

<div class="picker-root {rootClass}" bind:this={rootEl}>
  {#if multiple && values.length > 0}
    <div class="picker-badges" role="list" aria-label={t('dashboard.picker_selected_items')}>
      {#each values as idValue, index (idValue)}
        <span
          class="picker-badge {reorderable ? 'picker-badge-reorderable' : ''} {dragOverIndex === index ? 'picker-badge-drag-over' : ''}"
          role="listitem"
          title={reorderable ? t('dashboard.picker_reorder_hint', { label: labelFor(idValue) }) : idValue}
          draggable={reorderable && !disabled}
          ondragstart={() => onBadgeDragStart(index)}
          ondragover={(ev) => onBadgeDragOver(ev, index)}
          ondrop={() => onBadgeDrop(index)}
          ondragend={onBadgeDragEnd}
        >
          {#if reorderable}
            <span class="picker-badge-grip" aria-hidden="true">⋮⋮</span>
          {/if}
          <span class="picker-badge-label">{labelFor(idValue)}</span>
          {#if !disabled}
            <button
              type="button"
              class="picker-badge-remove"
              aria-label={t('dashboard.picker_remove_item', { label: labelFor(idValue) })}
              ondragstart={(ev) => ev.preventDefault()}
              onclick={() => removeValue(idValue)}
            >
              ×
            </button>
          {/if}
        </span>
      {/each}
    </div>
  {/if}

  <div class="picker-input-wrap">
    <input
      bind:this={inputEl}
      {id}
      class="input picker-input"
      type="text"
      role="combobox"
      aria-autocomplete="list"
      aria-expanded={open}
      aria-controls={listboxId}
      {disabled}
      value={displayValue}
      placeholder={placeholder || t('dashboard.picker_search_placeholder')}
      onfocus={openDropdown}
      oninput={onInputInput}
      onkeydown={onKeydown}
    />
    <span class="picker-chevron" aria-hidden="true">▾</span>
  </div>

  {#if open && !disabled}
    <ul id={listboxId} class="picker-dropdown {openUp ? 'picker-dropdown-up' : ''}" role="listbox">
      {#if selectableOptions.length === 0}
        <li class="picker-empty" role="presentation">{t('dashboard.picker_no_results')}</li>
      {:else}
        {#each groupedOptions as item, index (`${item.type}-${item.type === 'group' ? item.label : item.option.value}-${index}`)}
          {#if item.type === 'group'}
            <li class="picker-group" role="presentation">{item.label}</li>
          {:else}
            <li role="presentation">
              <button
                type="button"
                role="option"
                aria-selected={!multiple && value === item.option.value}
                class="picker-option {item.index === activeIndex ? 'picker-option-active' : ''}"
                onmousedown={(ev) => ev.preventDefault()}
                onclick={() => selectOption(item.option)}
              >
                <span class="picker-option-label">{item.option.label}</span>
                <span class="picker-option-id">{item.option.value}</span>
              </button>
            </li>
          {/if}
        {/each}
      {/if}
    </ul>
  {/if}
</div>
