# Design System (`packages/ui`)

The authoritative reference for every visual decision in the framework.
**Read this before changing anything a user can see.**

The design system owns every visual value. An application composes its
components and consumes its semantic tokens; it never defines a colour, a
radius, a shadow or a duration of its own.

**The property this buys:** a new project is rebranded by editing token values
in one place, and every screen follows — no component edits, no find-and-replace
across features. Every rule below exists to protect that property.

### Three documents, one system

The application has two surfaces with opposite needs — a dense work tool and a
public brochure — so the reference is split by audience:

| Document                                           | Covers                                                             |
| -------------------------------------------------- | ------------------------------------------------------------------ |
| **This file**                                      | The shared foundation: tokens, accessibility, conventions, theming |
| [design-system-admin.md](design-system-admin.md)   | The private area: density, tables, forms, working navigation       |
| [design-system-public.md](design-system-public.md) | The public site: rhythm, type scale, sections, CTA                 |

The two surface documents describe **different uses of the same tokens**, never
a second set of them. If either starts defining its own colours or radii, the
rebranding property is gone — and that property is most of the reason this
template exists.

---

## 1. Token architecture

Three layers, each with one job.

```
Primitive   raw values, no meaning          --color-brand-600, --radius-primitive-md
     ↓
Semantic    meaning, no component           --color-primary, --radius-control
     ↓
Component   one component's exception       --card-radius, --dialog-shadow
```

| Layer         | Who may reference it | Changes when                     |
| ------------- | -------------------- | -------------------------------- |
| **Primitive** | only `semantic.css`  | the palette itself changes       |
| **Semantic**  | components and apps  | a theme or brand changes         |
| **Component** | the owning component | one component needs an exception |

Files: `packages/ui/src/styles/{primitives,semantic,components}.css`, composed
by `tokens.css`.

### Which layer does a new token belong to?

Ask in order:

1. **Would every component change together?** → semantic layer.
   (_"Our corners are sharper"_ → change `--radius-control`.)
2. **Would only this one component change?** → component layer.
   (_"Our cards are flat but dialogs still float"_ → `--card-shadow`.)
3. **Is it a raw value with no meaning yet?** → primitive, then immediately
   give it a semantic name. A primitive nothing references is dead weight.

A component token that no project would plausibly change alone is **not
justified** — it is indirection without benefit. That is why buttons have a
radius token but not a font-size token: nobody rebrands one button's text size
independently of the rest of the UI.

### `@theme inline`

Semantic tokens are declared in `@theme inline`, which maps Tailwind utilities
onto the variables **by reference** (`bg-surface` → `var(--color-surface)`).
That indirection is what makes a runtime theme switch repaint the whole UI
without a rebuild. Declaring them in a plain `@theme` would inline the value at
build time and break theming.

---

## 2. Colour

### Primitives

| Scale     | Steps                                                              | Notes                               |
| --------- | ------------------------------------------------------------------ | ----------------------------------- |
| `brand`   | 50 · 100 · 200 · 300 · 400 · 500 · 600 · 700 · 800 · 900           | the only hue that changes per brand |
| `neutral` | 0 · 50 · 100 · 200 · 300 · 400 · 500 · 600 · 700 · 800 · 900 · 950 | surfaces, text, borders             |
| `success` | 500 · 600                                                          |                                     |
| `warning` | 500 · 600                                                          |                                     |
| `danger`  | 500 · 600                                                          |                                     |

Declared in `oklch()`: it is perceptually uniform, so a scale keeps even
visual steps and a dark-theme variant of the same hue stays recognisably the
same colour.

### Semantic tokens — **the only ones components may use**

| Token            | Utility             | Use for                            |
| ---------------- | ------------------- | ---------------------------------- |
| `primary`        | `bg-primary`        | the one action a screen wants most |
| `primary-hover`  | `bg-primary-hover`  | hover/active of primary            |
| `on-primary`     | `text-on-primary`   | text on a primary fill             |
| `background`     | `bg-background`     | the page behind everything         |
| `surface`        | `bg-surface`        | cards, headers, panels             |
| `surface-raised` | `bg-surface-raised` | dialogs, toasts, popovers          |
| `surface-sunken` | `bg-surface-sunken` | an empty slot: skeletons, tracks   |
| `border`         | `border-border`     | every divider and outline          |
| `text`           | `text-text`         | body copy                          |
| `text-muted`     | `text-text-muted`   | secondary copy, placeholders       |
| `success`        | `text-success`      | a completed outcome                |
| `warning`        | `text-warning`      | something needing attention        |
| `danger`         | `text-danger`       | an error or a destructive action   |
| `on-status`      | `text-on-status`    | text on a status fill              |

