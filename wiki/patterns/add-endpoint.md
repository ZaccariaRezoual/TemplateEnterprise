# Aggiungere un endpoint

**Fonte normativa**: [`docs/backend.md`](../../docs/backend.md) ·
[`CLAUDE.md`](../../CLAUDE.md) → «Regole architetturali non negoziabili»
**Esempio nel codice**: `modules/users/backend/Features/UpdateUser/` +
`modules/users/backend/UsersModule.cs`

## Quando serve

Ogni volta che l'API deve esporre un'operazione nuova.

**Quando NON serve**: se l'operazione appartiene a un dominio che non è quello
del modulo su cui stai lavorando. In quel caso non aggiungere l'endpoint qui e
non chiamare i servizi dell'altro modulo: vedi
[Far parlare due moduli](module-to-module.md).

## Procedura

Un endpoint sono tre cose: un **command o query** (con il suo validator), un
**handler**, e una **riga di mapping** nel modulo. La logica sta nell'handler;
il modulo si limita a instradare.

1. **Crea la cartella della feature** — `modules/<modulo>/backend/Features/<Feature>/`

   Una cartella per feature, non una per tipo di file. `Features/UpdateUser/`,
   mai `Commands/` + `Handlers/` + `Validators/`.

2. **Dichiara il command (o la query) e il suo validator** — nello stesso file

   ```csharp
   /// <summary>
   /// Updates the editable fields of a user profile.
   /// </summary>
   /// <param name="UserId">Account whose profile is updated.</param>
   /// <param name="DisplayName">New display name.</param>
   public sealed record UpdateUserCommand(
       Guid UserId,
       string DisplayName,
       string? JobTitle,
       bool IsActive
   ) : IRequest<UserProfileDto>;

   /// <summary>Validation rules for <see cref="UpdateUserCommand"/>.</summary>
   public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
   {
       /// <summary>Initializes the rules.</summary>
       public UpdateUserCommandValidator()
       {
           RuleFor(c => c.UserId).NotEmpty();
           RuleFor(c => c.DisplayName).NotEmpty().MaximumLength(200);
           RuleFor(c => c.JobTitle).MaximumLength(200);
       }
   }
   ```

   Il validator non va registrato a mano: `AddValidatorsFromAssembly` nel
   modulo lo trova, e la pipeline MediatR lo esegue prima dell'handler.

3. **Scrivi l'handler** — stessa cartella

   Il tipo di ritorno è un **DTO/Contract**, mai un'entity EF: le entity non
   escono dall'API. Documenta con `<exception>` ogni errore della gerarchia
   condivisa che l'handler può lanciare.

4. **Mappa l'endpoint** in `<Modulo>Module.cs` → `MapEndpoints`

   ```csharp
   group
       .MapPut(
           "/{userId:guid}",
           async (Guid userId, UpdateUserRequest body, ISender sender, CancellationToken ct) =>
               TypedResults.Ok(
                   await sender.Send(
                       new UpdateUserCommand(userId, body.DisplayName, body.JobTitle, body.IsActive),
                       ct
                   )
               )
       )
       .WithName("usersUpdate")
       .WithSummary("Updates a user profile.")
       .RequirePermission(Permissions.Users.Write)
       .ProducesValidationProblem();
   ```

   Quattro dettagli che sembrano cosmetici e non lo sono:

   - **`TypedResults`, non `Results`**: solo il tipo concreto (`Ok<T>`,
     `NoContent`, `FileStreamHttpResult`) porta la forma della risposta nel
     documento OpenAPI, e quindi nell'SDK generato. Con `IResult` il client
     resta senza tipi e nessuno se ne accorge finché non lo usa.
   - **`WithName` è l'`operationId`**, cioè il nome del metodo nell'SDK.
     Rinominare l'handler non rinomina un metodo pubblico; cambiare `WithName`
     sì.
   - **`RequirePermission(...)`** e non un ruolo. Se il permesso non esiste
     ancora → [Aggiungere un permesso](add-permission.md).
   - **`WithSummary`** finisce nei commenti dell'SDK: scrivilo per chi
     consumerà l'SDK senza vedere il backend.

5. **Rigenera l'SDK** se il contratto è cambiato

   ```bash
   node scripts/generate-sdk.mjs
   ```

   Committa sia `apps/api/openapi/v1.json` sia lo schema generato: il primo
   rende la modifica di contratto visibile nel diff della PR.

## Autenticazione: già protetto, non «da proteggere poi»

La fallback policy del host richiede un caller autenticato. Un endpoint è
protetto per il solo fatto di esistere; l'accesso anonimo è un **opt-out
esplicito** con `AllowAnonymous()` e un commento che lo motiva.

## Come si verifica

```bash
dotnet build                      # 0 warning: gli analyzer sono errori
dotnet test                       # unit + integration
node scripts/generate-sdk.mjs --check   # SDK allineato al contratto
```

Poi apri `apps/api/openapi/v1.json` e controlla che la risposta abbia uno
`$ref` a uno schema, non un oggetto vuoto.

## Errori tipici

**L'SDK espone il metodo ma la risposta è `unknown`.** L'handler dichiara
`Task<IResult>` invece del tipo concreto. È già successo due volte in questo
repo (Storage e Dashboard) ed è invisibile finché non scrivi il frontend.

**L'endpoint risponde 401 e non capisci perché.** Non è un bug: è la fallback
policy. Se deve essere pubblico, dillo con `AllowAnonymous()`.

**Il validator non viene eseguito.** Il file è in un assembly che il modulo non
scansiona: `AddValidatorsFromAssembly(typeof(<Modulo>Module).Assembly)` guarda
solo il proprio.

**La CI fallisce su «SDK is out of date».** Hai cambiato un contratto senza
rigenerare. `node scripts/generate-sdk.mjs`, poi committa i due file generati.

**429 durante gli e2e.** Il rate limiter è per IP e i worker Playwright
condividono l'IP. Non alzare i limiti nel codice: sono già rilassati in
`appsettings.Development.json`.

## Correlati

- [Aggiungere un permesso](add-permission.md)
- [Errori](errors.md)
- [Testare il backend](test-backend.md)
- [Creare un modulo](create-module.md)
