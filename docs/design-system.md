# Design System (`packages/ui`)

The design system owns every visual value in the framework. An application
composes its components and consumes its semantic tokens; it never defines a
color, a spacing or a radius of its own.

**The property this buys you:** a new project is rebranded by editing token
values in one place, and every screen follows — no component edits, no
find-and-replace across features.

## Token architecture

| Layer         | Example                                    | Who may use it       |
| ------------- | ------------------------------------------ | -------------------- |
| **Primitive** | `--color-brand-600`, `--color-neutral-100` | only `semantic.css`  |
| **Semantic**  | `--color-primary`, `--color-surface`       | components and apps  |
| **Component** | `--button-radius` (added when needed)      | the owning component |

Primitives are raw values. Semantic tokens carry meaning and are the **only**
layer a theme or brand overrides. Components resolve everything through
semantic tokens, so they inherit any theme without modification.

`@theme inline` maps Tailwind utilities onto the semantic variables _by
reference_ (`bg-surface` → `var(--color-surface)`), which is what makes a
runtime theme switch repaint the UI without a rebuild.

### Available semantic tokens

`primary`, `primary-hover`, `on-primary`, `background`, `surface`,
`surface-raised`, `border`, `text`, `text-muted`, `success`, `warning`,
`danger`, `on-status`.

Use them as Tailwind utilities: `bg-surface`, `text-text-muted`,
`border-border`, `text-danger`.

## Theming

`useTheme()` exposes the preference (`light` / `dark` / `system`) and
`installTheme()` — called once from the app bootstrap — applies it by setting
`data-theme` on `<html>` and tracks the OS setting. No component is
theme-aware.

Adding a theme means adding a `[data-theme="…"]` block of semantic overrides in
`semantic.css`. Nothing else changes.

## Rebranding a project

1. Ship a stylesheet overriding the **semantic** tokens (and primitives if the
   palette itself changes).
2. Import it after `@enterprise/ui/tokens.css`.
3. Stop.

If step 3 is not enough — if you need to edit a component to get the result —
that component has a tokenization gap. Fix the component **in the framework**,
never patch it in the project: the next project would hit the same wall.

## Component conventions

Each component is a folder with five files:

```
Button/
  Button.vue         implementation
  Button.types.ts    public contract (props, emits, slots) — fully documented
  Button.test.ts     behavior and accessibility tests
  Button.stories.ts  Storybook entry
  index.ts           public exports
```

Rules that hold for every component:

- **No literal values.** No hex colors, no `16px`, no raw shadows or z-indexes.
  A value used twice becomes a semantic token.
- **Optional props accept `undefined` explicitly** (`error?: string | undefined`).
  Consumers bind possibly-absent computed values, and `exactOptionalPropertyTypes`
  rejects a bare `?` for exactly that case.
- **The consumer's `class` wins.** Components disable attribute inheritance and
  merge `attrs.class` through `cn()` (clsx + tailwind-merge). Without this,
  `bg-primary` and a consumer's `bg-surface` both render and the winner depends
  on CSS source order.
- **Accessibility belongs here, not in features.** Label/`for` wiring,
  `aria-describedby`, `aria-invalid`, focus management and announcements are
  solved once in the component so every project inherits them.

## Storybook

```bash
pnpm --filter @enterprise/ui storybook        # http://localhost:6006
pnpm --filter @enterprise/ui build-storybook  # static build (runs in CI)
```

The toolbar theme switcher is the verification surface: every story must look
correct in both themes. A component that breaks after the switch has hardcoded
a value instead of using a token. The a11y addon runs axe on every story and is
configured to fail, not warn.

## Adding a component

1. Create the five-file folder following an existing component.
2. Export it from `src/index.ts`.
3. Write stories for the states that regress silently — error, loading,
   disabled, empty — not just the happy path.
4. Verify both themes in Storybook.
5. Update this document if you introduced a token.

Changing or removing a **semantic token is a breaking change** for every
project on the framework: deprecate it in a release note, never rename it
silently.