There is deliberately **no `secondary` colour**. A second brand colour is the
fastest way to a UI where nothing stands out; secondary actions use the
`secondary` _button variant_, which is neutral-filled.

---

## 3. Shape

| Semantic token     | Role                          | Used by       |
| ------------------ | ----------------------------- | ------------- |
| `--radius-control` | things you click or type into | Button, Input |
| `--radius-surface` | things that contain content   | Card, Dialog  |
| `--radius-pill`    | things that are fully round   | Badge, Avatar |

Named by **role, not size**. `rounded-md` scattered across components means a
rebrand is a search-and-replace; `--radius-control` means it is one line.
Controls and inputs share a token so a button and a field sitting in the same
form row always align visually.

---

## 4. Elevation

| Semantic token     | Meaning                                | Used by       |
| ------------------ | -------------------------------------- | ------------- |
| `--shadow-raised`  | sits on the page                       | (opt-in)      |
| `--shadow-overlay` | floats above it, blocking or transient | Dialog, Toast |

Two levels only. Every additional level is an invitation to pick arbitrarily,
and depth stops meaning anything when everything has some.

Shadows are short and soft: long shadows read as decoration and fall apart on
a dark surface, where there is no light to justify them.

**Cards are flat by default** (`--card-shadow: none`). Depth is reserved for
things that are actually above the page; a page of shadowed cards is a page
where nothing is emphasised.

---

## 5. Motion

| Token             | Value                          | For                                                          |
| ----------------- | ------------------------------ | ------------------------------------------------------------ |
| `--duration-fast` | 150ms                          | colour, background, border — state the user did not initiate |
| `--duration-base` | 200ms                          | transform, size — movement the user did initiate             |
| `--ease-standard` | `cubic-bezier(0.4, 0, 0.2, 1)` | everything                                                   |

These are wired to Tailwind's `--default-transition-duration` and
`--default-transition-timing-function`, so **every `transition-*` already in
the codebase inherits them**. Write `transition-colors` and it is tokenised;
specify a duration only when this element genuinely differs.

Rules:

- Never animate `width`/`height`/`top`/`left`. Animate `transform` and
  `opacity`, which the compositor can handle without relayout.
- Nothing that blocks input may animate longer than `--duration-base`.
- Motion must be **decorative, never informational**: if the only way to know
  something happened is having watched an animation, the design is broken for
  anyone with `prefers-reduced-motion`.

---

## 6. State

| Token                 | Value | Meaning                         |
| --------------------- | ----- | ------------------------------- |
| `--opacity-disabled`  | 0.6   | unavailable, but still readable |
| `--focus-ring-width`  | 2px   | keyboard focus                  |
| `--focus-ring-offset` | 2px   | gap between element and ring    |

### Interactive state matrix

Every interactive component implements these, in this precedence order:

| Priority | State      | Treatment                                                     |
| -------- | ---------- | ------------------------------------------------------------- |
| 1        | `disabled` | `--opacity-disabled`, `cursor-not-allowed`, no pointer events |
| 2        | `loading`  | spinner replaces the icon, control stays sized, `aria-busy`   |
| 3        | `active`   | pressed treatment                                             |
| 4        | `focus`    | focus ring (never removed)                                    |
| 5        | `hover`    | one step of colour shift                                      |
| 6        | default    | base                                                          |

The focus ring is applied once, globally, in `tokens.css` with `:where()` — it
costs no specificity, applies to every interactive element automatically, and
a component can still override it. **Removing focus styling is never an
acceptable design choice.** If it looks wrong, change the token.

---

## 7. Component catalogue

All components live in `packages/ui/src/components/<Name>/`.

### Button

| Prop      | Values                                       | Default   |
| --------- | -------------------------------------------- | --------- |
| `variant` | `primary` · `secondary` · `ghost` · `danger` | `primary` |
| `size`    | `sm` · `md` · `lg`                           | `md`      |
| `type`    | `button` · `submit` · `reset`                | `button`  |
| `loading` | boolean — shows a spinner, keeps the width   | `false`   |
| `block`   | boolean — full width                         | `false`   |

One `primary` per screen region. `danger` is for destructive actions only —
using it for emphasis trains users to ignore it where it matters.

### Input

