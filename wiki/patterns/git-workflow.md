# Aprire una PR come si deve

**Fonte normativa**: [`CLAUDE.md`](../../CLAUDE.md) → «Convenzioni» e «CI su
ogni PR»
**Esempio nel codice**: `.github/workflows/ci.yml` · `commitlint.config.js` ·
`.husky/`

## Branch

| Branch      | Cos'è                               |
| ----------- | ----------------------------------- |
| `main`      | produzione — **mai commit diretti** |
| `develop`   | sviluppo                            |
| `feature/*` | una funzionalità                    |
| `fix/*`     | una correzione                      |
| `release/*` | preparazione di un rilascio         |
| `hotfix/*`  | correzione urgente in produzione    |

```bash
git checkout develop && git pull
git checkout -b feature/<cosa>
```

## Commit: Conventional Commits, imposti da Commitlint

```
<tipo>(<ambito opzionale>): <descrizione all'imperativo>
```

Tipi usati qui: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`,
`build`, `ci`.

```
feat: seed a bootstrap administrator and rename the built-in roles
fix: keep the generated OpenAPI document out of prettier
docs: add the frontend pattern pages to the wiki
```

Il messaggio è in **inglese**, come il codice. Il corpo spiega **perché**, non
cosa: il «cosa» è nel diff. I commit di questo repository sono la fonte da cui
si ricostruisce una decisione mesi dopo — scrivili per quel lettore.

L'hook `commit-msg` rifiuta un messaggio fuori formato; `pre-commit` esegue
eslint e prettier sui file in stage.

## Prima di aprire la PR

```bash
pnpm lint && pnpm format:check
pnpm typecheck
pnpm test
dotnet build && dotnet test
node scripts/generate-sdk.mjs --check
```

Se hai toccato il frontend, aggiungi gli e2e (serve l'API attiva):

```bash
pnpm --filter @enterprise/web test:e2e
```

## La checklist

- [ ] La CI passerebbe (i comandi qui sopra sono verdi in locale).
- [ ] **Test**: unit sempre; integration se hai toccato il backend; e2e se hai
      toccato un flusso principale.
- [ ] **Documentazione aggiornata nello stesso commit**: `docs/` se è cambiata
      una regola, il README del modulo se è cambiato un contratto,
      [`wiki/patterns/`](README.md) se è cambiato un pattern.
- [ ] **SDK rigenerato e committato** se è cambiato un contratto API.
- [ ] **Design system**: nessun valore letterale nel markup; token nuovi
      documentati in `docs/design-system.md`.
- [ ] **Responsive**: pagina nuova o modificata **verificata a 375px**.
- [ ] Nessun endpoint nuovo senza `RequirePermission` o un `AllowAnonymous()`
      motivato da un commento.

## Cosa esegue la CI

`.github/workflows/ci.yml`, su ogni PR e su push a `main`/`develop`:

| Job        | Cosa fa                                                              |
| ---------- | -------------------------------------------------------------------- |
| **web**    | install → lint → format check → typecheck → test → build → Storybook |
| **sdk**    | `generate-sdk.mjs --check`: il generato committato è allineato       |
| **api**    | `dotnet build` + `dotnet test` in Release                            |
| **e2e**    | Playwright contro l'API reale                                        |
| **CodeQL** | analisi di sicurezza                                                 |

**Una modifica che rompe la CI non è finita.** Non è un modo di dire: gli
analyzer .NET sono configurati come errori, quindi un warning ferma la build.

## Errori tipici

**Commitlint rifiuta il messaggio.** Manca il tipo (`feat:`), oppure la
descrizione inizia in maiuscolo o finisce con un punto.

**La CI fallisce su format check ma in locale sembra a posto.** `pnpm format`
prima di committare — il pre-commit hook formatta solo i file in stage.

**La CI fallisce su «SDK is out of date».** Hai cambiato un contratto senza
rigenerare, o hai committato solo uno dei due file generati.

**Build rossa per un warning.** Gli analyzer sono errori di proposito: si
sistema il codice, non si abbassa la severità.

**Hai committato su `develop` per sbaglio.** Prima di committare guarda il
branch; su `main` non si committa mai.

## Correlati

- [Rivedere il codice di qualcun altro](code-review.md)
- [Rigenerare l'SDK](regenerate-sdk.md)
- [Checklist responsive](responsive-checklist.md)
