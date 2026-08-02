# Aggiungere un componente al design system

**Fonte normativa**: [`docs/design-system.md`](../../docs/design-system.md)
§7–9 — catalogo, contratto di accessibilità, convenzioni
**Esempio nel codice**: `packages/ui/src/components/Skeleton/` (il più
recente, e il più semplice da imitare)

## Quando serve

Quando un elemento visivo serve — o servirà — a più di una feature.

**Quando NON serve**: se lo usa una sola pagina, resta nella feature. Un design
system che accoglie tutto diventa un contenitore di casi particolari, e il
costo lo paga chi cerca il componente giusto fra trenta quasi-uguali.

**Quando serve ma non sembra**: se stai per _modificare_ un componente
esistente per ottenere un aspetto diverso, quasi sempre non ti serve un
componente nuovo — ti serve un **token** che manca. Vedi
[Scegliere il token giusto](use-design-tokens.md).

## Procedura

1. **Crea i cinque file.** Sempre tutti e cinque:

   ```
   packages/ui/src/components/Skeleton/
     Skeleton.vue         implementazione
     Skeleton.types.ts    contratto pubblico (props, emits, slot), documentato
     Skeleton.test.ts     comportamento e accessibilità
     Skeleton.stories.ts  Storybook
     index.ts             export pubblici
   ```

2. **Scrivi prima il contratto** — `.types.ts`. Ogni prop documentata, e le
   opzionali accettano `undefined` esplicitamente:

   ```ts
   export interface SkeletonProps {
     /** Shape of the placeholder… Defaults to `text`. */
     shape?: SkeletonShape | undefined;
     /** CSS width, e.g. `"8rem"` or `"60%"`. */
     width?: string | undefined;
   }
   ```

   Il `?` da solo non basta: con `exactOptionalPropertyTypes`, chi passa un
   computed che può valere `undefined` non compilerebbe.

3. **Implementa**, con un commento di apertura che dica **quando usarlo
   rispetto alle alternative simili** (Dialog vs Modal, Skeleton vs spinner).
   È la domanda che si pone chi arriva, e la risposta non sta nel codice.

   Nessun valore letterale: solo token. Un valore usato due volte diventa un
   token.

4. **Fai vincere la `class` del consumatore**: i componenti disattivano
   l'ereditarietà degli attributi e fondono con `cn()`. Senza, `bg-primary` del
   componente e `bg-surface` del consumatore convivono e vince l'ordine del CSS
   — cioè il caso.

5. **Scrivi la storia Storybook**, includendone una che mostri il componente in
   un contesto realistico, non solo isolato. L'addon a11y è configurato per
   **fallire**, non per avvisare.

6. **Esporta** da `packages/ui/src/index.ts`.

7. **Documenta il componente in `docs/design-system.md` §7, nello stesso
   commit.** Un componente non documentato verrà reinventato.

## Accessibilità: le cose che non si negoziano

- Il **focus visibile non si rimuove mai**: se stona, si cambia
  `--focus-ring-*`.
- **Il colore da solo non è informazione**: sempre accompagnato da testo,
  icona o `srLabel`. Un badge rosso con «3» non dice nulla a uno screen reader.
- **Rimuovere, non nascondere**: un elemento che non deve essere disponibile
  esce dal DOM. Nascosto visivamente resta focusabile e annunciato.
- I target touch da 44px sono già imposti dai token via `pointer: coarse`: non
  reimplementarli.

## Come si verifica

```bash
pnpm --filter @enterprise/ui test
pnpm --filter @enterprise/ui storybook   # guardalo in ENTRAMBI i temi
pnpm -r typecheck
```

Prova anche con la tastiera: `Tab` fino al componente, e usalo senza mouse.

## Errori tipici

**Il componente non si vede in `apps/web`.** Manca l'export da `index.ts`,
oppure Tailwind non scansiona il package (`@source` in
`apps/web/src/assets/styles/main.css`) — in quel caso nessun errore, solo un
elemento senza stili, spesso di dimensione zero.

**In dark mode il contrasto crolla.** Hai usato un token primitive, o ne hai
aggiunto uno semantic solo in `:root`.

**Il test a11y fallisce e lo hai disattivato.** L'addon è configurato per
fallire proprio per impedirlo: il problema è il componente.

**Hai aggiunto un token component «per sicurezza».** Se nessun progetto lo
cambierebbe da solo, è indirezione senza beneficio: usa il semantic.

**La prop opzionale non compila da chi la usa.** Manca `| undefined` nel tipo.

## Correlati

- [Scegliere il token giusto](use-design-tokens.md)
- [Checklist responsive](responsive-checklist.md)
- [Testare il frontend](test-frontend.md)
