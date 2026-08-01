# Errori

**Fonte normativa**: [`docs/backend.md`](../../docs/backend.md) →
«Cross-cutting pipeline» · [`CLAUDE.md`](../../CLAUDE.md) → «Errori»
**Esempio nel codice**: `apps/api/src/Application/Exceptions/` ·
`apps/api/src/Api/ErrorHandling/GlobalExceptionHandler.cs`

## Quando serve

Ogni volta che un'operazione non può concludersi e il client deve saperlo.

**Quando NON serve**: per un bug. Un `NullReferenceException` non va
trasformato in un errore «gentile» — deve restare un 500, perché è quello che
è. La gerarchia condivisa esiste per i fallimenti **attesi**; mascherare un
bug da errore di dominio significa non accorgersene mai.

## La regola in una riga

Lancia l'eccezione giusta e non pensare più alla risposta HTTP: la traduzione è
già scritta, una volta sola, nel `GlobalExceptionHandler`.

| Lanci                   | Il client riceve              | Quando                                    |
| ----------------------- | ----------------------------- | ----------------------------------------- |
| `ValidationException`   | **400** + dizionario `errors` | Input malformato (di norma dal validator) |
| `UnauthorizedException` | **401**                       | Non autenticato                           |
| `ForbiddenException`    | **403**                       | Autenticato ma senza permesso             |
| `NotFoundException`     | **404**                       | La risorsa non esiste                     |
| `BusinessException`     | **422**                       | Input valido, regola di dominio violata   |
| qualsiasi altra         | **500**, dettagli non esposti | Bug                                       |

Tutte derivano da `AppException`, e la regola vale anche al contrario: **una
eccezione che non deriva da `AppException` non deve mai essere lanciata di
proposito** per pilotare una risposta.

## Procedura

1. **Scegli il tipo.** La domanda che discrimina i due casi ambigui:

   - 404 vs 403: l'utente **non doveva sapere** che la risorsa esiste? Allora
     404, anche se il vero motivo è un permesso. Un 403 su una risorsa che non
     dovrebbe vedere le conferma che esiste.
   - 400 vs 422: la richiesta è **formalmente** sbagliata (campo mancante,
     formato errato) → 400, e di norma non la lanci tu, la lancia il validator.
     È formalmente corretta ma **il dominio la rifiuta** («email già
     registrata») → 422.

2. **Lancia, con un messaggio leggibile da un utente**

   ```csharp
   throw new BusinessException("This email address is already registered.");
   ```

   Il messaggio **arriva al client**: niente nomi di tabella, niente id
   interni, niente dettagli di infrastruttura.

3. **Documentala nell'handler**

   ```csharp
   /// <exception cref="NotFoundException">Thrown when the profile does not exist.</exception>
   ```

   Non è decorazione: è ciò che chi consuma l'SDK legge per sapere cosa può
   andare storto.

4. **Lato frontend** la gerarchia è speculare (`ApplicationError`,
   `ValidationError`, `BusinessError`, `NotFoundError`, …) e la mappatura da
   ProblemDetails avviene **solo** in `core/api/apiClient.ts`. Nessuna feature
   ispeziona uno status code a mano.

## Come si verifica

```bash
dotnet test
```

E a mano, guardando la forma della risposta, che deve essere ProblemDetails:

```bash
curl -i http://localhost:5080/api/users/00000000-0000-0000-0000-000000000000 \
  -H "Authorization: Bearer <token>"
```

## Errori tipici

**Un errore atteso esce come 500.** L'eccezione non deriva da `AppException`.

**Il messaggio contiene dettagli interni.** Ricorda che finisce nel body della
risposta: `$"User {id} missing in table users.profiles"` è un'informazione
regalata a chi sta sondando l'API.

**Hai catturato l'eccezione per restituire `Results.BadRequest(...)`.** Così
salti la traduzione centralizzata e produci una risposta con una forma diversa
da tutte le altre: il client ha un caso speciale da gestire per colpa tua.

**Validazione fatta a mano nell'handler.** Se è una regola di forma, va nel
validator: lì è eseguita dalla pipeline prima dell'handler, ed è visibile
anche al frontend attraverso lo stesso schema.

## Correlati

- [Aggiungere un endpoint](add-endpoint.md)
- [Testare il backend](test-backend.md)
