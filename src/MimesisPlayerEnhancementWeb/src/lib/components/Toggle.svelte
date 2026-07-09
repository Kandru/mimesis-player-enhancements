<script lang="ts">
  let {
    checked = false,
    disabled = false,
    label = '',
    onchange,
  }: {
    checked?: boolean;
    disabled?: boolean;
    label?: string;
    onchange?: (checked: boolean) => void;
  } = $props();

  function toggle() {
    if (disabled) return;
    onchange?.(!checked);
  }
</script>

<label class="toggle {disabled ? 'toggle-disabled' : ''}" title={label}>
  <input
    type="checkbox"
    class="toggle-input"
    {checked}
    {disabled}
    onchange={(e) => onchange?.((e.currentTarget as HTMLInputElement).checked)}
  />
  <span class="toggle-track" aria-hidden="true"></span>
</label>
