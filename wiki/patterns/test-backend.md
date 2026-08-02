# Testare il backend

**Fonte normativa**: [`CLAUDE.md`](../../CLAUDE.md) → «Nessuna feature è
completa senza test» · [`docs/backend.md`](../../docs/backend.md)
**Esempio nel codice**: `apps/api/tests/UnitTests/` ·
`apps/api/tests/IntegrationTests/`

## Quando serve

Sempre. La domanda non è _se_ scrivere test, ma _quale dei due tipi_.

| Scrivi un…      | Quando                                                                       |
| --------------- | ---------------------------------------------------------------------------- |
| **unit test**   | La logica sta in una classe e non tocca I/O: aggregazione, decisione, regola |
| **integration** | Contano SQL, schema, autenticazione, pipeline, confini fra moduli            |

Se per testare una logica devi montare mezzo host, quella logica probabilmente
va estratta in una classe — come è successo con `DashboardWidgetAggregator`,
nato proprio dall'esigenza di testare l'aggregazione senza un endpoint.

## Unit test

Niente I/O, niente host. Stack: **xUnit + Shouldly**.

```csharp
[Fact]
public async Task AFailingProvider_DoesNotBlankTheDashboard()
{
    var aggregator = Aggregator(
        new FailingProvider(),
        new StubProvider(new DashboardWidget("users.total", "Users"))
    );

    var widgets = await aggregator.GetWidgetsAsync();

    // La tile del modulo sano c'è ancora: una dashboard parziale batte
    // una pagina di errore, ed è il motivo per cui i provider sono isolati.
    widgets.Select(widget => widget.Id).ShouldBe(["users.total"]);
}
```

Il nome del test è una frase che descrive il **comportamento**, non il metodo
chiamato: `AFailingProvider_DoesNotBlankTheDashboard`, non `GetWidgets_Test2`.
Il commento spiega **perché** quel comportamento è quello giusto — è
l'informazione che un lettore futuro non può dedurre.

Se il tuo modulo è nuovo, aggiungine la `ProjectReference` in
`apps/api/tests/UnitTests/EnterpriseFramework.UnitTests.csproj`.

## Integration test

Postgres **vero** via Testcontainers (serve Docker attivo): stesso SQL, stesso
schema, stessi vincoli della produzione. Un database in-memory nasconde
esattamente i bug specifici del provider che ti interessa trovare.

```csharp
public sealed class DashboardModuleTests : IClassFixture<PostgresApiFactory>
{
    [Fact]
    public async Task Widgets_RequireAuthentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/dashboard/widgets", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
```

`IClassFixture<PostgresApiFactory>` = **un container per classe**. Metti nella
stessa classe i test che condividono lo stesso contesto: una classe per test
significa un container per test, e la suite diventa lenta abbastanza da non
essere più eseguita.

Cosa vale la pena coprire qui, e quasi nulla d'altro:

- che l'endpoint **richieda autenticazione e permesso** (è il confine di
  sicurezza reale);
- che un flusso **attraversi due moduli** (evento → subscriber → proiezione);
- che il **contratto** sia quello atteso (per esempio: gli enum viaggiano come
  stringa, non come intero).

## Come si esegue

```bash
dotnet test                                    # tutto
dotnet test apps/api/tests/UnitTests           # solo unit, veloce
dotnet test --filter "FullyQualifiedName~DashboardModuleTests"
```

Gli integration richiedono Docker in esecuzione.

## Errori tipici

**«The process cannot access the file … .dll».** Hai l'API in esecuzione:
tiene i lock sulle dll. Fermala prima di ricompilare. Il messaggio non lo dice.

**Un test passa da solo e fallisce nella suite.** Quasi sempre è il rate
limiter: i test condividono lo stesso «IP». `PostgresApiFactory` alza già i
budget — se aggiungi una factory tua, ricordati di fare lo stesso.

**Email duplicate fra test paralleli.** Usa `Guid.NewGuid()` per generarle, mai
`DateTime.Now`: i worker paralleli collidono sul millisecondo.

**Docker non attivo** → gli integration falliscono all'avvio del container, con
un errore che parla di Docker e non del tuo test. Non è il tuo test.

**Il test asserisce una posizione sullo schermo e fallisce.** Non è un test
backend: vedi [Testare il frontend](test-frontend.md).

## Correlati

- [Aggiungere un endpoint](add-endpoint.md)
- [Persistenza di un modulo](module-persistence.md)
- [Errori](errors.md)
