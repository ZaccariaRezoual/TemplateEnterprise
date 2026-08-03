# Calcolare disponibilità e prenotare senza sbagliare

**Fonte normativa**: [`modules/appointments/README.md`](../../modules/appointments/README.md) ·
[`docs/modules.md`](../../docs/modules.md)
**Esempio nel codice**: `modules/appointments/backend/Domain/Availability/AvailabilityCalculator.cs`

## Quando serve

Quando stai scrivendo logica che risponde a «quando è possibile?» — slot di
prenotazione, turni, finestre di consegna — o qualsiasi cosa in cui **due
richieste possono contendersi la stessa risorsa**.

Non è una pagina sul modulo Appointments: è sui tre schemi che quel modulo
usa, e che servono ogni volta che il problema ha la stessa forma.

## 1. La logica difficile va in una funzione pura

Il calcolo degli slot non tocca il database, non conosce HTTP e **non legge
l'orologio**: l'istante corrente è un parametro.

```csharp
public static IReadOnlyList<AvailableDay> Calculate(
    DateOnly from, DateOnly to,
    IReadOnlyCollection<AvailabilityRule> rules,
    IReadOnlyCollection<AvailabilityOverride> overrides,
    IReadOnlyCollection<BusyInterval> busy,
    AvailabilityRequest request)   // ← fuso, durate, limiti, NowUtc
```

Non è ordine per l'ordine. I casi che contano — il giorno del cambio ora, la
giornata già piena, la chiusura sopra un'apertura, lo slot libero ma troppo
vicino — **sono testabili solo se gli ingressi sono argomenti**. Con un
`DateTime.UtcNow` dentro, il test dell'ora legale si può scrivere due volte
l'anno.

L'handler raccoglie gli ingredienti e chiama la funzione. Non fa aritmetica.

## 2. I fusi orari: tre regole, nessuna negoziabile

- **Si persiste UTC.** Sempre. Un istante non ha fuso.
- **Gli orari di apertura sono locali** al fuso dell'attività, e si salvano
  così. Salvarli in UTC li farebbe spostare di un'ora due volte l'anno mentre
  il cartello sulla porta resta fermo.
- **La griglia si percorre in ora locale**, e si converte dopo. È questo che
  fa uscire giusti i giorni del cambio da soli: l'ora che a marzo non esiste
  non ha istante e viene saltata, quella che a ottobre esiste due volte viene
  offerta una volta sola.

```csharp
if (request.TimeZone.IsInvalidTime(startLocal)) continue;   // l'ora saltata
var startUtc = ToUtc(startLocal, request.TimeZone);          // mai aritmetica
```

Il fuso del **browser** serve a mostrare un orario, mai a calcolarlo.

## 3. La contesa la risolve il database

«Esiste già qualcosa a quell'ora?» seguito da un `INSERT` **non funziona**: fra
la lettura e la scrittura c'è uno spazio, e due richieste ci passano
comodamente.

```sql
ALTER TABLE appointments.appointments
  ADD CONSTRAINT appointments_no_overlap
  EXCLUDE USING gist (tstzrange("StartUtc", "EndUtc", '[)') WITH &&)
  WHERE ("Status" = 'Confirmed');
```

EF non sa modellare un vincolo di esclusione: si scrive a mano nella
migration, con il `Down` che lo rimuove.

Il codice **si aspetta il rifiuto**, non cerca di prevenirlo:

```csharp
catch (DbUpdateException ex) when (SlotConflict.IsSlotConflict(ex))
{
    throw new BusinessException("Quell'orario è appena stato preso.");
}
```

Un 422 con una frase leggibile, non un 500: due operatori che confermano nello
stesso istante stanno facendo una cosa legittima, e uno dei due deve perdere
**sapendo perché**.

### Attenzione al deadlock

Se l'operazione scrive più righe (confermare ne tocca due: la propria e quelle
che sposta), due transazioni possono prenderle in ordine opposto. Sintomo: il
test passa da solo e fallisce sotto carico.

La cura è togliere il ciclo, non correrlo:

```csharp
await _dbContext.Database.ExecuteSqlInterpolatedAsync(
    $"SELECT pg_advisory_xact_lock({ConfirmationLockKey})", ct);
```

Si può fare **perché la conferma è rara** — una persona che clicca. Non
serializzare mai una rotta calda con questo.

## 4. L'idempotenza è un indice unico

Per il lavoro che una seconda istanza può ripetere — un promemoria, un invio —
la garanzia è nello schema:

```csharp
reminder.HasIndex(r => new { r.AppointmentId, r.Kind }).IsUnique();
```

Chi inserisce per primo manda; il secondo viola il vincolo e non manda nulla.
E **si inserisce prima di pubblicare**: se il processo muore in mezzo il
cliente perde un promemoria, al contrario ne riceve uno per ogni crash.

## Come si verifica

```bash
dotnet test --filter "FullyQualifiedName~AvailabilityCalculator"   # i casi limite
dotnet test --filter "FullyQualifiedName~AppointmentsModuleTests"  # la contesa, con Postgres vero
```

Il test della doppia conferma **richiede un database reale**: un provider
in-memory non ha vincoli di esclusione, quindi il bug che stai prevenendo
passerebbe tutti i test.

## Errori tipici

**Gli slot sono giusti d'inverno e sbagliati d'estate di un'ora.** La griglia
viene percorsa in UTC. Va percorsa in ora locale e convertita dopo.

**`Cannot write DateTime with Kind=Unspecified`.** Un `DateTime` che arriva da
JSON o da una query string non ha `Kind`, e PostgreSQL lo rifiuta su una
colonna `timestamptz`. Normalizzalo all'ingresso — vedi `UtcInstant.From`.

**«Expected to affect 1 row(s), but actually affected 0».** Stai aggiungendo
un figlio a un aggregato caricato, e EF lo crede già esistente perché la
chiave è valorizzata e dichiarata generata dal database. Se gli id li assegna
il dominio, dichiaralo: `Property(x => x.Id).ValueGeneratedNever()`.

**Il test di concorrenza passa da solo e fallisce nella suite.** È un
deadlock, non un flake. Vedi §3.

## Correlati

- [Persistenza di un modulo](module-persistence.md)
- [Far parlare due moduli](module-to-module.md)
- [Testare il backend](test-backend.md)
