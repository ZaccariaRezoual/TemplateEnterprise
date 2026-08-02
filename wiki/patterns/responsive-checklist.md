# Checklist responsive

**Fonte normativa**: [`docs/design-system.md`](../../docs/design-system.md)
§12 · [`CLAUDE.md`](../../CLAUDE.md) punto 6 delle regole di lavoro
**Esempio nel codice**: `apps/web/src/layouts/DefaultLayout.vue` ·
`apps/web/e2e/responsive.spec.ts`

## Quando serve

Su **ogni** pagina e ogni elemento, senza eccezioni. L'applicativo supporta il
mobile al 100%, non «in modo accettabile».

Non esiste un «quando NON serve».

## Mobile-first è il meccanismo, non una preferenza

Scrivi prima il layout del telefono — **classi senza prefisso** — e lo
_estendi_ con `sm:`/`md:`/`lg:`. Mai il contrario: partire dal desktop e
disfarlo con query `max-` produce layout che si rompono sui dispositivi che non
hai provato.

| Prefisso  | Da     | Rappresenta                                   |
| --------- | ------ | --------------------------------------------- |
| _nessuno_ | 0      | telefono piccolo (375px) — **il layout base** |
| `sm`      | 640px  | telefono grande, orizzontale                  |
| `md`      | 768px  | tablet — la nav entra nell'header             |
| `lg`      | 1024px | laptop — più colonne                          |
| `xl`      | 1280px | desktop — solo container più largo            |

## La checklist

Prima di considerare finita una pagina:

- [ ] **Nessuno scroll orizzontale di pagina.** È l'unico bug di layout a cui
      l'utente non può rimediare. Una _regione_ scrollabile di proposito (una
      tabella dati) va bene; la pagina no.
- [ ] **`min-h-dvh`, mai `h-screen`.** `100vh` su mobile è più alto dell'area
      visibile per via delle barre del browser: il fondo di ogni schermata
      resta tagliato.
- [ ] **Testo ≥16px su mobile.** Sotto i 16px iOS fa auto-zoom sul focus e
      rifluisce la pagina sotto il dito dell'utente.
- [ ] **Zoom mai disabilitato.** `user-scalable=no` e `maximum-scale=1` sono
      fallimenti di accessibilità: il meta viewport non si tocca.
- [ ] **Niente che dipenda dall'hover.** Su touch non esiste. L'hover può solo
      _arricchire_ ciò che il tap già rivela.
- [ ] **Le barre fisse riservano spazio.** Il contenuto sotto un header o una
      bottom bar fissa è irraggiungibile: padding sul contenitore che scrolla.
- [ ] **Safe area rispettate.** `env(safe-area-inset-*)` per tutto ciò che è
      ancorato a un bordo, altrimenti l'home indicator di iOS si mangia
      l'ultima fila di controlli.
- [ ] **Verificata a 375px.** Poi 768 e 1440. **375 è quella che trova i bug.**

## Target touch: già fatto, non rifarlo

`--touch-target-min: 44px` è imposto in `tokens.css` sotto
`@media (pointer: coarse)` — per **modalità di input, non per larghezza dello
schermo**. È la differenza che conta: un tablet in orizzontale è largo _e_
touch, un laptop con touchscreen è entrambe le cose insieme. Dimensionare per
breakpoint sbaglierebbe in tutti e due i casi.

Quindi non aggiungere `min-h-11` a mano: lo stai già ricevendo dove serve.

## Come si verifica

Le regole meccaniche hanno un test:

```bash
pnpm --filter @enterprise/web test:e2e   # e2e/responsive.spec.ts (API attiva)
```

`responsive.spec.ts` esiste perché **una regressione responsive è invisibile a
ogni altro test**: le asserzioni su ruoli e testi passano benissimo mentre la
pagina scrolla di lato e metà dei controlli sono troppo piccoli da toccare.
Solo una misura la trova.

A mano, in DevTools a 375×667: scorri fino in fondo e prova a toccare l'ultimo
controllo.

## Errori tipici

**Scroll orizzontale che non trovi.** Di solito è un elemento con larghezza
fissa, una tabella senza contenitore `overflow-x-auto`, o una stringa lunga
senza `truncate`. Misura invece di guardare:

```js
document.documentElement.scrollWidth > document.documentElement.clientWidth;
```

**L'ultimo pulsante è coperto dalla bottom bar.** Manca il padding di riserva
sul contenitore che scrolla (`pb-20 md:pb-10` nel layout di default).

**Confronti bounding box e il test è sbagliato.** Le bounding box sono in
coordinate documento, una barra `fixed` vive in coordinate viewport. Chiedi al
browser cosa è realmente dipinto: `document.elementFromPoint(...)`.

**Un menu che si apre in hover.** Su touch non si apre mai. Serve un tap.

**`h-screen` copiato da un esempio online.** Su desktop non si nota, su mobile
taglia il fondo di ogni pagina.

## Correlati

- [Scegliere il token giusto](use-design-tokens.md)
- [Aggiungere un componente al design system](add-ui-component.md)
- [Testare il frontend](test-frontend.md)
