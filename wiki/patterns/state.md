# Dove tenere lo stato

**Fonte normativa**: [`docs/frontend.md`](../../docs/frontend.md) → «State: two
homes, no overlap» · [`CLAUDE.md`](../../CLAUDE.md) → «Stato»
**Esempio nel codice**: `apps/web/src/features/demo/composables/useDemo.ts`
(server) · `apps/web/src/features/demo/stores/demoPreferences.store.ts` (client)

## La domanda, una sola

> **Questo dato può cambiare senza che l'utente faccia niente?**

- **Sì** → viene dal server → **TanStack Query**
- **No** → esiste solo in questa sessione → **Pinia**

Non c'è una terza casa e non c'è sovrapposizione: **mai dati del server dentro
Pinia.**

| Dato                                | Casa           |
| ----------------------------------- | -------------- |
| Elenco utenti, notifiche, widget    | TanStack Query |
| Sidebar aperta/chiusa, tema, filtri | Pinia          |
| Sessione (access token in memoria)  | Pinia          |
| Profilo dell'utente loggato         | TanStack Query |

## Perché la regola è rigida

Un dato del server messo in Pinia diventa una **seconda copia** con un ciclo di
vita che scrivi tu: quando si aggiorna, quando scade, cosa succede se due
componenti la modificano. Sono esattamente i problemi che TanStack Query
risolve — caching, deduplica, refetch, stati di caricamento ed errore — e
riscriverli a mano significa riscriverli peggio.

Il sintomo tipico arriva mesi dopo: due schermate mostrano numeri diversi per
la stessa cosa, e nessuno sa quale sia giusto.

## Server state — TanStack Query

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

Dopo una mutation **invalida**, non applicare una patch alla cache: il refetch
ritorna ciò che l'utente ha davvero il diritto di vedere.

## Client state — Pinia

Solo ciò che nasce e muore nel browser: preferenze di visualizzazione, stato
della UI, sessione.

```ts
const session = useSessionStore();
```

L'access token vive **solo in memoria** (quindi in uno store), mai in
`localStorage`. Il refresh token sta in un cookie httpOnly che JavaScript non
può leggere: è ciò che limita il danno di una XSS.

## Il caso di confine

«Il profilo dell'utente loggato»: è dato del server, quindi query. Ma la
**sessione** (sono autenticato? con quale token?) è client state, perché la
possiede il browser.

Regola pratica: se ricaricando la pagina il dato deve essere richiesto al
server, è server state.

## Come si verifica

```bash
pnpm -r typecheck && pnpm test
```

E una verifica che vale più dei test: cerca `useQuery` dentro uno store Pinia,
oppure una `ref` che contiene una risposta dell'API. Se la trovi, hai una
seconda copia.

## Errori tipici

**Uno store Pinia che chiama il feature service e salva il risultato.** È
server state travestito. Sposta la chiamata in un composable con `useQuery`.

**`refetch()` chiamato a mano dopo ogni azione.** Serve un
`invalidateQueries` in `onSuccess`, così ogni consumatore della chiave si
aggiorna, non solo quello che ha agito.

**Il token in `localStorage` «perché è più comodo».** Diventa leggibile da
qualunque script iniettato nella pagina. In memoria, sempre.

**Due chiavi diverse per lo stesso dato.** Nasce quando le chiavi si scrivono
inline: tienile nell'oggetto `<feature>Keys`.

## Correlati

- [Aggiungere una feature frontend](add-feature.md)
- [Rendere un evento realtime](make-it-realtime.md)
