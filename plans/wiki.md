# Piano — Wiki di sviluppo (`wiki/`)

> Piano operativo per costruire la wiki interna del framework, partendo dai
> pattern di sviluppo in `wiki/patterns/`. Stessa logica di
> [PLAN.md](../PLAN.md): fasi ordinate per dipendenza, ognuna con un criterio
> di "done" verificabile.

---

## 1. Il problema da risolvere

Oggi la conoscenza del progetto è distribuita su cinque posti, ognuno con uno
scopo diverso:

| Dove               | Cosa contiene                              | A chi parla                     |
| ------------------ | ------------------------------------------ | ------------------------------- |
| `Struttura.md`     | Visione e architettura complessiva         | Chi decide                      |
| `PLAN.md`          | Piano di costruzione a fasi                | Chi costruisce il framework     |
| `docs/`            | Regole normative: architettura, moduli, DS | Chi deve sapere **cosa è vero** |
| `CLAUDE.md`        | Contratto operativo per le sessioni AI     | Claude Code                     |
| `modules/*/README` | Decisioni e contratti del singolo modulo   | Chi tocca quel modulo           |

Manca il livello che serve davvero a uno sviluppatore nuovo: **"devo fare X,
come si fa qui dentro?"**. Oggi la risposta si ricostruisce leggendo tre
documenti e due moduli di riferimento. Questo è il vuoto che riempie la wiki.

## 2. Il confine con `docs/` — la regola che regge tutto

> **La wiki non ridefinisce nulla: linka.**

- `docs/` è **normativo**: dice cosa è vero e cosa è vietato. In caso di
  conflitto vince `docs/`, sempre.
- `wiki/` è **operativo**: mostra la procedura, nell'ordine in cui la si
  esegue, con i file reali del repository.

Ogni pagina della wiki apre con un rimando alla fonte normativa che le
corrisponde. Se una pagina si trova a **spiegare una regola** invece che ad
applicarla, quella regola appartiene a `docs/` e la pagina deve linkarla.

Il motivo è lo stesso già scritto in CLAUDE.md per il design system: due copie
divergono sempre. Una wiki che riscrive le regole diventa, nel giro di pochi
mesi, la versione sbagliata che tutti leggono.

**Test di verifica del confine**: cancellando una pagina della wiki non si deve
perdere nessuna informazione normativa — solo tempo.

## 3. Struttura

```
wiki/
  README.md              Home: mappa e "come si legge questa wiki"
  getting-started/
    day-one.md           Da zero ad app in esecuzione, con credenziali dev
    tour.md              Giro del repository in 15 minuti
    glossary.md          Modulo, seam, proiezione, token semantic, ...
  patterns/              ⟵ IL CUORE, punto di partenza
    README.md            Indice dei pattern, per attività
    <pattern>.md
  decisions/             ADR: perché una scelta è stata fatta (fase 4)
```

Niente cartella `how-to/` accanto a `patterns/`: due tassonomie per lo stesso
tipo di contenuto costringono a decidere ogni volta dove scrivere, e la
risposta sbagliata rende la pagina introvabile.

## 4. Formato di una pattern page

Ogni pagina in `patterns/` ha **sempre** questa struttura. La rigidità è
voluta: una wiki in cui ogni pagina è organizzata diversamente si legge come
una raccolta di appunti.

```markdown
# <Attività, all'infinito> — es. "Aggiungere un endpoint"

**Fonte normativa**: docs/backend.md §…, CLAUDE.md §…
**Esempio nel codice**: modules/demo/backend/Features/…

## Quando serve

Una frase. E quando NON serve (l'alternativa da preferire).

## Procedura

Passi numerati. Ogni passo cita il file reale e, dove serve, uno snippet
minimo preso dal repository — non inventato.

## Come si verifica

Il comando che dimostra che è fatto bene (`dotnet test`, `pnpm -r typecheck`,
`node scripts/generate-sdk.mjs --check`, …).

## Errori tipici

Cosa va storto davvero, e il sintomo con cui si manifesta.

## Correlati

Link ad altre pattern page.
```

Due regole non negoziabili sul contenuto:

1. **Ogni snippet viene dal repository**, non dalla fantasia. Uno snippet che
   non compila insegna a scrivere codice che non compila.
2. **"Errori tipici" non è opzionale.** È la sezione che fa risparmiare
   davvero tempo, ed è l'unica che non si può dedurre leggendo il codice.

## 5. Catalogo iniziale dei pattern

Derivato dalle attività reali di questo repository, non da un elenco astratto.

### Backend

