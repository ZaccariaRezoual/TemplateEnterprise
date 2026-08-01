# Rendere un evento realtime

**Fonte normativa**: [`modules/realtime/README.md`](../../modules/realtime/README.md) ·
[`CLAUDE.md`](../../CLAUDE.md) → «Event-Driven»
**Esempio nel codice**: `apps/api/src/Application/Abstractions/IRealtimeEvent.cs` ·
`modules/notifications/`

## Quando serve

Quando un cambiamento deve comparire sullo schermo di chi è già connesso,
senza che ricarichi la pagina.

**Quando NON serve**: se il dato è già rifetchato di continuo, o se la
tempestività non conta davvero. Una connessione WebSocket e un canale in più
sono un costo permanente; una `refetchInterval` è a volte la risposta onesta.

## Il principio

Il modulo che pubblica l'evento **non sa che SignalR esiste**. Dichiara cosa è
successo e chi è interessato; il modulo Realtime scopre da solo le
implementazioni di `IRealtimeEvent` all'avvio e le distribuisce.

## Procedura

1. **Fai implementare `IRealtimeEvent` al tuo evento di dominio**

   ```csharp
   public sealed record OrderShipped(Guid OrderId, Guid CustomerId) : IRealtimeEvent
   {
       public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
       public RealtimeAudience Audience => RealtimeAudience.ForUser(CustomerId);
       public string Channel => "order.shipped";
   }
   ```

   Nessuna registrazione: l'intera integrazione è questa.

2. **Scegli l'audience.** Si risolve **solo lato server** — un client non
   chiede mai di ricevere il traffico di un gruppo, perché gli basterebbe
   nominarlo per leggere gli eventi altrui.

   | Metodo                            | Raggiunge                                  |
   | --------------------------------- | ------------------------------------------ |
   | `RealtimeAudience.ForUser(id)`    | tutte le connessioni di quell'account      |
   | `RealtimeAudience.ForGroup(name)` | un gruppo assegnato dal server (es. ruolo) |
   | `RealtimeAudience.Everyone`       | tutti i connessi — da usare con parsimonia |

   Attenzione ai gruppi per ruolo in un'installazione multi-tenant:
   `role:Admin` raggiunge gli amministratori di **tutti** i tenant.

3. **Il payload è pubblico.** Quello che l'evento espone viene serializzato e
   inviato: non marcare mai come realtime un evento che porta segreti o dati di
   un altro utente.

4. **Pubblicalo normalmente** sull'event bus. Nient'altro da fare.

5. **Lato client**, la feature non tocca mai SignalR:

   ```ts
   useRealtimeInvalidation("order.shipped", ["orders"]); // di norma questo
   useRealtimeEvent<IOrderShipped>("order.shipped", (order) => { … });
   ```

   Preferisci **invalidare** invece di applicare una patch alla cache: il
   refetch ritorna ciò che l'utente ha davvero il diritto di vedere, e il
   server resta l'unica autorità.

## Gli eventi non vengono replayati

Alla riconnessione **non arriva nulla di ciò che è passato mentre eri
offline**. È una scelta esplicita: ciò che deve sopravvivere a una
disconnessione va **persistito e rifetchato**. Per questo il modulo
Notifications salva la notifica prima di pubblicarla.

Se il tuo evento non sopravvive a un tunnel in metropolitana, non è realtime:
è un dato che ti sei dimenticato di salvare.

## Come si verifica

```bash
dotnet test
pnpm --filter @enterprise/web test:e2e   # e2e/realtime.spec.ts, API attiva
```

A mano: due tab dello stesso account devono aggiornarsi entrambe, e un secondo
account non deve ricevere nulla.

## Errori tipici

**In sviluppo non arriva niente.** Il proxy Vite deve avere `ws: true` sul path
`/hubs`: senza, la richiesta di negotiate passa ma l'upgrade a WebSocket no, e
il client degrada in silenzio.

**429 sul negotiate.** Il rate limiter globale conta le riconnessioni. Il path
`/hubs` è già esentato — se lo reintroduci, gli e2e cominciano a fallire in
modo intermittente.

**L'evento arriva a chi non doveva.** Audience sbagliata: `ForGroup("role:…")`
attraversa i tenant. Un'audience vuota, invece, non raggiunge **nessuno** — mai
tutti: un evento mal configurato deve essere innocuo, non una fuga di dati.

**Il test e2e è instabile.** Non aspettare l'assenza dell'indicatore di
disconnessione (è assente anche prima che compaia): aspetta lo stato esplicito
`data-status="connected"`.

## Correlati

- [Far parlare due moduli](module-to-module.md)
- [Contribuire un widget](contribute-dashboard-widget.md)
