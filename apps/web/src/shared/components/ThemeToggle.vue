<script setup lang="ts">
/**
 * -----------------------------------------------------------------------------
 * ThemeToggle
 * -----------------------------------------------------------------------------
 *
 * Segmented control that switches between light, dark and system themes.
 *
 * Responsibilities:
 * - Renders the three options with the active one marked.
 * - Delegates every state change to the design system's `useTheme`.
 *
 * It contains no theming logic: the theme engine repaints the UI through the
 * semantic tokens. It lives in the app (not in `@enterprise/ui`) because the
 * placement of a theme control is a product decision, not a design-system one.
 */
import { ComputerDesktopIcon, MoonIcon, SunIcon } from "@heroicons/vue/24/outline";
import { useTheme, type ThemePreference } from "@enterprise/ui";

const { preference, setTheme } = useTheme();

const options: ReadonlyArray<{ value: ThemePreference; label: string; icon: unknown }> = [
  { value: "light", label: "Light", icon: SunIcon },
  { value: "dark", label: "Dark", icon: MoonIcon },
  { value: "system", label: "System", icon: ComputerDesktopIcon },
];
</script>

<template>
  <div
    role="radiogroup"
    aria-label="Color theme"
    class="inline-flex rounded-lg border border-border bg-surface p-0.5"
  >
    <button
      v-for="option in options"
      :key="option.value"
      type="button"
      role="radio"
      :aria-checked="preference === option.value"
      :title="option.label"
      class="inline-flex cursor-pointer items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm transition-colors"
      :class="
        preference === option.value
          ? 'bg-primary text-on-primary'
          : 'text-text-muted hover:text-text'
      "
      @click="setTheme(option.value)"
    >
      <component :is="option.icon" class="size-4" aria-hidden="true" />
      <span class="sr-only sm:not-sr-only">{{ option.label }}</span>
    </button>
  </div>
</template>
