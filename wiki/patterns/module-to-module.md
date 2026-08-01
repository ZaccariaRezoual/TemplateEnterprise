# Far parlare due moduli

**Fonte normativa**: [`docs/modules.md`](../../docs/modules.md) §5–6 ·
[`CLAUDE.md`](../../CLAUDE.md) → «Regole architetturali non negoziabili»
**Esempio nel codice**: `modules/authorization/Features/GrantDefaultRole/`
(evento) · `apps/api/src/Application/Abstractions/IUserClaimsEnricher.cs`
(extension point) · `modules/users/Features/Projection/` (proiezione)

## Quando serve

Ogni volta che stai per scrivere, dentro il modulo A, un `using` verso il
progetto `backend` del modulo B. **Quello è il momento di fermarsi.**

Un modulo non chiama i servizi di un altro e non legge le sue tabelle. Non è
purismo: è ciò che rende un modulo disinstallabile, ed è l'unica proprietà del
framework che si perde per sempre alla prima eccezione.

## La scelta: tre strumenti, una domanda ciascuno

| Cosa ti serve                                  | Strumento           |
| ---------------------------------------------- | ------------------- |
| «È successo qualcosa, chi vuole reagisca»      | **Evento pubblico** |
| «Mi serve un comportamento che non so fornire» | **Extension point** |
| «Mi servono dati che appartengono a un altro»  | **Proiezione**      |

### 1. Evento pubblico — notifica

Il publisher dichiara cosa è successo e non sa chi ascolta.

L'evento vive nel progetto `shared/` del publisher, che è un **contratto
pubblico**: i subscriber referenziano solo quello.

```csharp
// modules/auth/shared/Events/UserRegistered.cs
public sealed record UserRegistered(Guid UserId, string Email, string DisplayName) : IDomainEvent
```

Il subscriber sta nel modulo che reagisce:

```csharp
// modules/authorization/backend/Features/GrantDefaultRole/
public sealed partial class GrantDefaultRoleOnUserRegistered
    : INotificationHandler<DomainEventNotification<UserRegistered>>
```

Regola sui contratti: **si aggiungono campi, non se ne rimuovono né si
riusano**. Cambiare un evento pubblico è un breaking change per moduli che
potresti non conoscere.

Il subscriber deve essere **facoltativo**: se il modulo che ascolta viene
disabilitato, l'operazione originale continua a funzionare (una registrazione
senza il modulo Authorization crea un account senza permessi, non un errore).

### 2. Extension point — comportamento

Quando non basta essere avvisati, ma serve che qualcun altro _faccia_ qualcosa
e restituisca un risultato. L'interfaccia la dichiara il **host**, in
`apps/api/src/Application/Abstractions/`, e i due moduli dipendono da lì —
mai l'uno dall'altro.

```csharp
// Auth chiama questo mentre emette un token, senza sapere cosa siano i ruoli.
public interface IUserClaimsEnricher
{
    Task<IReadOnlyCollection<Claim>> GetClaimsAsync(Guid userId, CancellationToken ct);
}
```

Authorization lo implementa e lo registra nel proprio `ConfigureServices`.
Senza quel modulo non c'è nessun enricher, i token non hanno claim di
permessi, e niente si rompe.

Gli extension point esistenti: `IUserClaimsEnricher`, `IRealtimeEvent`,
`IDashboardWidgetProvider`, `ITenantContext`, `IFeatureFlags`, `ICacheService`.
Prima di crearne uno nuovo, controlla che il caso non sia già coperto.

### 3. Proiezione — dati

Il modulo che ha bisogno dei dati altrui **se li costruisce**, sottoscrivendo
gli eventi pubblici dell'altro e mantenendo la propria copia nel proprio
schema.

`modules/users` fa esattamente questo: il profilo utente esiste perché
`CreateProfileOnUserRegistered` reagisce a un evento di Auth, non perché
qualcuno legga `auth.users`.

Le entità altrui si referenziano **per id**, mai con una foreign key verso un
altro schema: una FK cross-schema rende i due moduli un solo modulo con due
cartelle.

Il costo è la consistenza eventuale, ed è un costo reale: la proiezione è
aggiornata un istante dopo. Se ti serve una lettura sempre perfettamente
allineata, probabilmente i due moduli sono uno solo.

## Come si verifica

Disabilita il modulo subscriber e riavvia:

```jsonc
"Modules": { "<Subscriber>": { "Enabled": false } }
```

L'operazione del publisher deve continuare a funzionare, con la sola
conseguenza attesa mancante.

```bash
dotnet test   # gli integration test coprono i flussi cross-modulo
```

## Errori tipici

**L'evento non arriva a nessuno.** Il bus fa dispatch sul tipo **concreto**: se
lo pubblichi da un ciclo tipizzato `IDomainEvent`, il tipo statico è quello
sbagliato. Pubblica dal tipo reale (`MediatREventBus` usa `GetType()`, ma il
tuo codice chiamante deve comunque passare l'evento concreto).

**Il subscriber non viene registrato.** Il modulo che lo contiene non chiama
`AddMediatR(...)` sul proprio assembly.

**La proiezione è vuota.** Il modulo è stato installato dopo che gli eventi
erano già passati, e gli eventi **non vengono replayati**. Serve un backfill
esplicito una tantum.

**Hai messo l'evento nel progetto `backend`.** Allora il subscriber deve
referenziare l'implementazione del publisher, cioè esattamente ciò che il
pattern evita. L'evento va in `shared/`.

## Correlati

- [Creare un modulo](create-module.md)
- [Rendere un evento realtime](make-it-realtime.md)
- [Contribuire un widget](contribute-dashboard-widget.md)