| Pagina                           | Nasce da                                                                                                    |
| -------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| `add-endpoint.md`                | Command/query MediatR + validator + `RequirePermission` + tipo di ritorno concreto (`Ok<T>`, mai `IResult`) |
| `create-module.md`               | `IModule`, `module.json`, DbContext, schema, migrations                                                     |
| `module-to-module.md`            | Evento pubblico vs extension point vs proiezione — quale scegliere                                          |
| `add-permission.md`              | Catalogo `Permissions` → endpoint → frontend                                                                |
| `module-persistence.md`          | Schema dedicato, migrations, `Modules:AutoMigrate`                                                          |
| `errors.md`                      | Gerarchia condivisa → ProblemDetails                                                                        |
| `make-it-realtime.md`            | `IRealtimeEvent`: audience e channel                                                                        |
| `contribute-dashboard-widget.md` | `IDashboardWidgetProvider`                                                                                  |
| `test-backend.md`                | Unit vs integration con Testcontainers                                                                      |

### Frontend

| Pagina                    | Nasce da                                                        |
| ------------------------- | --------------------------------------------------------------- |
| `add-feature.md`          | componente → composable → feature service → SDK                 |
| `state.md`                | TanStack Query vs Pinia: la domanda da porsi                    |
| `regenerate-sdk.md`       | `node scripts/generate-sdk.mjs`, e perché la CI fallisce        |
| `add-ui-component.md`     | I 5 file, i token, la storia Storybook, l'a11y che deve fallire |
| `use-design-tokens.md`    | Scegliere il livello del token; il valore letterale è un bug    |
| `responsive-checklist.md` | Mobile-first, la verifica a 375px                               |
| `client-permissions.md`   | `usePermissions()` / `v-can` — presentazione, non sicurezza     |
| `i18n.md`                 | Aggiungere una stringa e un locale                              |
| `test-frontend.md`        | Vitest, Playwright, e2e che richiedono l'API                    |

### Trasversali

| Pagina               | Nasce da                                                                                                                                                                    |
| -------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `git-workflow.md`    | Branch, Conventional Commits, cosa deve passare in CI                                                                                                                       |
| `code-review.md`     | Cosa si guarda in una PR di questo repository                                                                                                                               |
| `new-project.md`     | `scripts/create-project.mjs` (rimanda a docs/create-project.md)                                                                                                             |
| `troubleshooting.md` | I fallimenti già incontrati: Tailwind che non vede i package dei moduli, `/hubs` senza `ws: true`, rate limiter durante gli e2e, lock dei file .dll con l'API in esecuzione |

`troubleshooting.md` merita attenzione: sono problemi **già pagati** durante la
costruzione del framework, con sintomi che non suggeriscono la causa. È la
pagina con il rapporto valore/righe più alto dell'intera wiki.

## 6. Fasi

### Fase 0 — Fondamenta

- `wiki/README.md` con la mappa e la regola del confine con `docs/`
- `wiki/patterns/README.md` (indice per attività)
- Il template di pagina come file reale: `wiki/patterns/_template.md`
- Regola aggiunta a `CLAUDE.md`: dove vive la wiki, e che una pattern page si
  aggiorna **nello stesso commit** del pattern che descrive

✅ **Done quando**: esiste lo scheletro, il template è usabile per copia, e
CLAUDE.md indica il confine `docs/` ↔ `wiki/`.

### Fase 1 — Pattern backend

Le 4 pagine che sbloccano il lavoro quotidiano, nell'ordine:
`add-endpoint`, `create-module`, `module-to-module`, `add-permission`.
Poi le restanti della tabella.

✅ **Done quando**: uno sviluppatore aggiunge un endpoint protetto seguendo
solo la pagina, e `dotnet build` + `dotnet test` passano.

### Fase 2 — Pattern frontend

Prima `add-feature`, `state`, `regenerate-sdk` (la catena che si usa ogni
giorno), poi le pagine di design system e responsive, che rimandano a
`docs/design-system.md` senza riscriverlo.

✅ **Done quando**: si aggiunge una pagina che consuma un endpoint nuovo
seguendo solo la wiki, e `pnpm -r typecheck` + `pnpm test` passano.

### Fase 3 — Onboarding e trasversali

`getting-started/` completo + `git-workflow`, `code-review`,
`troubleshooting`, `new-project`.

✅ **Done quando**: una persona che non ha mai visto il repository arriva
all'app in esecuzione e alla prima PR usando solo `wiki/getting-started/`.
Da verificare **su una persona vera**, non per ispezione: è l'unico modo di
scoprire i passaggi che sembrano ovvi solo a chi li ha già fatti.

### Fase 4 — Manutenzione e pubblicazione

- Link checker in CI (i link rotti sono il primo sintomo di una wiki morta)
- Voce nella PR checklist: "pattern toccato → pagina aggiornata?"
- `decisions/`: portare qui le decisioni architetturali già motivate nei
  README dei moduli, in formato ADR
- Pubblicazione (vedi §8)