| Prop    | Notes                                                                                                                                 |
| ------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `label` | **required** — an unlabelled field is unusable with a screen reader. Use `labelHidden` when the design needs it hidden, never omit it |
| `error` | sets `aria-invalid` and wires `aria-describedby`                                                                                      |
| `hint`  | helper text, also wired to `aria-describedby`                                                                                         |
| `type`  | `text` · `email` · `password` · `search` · `tel` · `url` · `number`                                                                   |
| slots   | `prefix`, `suffix`                                                                                                                    |

### Textarea

Same contract as `Input` (`label` required, `hint`, `error`, `required`,
`disabled`, `readonly`), plus `rows` (default 5) and `maxlength`.

Use it whenever the expected answer is prose: a single-line box tells the
person "one line is enough", and they answer accordingly.

Setting `maxlength` shows a live counter, announced politely — a limit
discovered on submit means rewriting a message already finished.

### Card

| Prop           | Notes                                                                                                    |
| -------------- | -------------------------------------------------------------------------------------------------------- |
| `title`        | rendered at `headingLevel`                                                                               |
| `headingLevel` | `h2` · `h3` · `h4` — pick what the page outline needs, never what looks right; size comes from the token |
| `flush`        | removes padding, for tables and lists that own their edges                                               |
| slots          | `default`, `header`, `actions`, `footer`                                                                 |

### Badge

`variant`: `neutral` · `success` · `warning` · `danger` · `info`.

`srLabel` exists because colour alone is not information: a red badge reading
"3" means nothing to a screen reader, or to anyone who cannot distinguish it
from the green one.

### Avatar

`size`: `sm` · `md` · `lg`. `name` is **required** — it produces the initials
fallback _and_ the accessible name. Set `decorative` when the name is already
adjacent in the DOM, so it is not announced twice.

### Skeleton

`shape`: `text` · `block` · `circle`, plus `width` / `height`.

Use it — not a spinner — whenever the eventual layout is known: the space is
reserved up front, so arriving data does not shift the page, and the user sees
the shape of what is coming. A spinner is right only when the result's shape is
genuinely unknown.

Compose several to match the layout being replaced; that is what makes the
swap invisible. Skeletons are `aria-hidden` (a screen reader gains nothing from
"loading" repeated per placeholder) — announce the state on the container
instead, with `aria-busy`. The pulse stops under `prefers-reduced-motion`.

### Dialog

`title` required, `description` optional, `persistent` disables
dismiss-on-backdrop. Traps focus, restores it on close, closes on Escape
unless persistent.

Use a Dialog when the user must decide before continuing. If they do not need
to decide, use a Toast.

### Toast

`variant`: `info` · `success` · `warning` · `danger`.

`danger` toasts **never auto-dismiss** — an error nobody read is reported as
"nothing happened". Everything else defaults to 5s. The stack is capped
(`max`, default 4) so it cannot cover the UI it is describing.

A toast can be missed by design; anything that must not be missed is a Dialog.

---

## 8. Accessibility contract

Non-negotiable, and enforced in component tests:

| Requirement           | Minimum                                         |
| --------------------- | ----------------------------------------------- |
| Text contrast         | 4.5:1 (3:1 for ≥18px)                           |
| UI/border contrast    | 3:1                                             |
| Focus indicator       | visible, 3:1 against the adjacent surface       |
| Target size           | 24×24px minimum                                 |
| Colour as information | never alone — pair with text, icon or `srLabel` |

Accessibility lives **in the component, not in features**: label wiring,
`aria-describedby`, `aria-invalid`, focus management and live-region
announcements are solved once so every project inherits them. A feature that
has to add ARIA to use a component correctly is a component with a gap.

Both themes must pass. The Storybook a11y addon runs axe on every story and is
configured to **fail, not warn**.

---

## 9. Component conventions

Five files per component:

```
Button/
  Button.vue         implementation
  Button.types.ts    public contract (props, emits, slots) — fully documented
  Button.test.ts     behaviour and accessibility tests
  Button.stories.ts  Storybook entry
  index.ts           public exports
```

- **No literal values.** No hex, no `16px`, no raw shadow, duration or
  z-index. A value used twice becomes a token.
- **Optional props accept `undefined` explicitly** (`error?: string | undefined`)
  — consumers bind possibly-absent computed values, and
  `exactOptionalPropertyTypes` rejects a bare `?` for exactly that case.
