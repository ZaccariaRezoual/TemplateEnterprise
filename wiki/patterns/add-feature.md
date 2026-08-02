# Aggiungere una feature frontend

**Fonte normativa**: [`docs/frontend.md`](../../docs/frontend.md) →
«The request path» · [`CLAUDE.md`](../../CLAUDE.md) → «Feature-first»
**Esempio nel codice**: `apps/web/src/features/demo/` ·
`modules/dashboard/frontend/src/`

## Quando serve

Quando aggiungi una schermata o un insieme di schermate che consumano l'API.

**Quando NON serve**: se la funzionalità appartiene a un modulo, il codice va
nel frontend di quel modulo (`modules/<nome>/frontend/`), non in `apps/web`.
La domanda è: «servirebbe ad almeno due progetti?». Se sì, è un modulo.

## La catena, che non si salta mai

```
Componente → Composable → Feature service (api/) → SDK → API
```

Ogni anello ha un compito, e ogni scorciatoia costa cara:

- il **componente** non sa che esistono le API;
- il **composable** possiede lo stato server (TanStack Query) e le query key;
- il **feature service** è l'unico posto che sa **quali** operazioni usa la
  feature;
- l'**SDK** è generato: nessuno costruisce URL a mano.

Le preoccupazioni trasversali — correlation id, token, mappatura degli errori —
vivono **solo** in `core/api/apiClient.ts`. Una feature che le tocca le sta
duplicando.

## Procedura

1. **Crea la cartella**, con dentro tutto ciò che la feature possiede:

   ```
   apps/web/src/features/<nome>/
     api/          <nome>.api.ts        il feature service
     composables/  use<Nome>.ts         query, mutation, query key
     components/   pezzi riusabili nella feature
     pages/        una per route
     stores/       solo se serve client state
     types/        tipi della feature
     validators/   schemi Zod
     routes.ts     le route della feature
   ```

2. **Scrivi il feature service** — `api/<nome>.api.ts`

   ```ts
   export const demoApi = {
     ping(signal?: AbortSignal): Promise<IPingResponse> {
       return request(() => api.GET("/api/demo/ping", signal === undefined ? {} : { signal }));
     },

     echo(text: string): Promise<IEchoResponse> {
       return request(() => api.POST("/api/demo/echo", { body: { text } }));
     },
   };
   ```

   I path sono controllati contro il documento OpenAPI **a compile time**: un
   endpoint rinominato rompe la build, non la produzione. Accetta un
   `AbortSignal` sulle letture, così TanStack Query può annullare.

3. **Scrivi il composable** — `composables/use<Nome>.ts`, con le query key
   centralizzate:

   ```ts
   export const demoKeys = {
     all: ["demo"] as const,
     ping: () => [...demoKeys.all, "ping"] as const,
   };

   export function usePing() {
     return useQuery<IPingResponse>({
       queryKey: demoKeys.ping(),
       queryFn: ({ signal }) => demoApi.ping(signal),
     });
   }
   ```

   Le chiavi non si scrivono a mano nei componenti: l'invalidazione (anche
   quella che arriva dal realtime) usa queste.

4. **Dopo una mutation, invalida** — non aggiornare la cache a mano:

   ```ts
   return useMutation({
     mutationFn: (text: string) => demoApi.echo(text),
     onSuccess: () => queryClient.invalidateQueries({ queryKey: demoKeys.ping() }),
   });
   ```

   Il refetch ritorna ciò che l'utente ha davvero il diritto di vedere. Una
   patch locale ritorna ciò che il client ha immaginato.

5. **Scrivi le pagine** usando solo componenti del design system e token
   semantic → [Scegliere il token giusto](use-design-tokens.md). La pagina è
   mobile-first e non è finita finché non l'hai vista a 375px →
   [Checklist responsive](responsive-checklist.md).

6. **Registra le route** — `routes.ts` nella feature, poi una riga in
   `apps/web/src/router/index.ts`:

   ```ts
   {
     path: "/users",
     name: "users",
     component: () => import("./pages/UsersPage.vue"),
     meta: { title: "Users", requiresAuth: true, permissions: ["users.read"] },
   }
   ```

   `permissions` nel meta è applicato dalla guard del modulo Authorization: il
   requisito sta accanto alla route che protegge, non in una tabella centrale
   che nessuno aggiorna.

## Come si verifica

```bash
pnpm -r typecheck
pnpm test
pnpm --filter @enterprise/web test:e2e   # richiede l'API attiva
```

## Errori tipici

**Hai importato l'SDK dentro un componente.** Funziona, e a quel punto la
gestione degli errori e del token è duplicata in un posto in cui nessuno la
cercherà.

**Hai messo i dati del server in Pinia.** Vedi [Dove tenere lo
stato](state.md): è la scelta che poi si paga per anni.

**`invalidateQueries` non aggiorna niente.** La chiave usata per invalidare non
è la stessa usata dalla query. Per questo le chiavi stanno in un unico oggetto.

**La pagina è bianca dopo un errore API.** Non hai gestito `isError`: ogni
query ne ha tre di stati, e mostrarne uno solo è metà lavoro.

**404 su una route che esiste.** L'import in `router/index.ts` manca: le route
della feature non si registrano da sole.

## Correlati

- [Dove tenere lo stato](state.md)
- [Rigenerare l'SDK](regenerate-sdk.md)
- [Nascondere ciò che l'utente non può fare](client-permissions.md)
- [Testare il frontend](test-frontend.md)