✅ **Done quando**: la CI fallisce su un link rotto e la checklist di PR
include la wiki.

---

## Stato di avanzamento

| Fase | Stato | Cosa esiste                                                                   |
| ---- | ----- | ----------------------------------------------------------------------------- |
| 0    | ✅    | `wiki/README.md`, `patterns/README.md`, `_template.md`, regola in `CLAUDE.md` |
| 1    | ✅    | 9 pattern backend                                                             |
| 2    | ✅    | 9 pattern frontend                                                            |
| 3    | ✅    | `getting-started/` (day-one, tour, glossary) + 4 pattern trasversali          |
| 4    | ✅    | `scripts/check-links.mjs` in CI, PR template, `decisions/`                    |

**Non ancora verificato**: il criterio di done della Fase 3 richiede una
persona che non ha mai visto il repository. È l'unico modo di scoprire i
passaggi che sembrano ovvi solo a chi li ha già fatti — nessuna rilettura lo
sostituisce.

**Le ADR sono un contenitore, non un contenuto.** Le decisioni di questo
framework sono già motivate nei README dei moduli, che è dove servono; portarle
in `decisions/` significherebbe duplicarle. La cartella esiste per le decisioni
trasversali future.

## 7. Manutenzione: perché questa wiki non morirà

Le wiki muoiono perché aggiornarle è un lavoro separato dal codice. Contromisure:

1. **Vive nel repository**, non su una piattaforma esterna: si modifica nella
   stessa PR del codice e si rivede con lo stesso diff.
2. **Non duplica le regole**: una regola che cambia si aggiorna in `docs/`, e le
   pagine della wiki che la linkano restano valide.
3. **Gli snippet vengono dal codice**, quindi un pattern che cambia rende la
   pagina visibilmente sbagliata invece che silenziosamente obsoleta.
4. **La CI controlla i link**, che è il degrado più frequente e più economico
   da intercettare.
5. **Viaggia con i progetti**: `scripts/create-project.mjs` copia i file
   tracciati da git, quindi ogni nuovo progetto nasce con la sua wiki già
   dentro — e la manutiene per sé.

## 8. Pubblicazione — decisione da prendere

Il contenuto resta comunque markdown nel repository. Le opzioni riguardano solo
come lo si legge:

| Opzione                   | Pro                                             | Contro                                      |
| ------------------------- | ----------------------------------------------- | ------------------------------------------- |
| **Solo repo** (default)   | Zero infrastruttura; leggibile su GitHub        | Niente ricerca full-text decente            |
| GitHub Wiki sincronizzata | UI familiare                                    | Repo separato: il diff si stacca dal codice |
| Sito statico (VitePress)  | Ricerca, navigazione, versionamento per release | Un altro build da mantenere in CI           |

**Deciso: solo repo.** Con 22 pagine, la ricerca di GitHub e un grep sono
sufficienti, e il markdown resta a un diff di distanza dal codice che descrive.

VitePress si valuta quando comparirà un sintomo concreto — «non trovo la
pagina», non «sarebbe bello avere un sito». Costruire il sito prima significa
aggiungere un build alla CI per un problema che nessuno ha ancora.

## 9. Lingua — deciso: italiano

**La wiki è in italiano.** Gli sviluppatori sono oggi tutti italiani, e la wiki
esiste per farli lavorare, non per essere pubblicata: la lingua che si legge
più in fretta vince su qualsiasi considerazione di coerenza.

Restano in **inglese**, e non cambiano:

- il codice, i commenti e i commit (regola di `CLAUDE.md`);
- `docs/` e i README dei moduli, che sono documentazione adiacente al codice e
  viaggiano nei progetti generati dal template;
- gli identificatori citati nelle pagine: si scrive «il validator di
  `UpdateUserCommand`», mai «il validatore del comando aggiorna-utente».
  Tradurre un identificatore lo rende non cercabile, che è l'unico difetto
  della documentazione che costa più della lingua sbagliata.

Se un giorno il team diventasse misto, la conversione è una traduzione a
contenuto invariato — il costo sta tutto nello scrivere le pagine, che è
lavoro già fatto in quel momento.

## 10. Ordine di esecuzione

```
Fase 0  Scheletro + template + regola di confine   (tutto il resto vi si appoggia)
Fase 1  Pattern backend                            (dove si aggiunge più codice)
Fase 2  Pattern frontend                           (dipende dall'SDK, quindi dal backend)
Fase 3  Onboarding + trasversali                   (si scrive meglio quando i pattern esistono già)
Fase 4  Manutenzione + pubblicazione               (ha senso solo con contenuto reale)
```

Il principio guida è lo stesso di PLAN.md: **prima ciò che si usa ogni
giorno**. Una wiki che parte dal glossario e arriva ai pattern dopo sei
settimane non viene letta da nessuno nel frattempo.