- **The consumer's `class` wins.** Components disable attribute inheritance and
  merge through `cn()` (clsx + tailwind-merge). Without it, `bg-primary` and a
  consumer's `bg-surface` both render and the winner depends on CSS order.
- **Removal over hiding.** An element that should not be available is removed
  from the DOM, not visually hidden — hidden elements stay focusable and
  announced (see `v-can`, `v-feature`).

---

## 10. Theming

`useTheme()` exposes the preference (`light` · `dark` · `system`);
`installTheme()`, called once from the app bootstrap, applies it by setting
`data-theme` on `<html>` and tracks the OS setting. **No component is
theme-aware.**

Adding a theme = adding one `[data-theme="…"]` block of semantic overrides in
`semantic.css`. Nothing else changes.

### Surfaces

A **theme** answers "which palette"; a **surface** answers "which rhythm and
scale". Both are semantic-token overrides, and both are applied by an
attribute on an ancestor: `data-theme` on `<html>`, `data-surface` on a shell.

```css
[data-surface="public"] {
  --semantic-text-display: var(--font-size-primitive-display);
  --semantic-space-band: var(--space-primitive-band);
}
```

`PublicLayout` sets `data-surface="public"`; everything inside it inherits the
larger type and the wider rhythm without a single page knowing. The private
area uses the defaults on `:root`.

Adding a surface follows the same three rules as adding a theme: **semantic
tokens only**, defined for every theme, documented in the surface's file.

### Rebranding a project

1. Ship a stylesheet overriding the **semantic** tokens (and primitives if the
   palette itself changes).
2. Import it after `@enterprise/ui/tokens.css`.
3. Stop.

If step 3 is not enough — if you must edit a component to get the result —
that component has a **tokenisation gap**. Fix the component _in the
framework_; never patch it in the project, or the next project hits the same
wall.

---

## 11. Changing the system

**Adding a component**

1. Create the five-file folder following an existing component.
2. Export it from `src/index.ts`.
3. Write stories for the states that regress silently — error, loading,
   disabled, empty — not only the happy path.
4. Verify both themes in Storybook.
5. Update this document.

**Adding a token** — decide its layer with the rule in §1, add it, document it
here in the same commit.

**Changing or removing a semantic token is a breaking change** for every
project on the framework. Deprecate it in a release note; never rename it
silently.

**Tailwind sources.** Tailwind only scans what `@source` lists in
`apps/web/src/assets/styles/main.css`. A new package or module frontend that
ships markup must be added there, or its classes are silently missing from the
bundle — components render unstyled with no build error.

---

## 12. Responsive & mobile

**The application supports mobile 100%.** Not "degrades acceptably" — every
page and every control must be fully usable on a 375px phone with a finger.
This section is as binding as the accessibility contract.

### Breakpoints

Tailwind's defaults, used as-is so the scale matches what every developer
already knows:

| Prefix | Min width | Represents             | What changes                           |
| ------ | --------- | ---------------------- | -------------------------------------- |
| _none_ | 0         | small phone (375px)    | **the base layout** — write this first |
| `sm`   | 640px     | large phone, landscape | denser padding, inline label text      |
| `md`   | 768px     | tablet                 | navigation moves into the header       |
| `lg`   | 1024px    | laptop                 | multi-column content                   |
| `xl`   | 1280px    | desktop                | wider container only                   |

**Mobile-first is not a preference, it is the mechanism.** Unprefixed classes
are the phone layout; `sm:`/`md:`/`lg:` only ever _add_. Writing desktop
styles first and undoing them with `max-` queries produces layouts that break
on the devices you did not test.

Test at **375, 768 and 1440**. 375 is the one that finds bugs.

### Non-negotiable rules

| Rule                                | Why                                                                                                                       |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| **No horizontal page scroll, ever** | The one layout bug a user cannot work around. A deliberately scrollable _region_ (a data table) is fine; the page is not. |
| **`min-h-dvh`, never `h-screen`**   | `100vh` on mobile is taller than the visible area because of browser chrome, so the bottom of every screen is cut off.    |
| **Body text ≥16px on mobile**       | Below 16px iOS auto-zooms on focus, which reflows the page under the user's finger.                                       |
| **Never disable zoom**              | `user-scalable=no` and `maximum-scale=1` are accessibility failures. The viewport meta stays as it is.                    |
| **Nothing depends on hover**        | There is no hover on a touch screen. Hover may only _enhance_ what tap already reveals.                                   |
| **Fixed bars reserve space**        | Content under a fixed header or bottom bar is unreachable. Reserve padding on the scrolling container.                    |
| **Respect safe areas**              | `env(safe-area-inset-*)` for anything pinned to an edge, or the iOS home indicator eats the last row of controls.         |

