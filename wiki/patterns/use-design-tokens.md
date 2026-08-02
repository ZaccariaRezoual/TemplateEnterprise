# Scegliere il token giusto

**Fonte normativa**: [`docs/design-system.md`](../../docs/design-system.md) §1
— catalogo completo e regola di livello. **In caso di dubbio visivo vince quel
documento**, non questa pagina.
**Esempio nel codice**: `packages/ui/src/styles/{primitives,semantic,components}.css`

## Quando serve

Prima di scrivere qualsiasi markup o stile, ovunque: `packages/ui`,
`apps/web`, il frontend di un modulo.

**Quando NON serve**: mai. Non c'è un caso in cui un valore letterale sia
accettabile. `#3b82f6`, `16px`, `rounded-md`, `duration-300` nel markup sono
**bug**, non scorciatoie: rompono la proprietà per cui un progetto si
ri-brandizza toccando solo i token.

## I tre livelli

```
Primitive   valori grezzi, nessun significato   --color-brand-600
     ↓
Semantic    significato, nessun componente      --color-primary, --radius-control
     ↓
Component   l'eccezione di un solo componente   --card-radius
```

Chi può referenziare cosa:

| Livello       | Lo referenzia          |
| ------------- | ---------------------- |
| **Primitive** | solo `semantic.css`    |
| **Semantic**  | componenti e app       |
| **Component** | solo il suo componente |

Nel 95% dei casi quello che ti serve è **già nel livello semantic**:
`bg-surface`, `text-text-muted`, `border-border`, `rounded-(--radius-control)`.

## Procedura: mi serve un valore nuovo

1. **Cerca prima.** Apri
   [`docs/design-system.md`](../../docs/design-system.md) §2–6 e verifica che
   non esista già. Un token duplicato con un nome diverso è peggio di un valore
   letterale, perché sembra corretto.

2. **Decidi il livello**, in quest'ordine:

   - **Cambierebbero tutti i componenti insieme?** → semantic
     («i nostri angoli sono più spigolosi» → `--radius-control`)
   - **Cambierebbe solo questo componente?** → component
     («le card sono piatte ma i dialog no» → `--card-shadow`)
   - **È un valore grezzo senza significato?** → primitive, e **subito dopo**
     dagli un nome semantic. Un primitive che nessuno referenzia è peso morto.

   Un token component che nessun progetto cambierebbe da solo **non è
   giustificato**: è indirezione senza beneficio. Per questo i bottoni hanno un
   token per il radius ma non per la dimensione del testo.

3. **Aggiungilo nel file del livello giusto** e, se è semantic, in **entrambi i
   temi** (`:root` e `[data-theme="dark"]`).

4. **Documentalo in `docs/design-system.md` nello stesso commit.** Non nel
   successivo: un token non documentato è un token che il prossimo progetto
   reinventerà.

## `@theme inline`: non toccarlo

I token semantic sono dichiarati dentro `@theme inline`, che mappa le utility
Tailwind sulle variabili **per riferimento**. È quello che fa sì che cambiare
tema ridipinga tutta la UI a runtime, senza rebuild. Con un `@theme` normale il
valore verrebbe inlineato a build time e il theming si romperebbe.

## Se per ottenere un risultato devi modificare un componente

Quel componente ha un **buco di tokenizzazione**. Si sistema nel framework
(aggiungendo il token che manca), **mai** nel progetto. È la regola che tiene
in piedi tutto il resto: il giorno in cui un progetto forka un componente, il
design system smette di essere una fonte unica.

## Come si verifica

```bash
pnpm --filter @enterprise/ui storybook
```

Guarda il componente in **entrambi i temi**: è il test visivo che la proprietà
«cambio token = cambio ovunque» regge ancora.

Una verifica testuale rapida:

```bash
rg '#[0-9a-fA-F]{6}|rounded-md|duration-[0-9]' apps/web/src modules/*/frontend/src
```

Qualunque risultato fuori dai file dei token è da sistemare.

## Errori tipici

**Hai usato un token primitive nel markup.** Salta il livello che dà
significato: al prossimo rebrand quel valore non seguirà il tema.

**Hai aggiunto il token solo in `:root`.** In dark mode resta il valore chiaro,
e spesso il contrasto scende sotto la soglia di accessibilità senza che nessuno
se ne accorga.

**Hai rimosso o cambiato il significato di un token semantic.** È **breaking**
per tutti i progetti sul framework: serve una deprecazione documentata, mai una
sostituzione silenziosa.

**Hai tolto il focus visibile perché «stonava».** Non si rimuove mai: si cambia
`--focus-ring-*`.

**Le classi Tailwind di un package non hanno effetto.** Non è un problema di
token: Tailwind non scansiona quel package. Aggiungi il percorso alle direttive
`@source` in `apps/web/src/assets/styles/main.css`.

## Correlati

- [Aggiungere un componente al design system](add-ui-component.md)
- [Checklist responsive](responsive-checklist.md)
