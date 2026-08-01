# Persistenza di un modulo

**Fonte normativa**: [`docs/modules.md`](../../docs/modules.md) §6 ·
[`CLAUDE.md`](../../CLAUDE.md) → «Moduli indipendenti»
**Esempio nel codice**: `modules/users/backend/Persistence/` ·
`modules/auth/backend/Persistence/`

## Quando serve

Quando il modulo ha dati propri da salvare.

**Quando NON serve**: se i dati che ti servono appartengono a un altro modulo.
Non aggiungere una tabella che li duplica «per comodità»: vedi
[Far parlare due moduli](module-to-module.md) → proiezione.

## Il principio

Ogni modulo possiede il proprio `DbContext`, il proprio **schema PostgreSQL** e
la propria storia di migrations. Installare o rimuovere un modulo non tocca i
dati di nessun altro. Non esistono foreign key fra schemi diversi: le entità
altrui si referenziano per id.

## Procedura

1. **Crea il `DbContext`** — `modules/<nome>/backend/Persistence/<Nome>DbContext.cs`

   ```csharp
   public sealed class UsersDbContext : DbContext
   {
       /// <summary>PostgreSQL schema holding every table of this module.</summary>
       public const string Schema = "users";

       public DbSet<UserProfile> Profiles => Set<UserProfile>();

       protected override void OnModelCreating(ModelBuilder modelBuilder)
       {
           modelBuilder.HasDefaultSchema(Schema);
           // …
       }
   }
   ```

2. **Registralo nel modulo**, con la storia delle migrations nello **stesso
   schema** — altrimenti tutti i moduli condividono una tabella di history e
   l'indipendenza è persa:

   ```csharp
   services.AddDbContext<UsersDbContext>(options =>
       options.UseNpgsql(
           connectionString,
           npgsql =>
               npgsql.MigrationsHistoryTable("__ef_migrations_history", UsersDbContext.Schema)
       )
   );

   if (configuration.ShouldAutoMigrate(Name))
   {
       services.AddHostedService<UsersDbMigrator>();
   }
   ```

3. **Crea la prima migration**

   ```bash
   dotnet tool restore   # una volta sola, per dotnet-ef
   dotnet ef migrations add Initial \
     --project modules/<nome>/backend \
     --startup-project apps/api/src/Api
   ```

4. **Applicala.** In Development parte da sola all'avvio
   (`Modules:AutoMigrate`). In produzione è uno **step esplicito di release**:
   nessuna istanza deve modificare lo schema come effetto collaterale
   dell'avvio.

   ```bash
   dotnet ef database update \
     --project modules/<nome>/backend \
     --startup-project apps/api/src/Api
   ```

## Multi-tenant (opt-in)

Se il modulo deve essere multi-tenant, l'entità implementa `ITenantOwned` e il
`DbContext` chiama `ApplyTenantFilters`. **Mai filtrare a mano**: una `Where`
dimenticata non dà errore, restituisce i dati di un altro cliente. Vedi
[`docs/enterprise-features.md`](../../docs/enterprise-features.md).

## Come si verifica

```bash
dotnet test   # gli integration test usano Testcontainers: SQL e schema reali
```

E controlla che lo schema sia davvero separato:

```bash
docker exec enterprise-framework-postgres-1 \
  psql -U app -d enterprise -c '\dn'
```

## Errori tipici

**Le tabelle finiscono in `public`.** Manca `HasDefaultSchema(Schema)`.

**`dotnet ef` non trova il DbContext.** Servono entrambi i progetti:
`--project` è il modulo, `--startup-project` è sempre `apps/api/src/Api`.

**La migration nasce vuota.** Il `DbContext` non è registrato nel container, o
il modulo è disabilitato in configurazione: il tool costruisce il host reale.

**`dotnet build` fallisce chiedendo il database.** La build avvia il host per
generare l'OpenAPI, e in Development le migrations partono all'avvio: serve
Postgres su, oppure `Modules:AutoMigrate=false`.

**Analyzer in errore sui file di migration.** Non riformattarli: sono generati.
Il repository li esclude già con `[**/Migrations/*.cs] generated_code = true`
in `.editorconfig`.

## Correlati

- [Creare un modulo](create-module.md)
- [Far parlare due moduli](module-to-module.md)
- [Testare il backend](test-backend.md)