### Touch targets

`--touch-target-min: 44px`, `--touch-target-spacing: 8px`.

Enforced in `tokens.css` under `@media (pointer: coarse)` — by **input
modality, not viewport width**. This matters: a tablet in landscape is wide
_and_ touch-operated, and a touchscreen laptop is both at once. Sizing by
breakpoint would miss both. Desktop pointer layouts stay compact; anything
driven by a finger gets the full 44px automatically, with no per-component
work.

Scoped to controls (`button`, `[role=button]`, form fields). WCAG exempts
inline links inside prose, and forcing 44px on them would wreck paragraph
rhythm.

`touch-action: manipulation` on interactive elements removes the 300ms tap
delay **without** disabling pinch-to-zoom.

### Layout patterns

**Navigation** — bottom bar on phones, inline in the header from `md` up.
Bottom is within thumb reach and is where users look on a phone. Maximum 5
items, each with an icon _and_ a label: labels alone are hard to scan, icons
alone are hard to understand.

**Header** — holds only what must always be reachable (identity, status,
account). Everything else moves out. Packing brand, nav and every control
into one row is what causes horizontal scroll at 375px. Long values truncate;
a long product name must never be what breaks the layout.

**Dialog** — a full-width bottom sheet on phones, a centred box from `sm` up.
A centred box on a phone wastes the edges and puts actions away from the
thumb. `max-h-[85dvh]` with internal scrolling keeps the close affordance
reachable when content is long.

**Data tables** — keep every column and scroll _within_ the region. This
requires a `min-w-*` on the table: without it `w-full` compresses columns into
unreadable slivers instead of overflowing. The region gets `tabindex="0"`,
`role="region"` and a label, because overflow containers are otherwise not
keyboard-scrollable. Dropping columns on small screens hides data the user
came for — prefer scrolling.

**Forms** — single column on mobile, always. Side-by-side fields at 375px
leave neither readable. Use semantic `type`s (`email`, `tel`, `number`) so the
correct keyboard appears.

### Verification

`apps/web/e2e/responsive.spec.ts` asserts these at real device sizes, because
responsive regressions are invisible to every other test: assertions on roles
and text pass perfectly while the page scrolls sideways and half the controls
are too small to tap. It measures horizontal overflow, bottom-bar placement,
whether the last control is actually covered (`elementFromPoint`, not bounding
boxes — a fixed bar lives in viewport coordinates while boxes are in document
ones), touch-target heights under `pointer: coarse`, and `tap()` without hover.

---

## 13. Verification

```bash
pnpm --filter @enterprise/ui storybook        # http://localhost:6006
pnpm --filter @enterprise/ui build-storybook  # static build (runs in CI)
pnpm --filter @enterprise/ui test             # behaviour + a11y
```

The Storybook theme switcher is the verification surface: **every story must
look correct in both themes.** A component that breaks after the switch has
hardcoded a value instead of using a token.

Responsive behaviour is verified by `apps/web/e2e/responsive.spec.ts` (see
§12), which runs at 375px, on an emulated touch device, and at tablet and
desktop widths.

---

## Anti-patterns

| Don't                                 | Do                                           |
| ------------------------------------- | -------------------------------------------- |
| `bg-[#3b82f6]`, `p-[13px]`            | `bg-primary`, spacing scale                  |
| `bg-blue-600` (a primitive)           | `bg-primary` (a semantic token)              |
| `rounded-md` in a component           | `rounded-(--button-radius)`                  |
| `duration-300`                        | inherit the default, or use a duration token |
| `outline-none` on a focusable element | change `--focus-ring-*`                      |
| A colour-only status indicator        | colour **plus** text or `srLabel`            |
| Patching a component inside a project | fix the tokenisation gap in the framework    |
| A new colour "just for this screen"   | a semantic token, or an existing one         |
| `h-screen` / `100vh`                  | `min-h-dvh`                                  |
| Desktop first, undone with `max-`     | mobile base, extended with `sm:` `md:` `lg:` |
| An action that appears only on hover  | visible on tap; hover may only enhance       |
| A fixed bar with no reserved padding  | padding on the scrolling container           |
| `w-full` table in `overflow-x-auto`   | add `min-w-*` so it scrolls, not squashes    |
| Shipping a page tested only at 1440px | check 375px — that is where bugs are         |
