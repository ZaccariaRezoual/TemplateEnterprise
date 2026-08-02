# Testare il frontend

**Fonte normativa**: [`CLAUDE.md`](../../CLAUDE.md) → «Nessuna feature è
completa senza test» · [`docs/frontend.md`](../../docs/frontend.md)
**Esempio nel codice**: `modules/dashboard/frontend/src/components/DashboardGrid.spec.ts` ·
`apps/web/e2e/`

## Quando serve

Sempre. La domanda è quale dei due livelli.

| Scrivi un…           | Quando                                                           |
| -------------------- | ---------------------------------------------------------------- |
| **unit / component** | Logica, stato, rendering condizionale, accessibilità di un pezzo |
| **e2e (Playwright)** | Un flusso attraversa più schermate, o coinvolge davvero l'API    |

Regola pratica: se il test ti costringe a simulare mezzo backend, probabilmente
è un e2e. Se un e2e verifica un `if`, probabilmente è un unit test.

## Unit e component — Vitest + Vue Test Utils

Ambiente jsdom. Si testa il **comportamento visibile**, non l'implementazione:

```ts
async function renderGrid(widgets: unknown[]) {
  provideDashboardApi(mockApi(widgets));

  const wrapper = mount(DashboardGrid, {
    global: { plugins: [VueQueryPlugin], stubs: { RouterLink: { template: "<a><slot/></a>" } } },
  });
  await flushPromises();
  return wrapper;
}

it("says the dashboard is empty rather than showing nothing", async () => {
  const wrapper = await renderGrid([]);

  expect(wrapper.text()).toContain("No widgets yet");
});
```

Due abitudini che pagano:

- **Mocka al confine dell'SDK**, non i composable: così il test copre anche il
  feature service, che è dove si sbaglia il path.
- **Interroga come un utente**: testo e ruoli, non classi CSS. Un test che
  cerca `.btn-primary` fallisce a ogni ritocco estetico e passa mentre il
  pulsante è inutilizzabile.

Il nome del test è una frase sul comportamento, e vale la pena scrivere nel
commento **perché** quel comportamento è quello giusto: «una tabella vuota
significherebbe _non ci sono utenti_, che è una bugia».

## e2e — Playwright

Stack reale: richiedono l'**API in esecuzione** in Development (Playwright
avvia da solo il dev server Vite).

```bash
dotnet run --project apps/api/src/Api        # in un terminale
pnpm --filter @enterprise/web test:e2e       # nell'altro
```

Cosa vale la pena coprire, e poco altro:

- i flussi principali (login, registrazione, la pagina di lavoro);
- ciò che attraversa i confini: permessi, realtime, redirect delle guard;
- il **comportamento responsive** — `e2e/responsive.spec.ts`.

`responsive.spec.ts` merita una nota: esiste perché una regressione responsive
è **invisibile a ogni altro test**. Le asserzioni su ruoli e testi passano
benissimo mentre la pagina scrolla di lato. Solo una misura la trova.

Per i dati usa `crypto.randomUUID()`, mai un timestamp: i worker paralleli
collidono sul millisecondo.

## Come si esegue

```bash
pnpm test                                    # tutti i package
pnpm --filter @enterprise/ui test            # solo il design system
pnpm --filter @enterprise/web test:e2e       # e2e (API attiva)
pnpm -r typecheck
```

## Errori tipici

**Il test e2e fallisce solo quando si esegue tutta la suite.** Quasi sempre è
il rate limiter: i worker condividono l'IP. I limiti sono già rilassati in
`appsettings.Development.json` — se ti serve alzarli ancora, il posto è quello,
non il codice.

**Un e2e che aspetta l'assenza di un elemento è instabile.** Un elemento è
assente anche _prima_ di comparire: aspetta uno stato esplicito
(`data-status="connected"`), non un'assenza.

**`toBeInViewport` fallisce con «viewport ratio 0».** Hai scrollato prima che
il contenuto asincrono fosse renderizzato, quindi la pagina era più corta:
aspetta la visibilità dell'elemento, poi scrolla.

**Il test misura bounding box contro una barra `fixed`.** Coordinate diverse
(documento vs viewport): usa `document.elementFromPoint`.

**Un component test fallisce su `RouterLink`.** Va stubbato — non c'è un router
nel test.

**Hai testato che un composable chiami un altro composable.** Stai testando
l'implementazione: al primo refactor legittimo il test diventa rosso senza che
nulla si sia rotto.

## Correlati

- [Aggiungere una feature frontend](add-feature.md)
- [Checklist responsive](responsive-checklist.md)
- [Testare il backend](test-backend.md)
