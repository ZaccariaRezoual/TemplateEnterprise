# Creare un modulo

**Fonte normativa**: [`docs/modules.md`](../../docs/modules.md) — il Module
Contract
**Esempio nel codice**: `modules/demo/` (lo scheletro minimo) ·
`modules/auth/` (il template canonico, completo)

## Quando serve

Quando aggiungi una capacità che ha un dominio proprio e che deve poter essere
installata o disabilitata senza toccare il resto.

**Quando NON serve**: se la cosa che stai aggiungendo appartiene a un dominio
già esistente, va in quel modulo. Un modulo per feature produce venti moduli
che dipendono l'uno dall'altro, cioè un monolite con più cartelle.

## Procedura

1. **Crea la struttura**

   ```
   modules/<nome>/
     README.md          decisioni e contratti del modulo
     module.json        manifest
     backend/           <Nome>Module.cs, Features/, Domain/, Persistence/
     shared/            contratti pubblici (eventi, DTO) — solo se servono
     frontend/          package @enterprise/module-<nome> — solo se ha UI
     tests/
   ```

2. **Scrivi il manifest** — `modules/<nome>/module.json`

   ```json
   {
     "name": "Demo",
     "version": "1.0.0",
     "dependencies": [],
     "enabled": true
   }
   ```

   `dependencies` sono nomi di moduli, e il `ModuleLoader` li ordina
   topologicamente all'avvio. Dichiarare una dipendenza che non serve
   costringe chi disinstalla l'altro modulo a toccare anche questo.

3. **Crea il progetto** — `modules/<nome>/backend/EnterpriseFramework.Modules.<Nome>.csproj`

   Il manifest va **embedded**, altrimenti il loader non lo trova a runtime:

   ```xml
   <ItemGroup>
     <EmbeddedResource Include="..\module.json" LogicalName="module.json" />
   </ItemGroup>
   ```

   Referenzia `Application` e `Modules.Abstractions`. **Mai** il progetto
   `backend` di un altro modulo: solo i suoi `shared/`.

4. **Implementa `IModule`** — `<Nome>Module.cs`

   ```csharp
   public sealed class DemoModule : IModule
   {
       public string Name => "Demo";

       public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
       {
           services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DemoModule).Assembly));
           services.AddValidatorsFromAssembly(typeof(DemoModule).Assembly);
       }

       public void MapEndpoints(IEndpointRouteBuilder endpoints)
       {
           var group = endpoints.MapGroup("/api/demo").WithTags("Demo");
       }
   }
   ```

   `Name` deve coincidere con `module.json`: è la chiave con cui la
   configurazione lo abilita (`Modules:<Nome>:Enabled`).

5. **Se ha dati propri** → [Persistenza di un modulo](module-persistence.md).
   Schema PostgreSQL dedicato e migrations proprie: nessuna tabella condivisa,
   nessuna foreign key verso lo schema di un altro modulo.

6. **Registralo nel host** — `apps/api/src/Api/Program.cs`

   ```csharp
   var modules = ModuleLoader.Load(
       [
           typeof(DemoModule).Assembly,
           // …
       ],
       builder.Configuration
   ```

   E aggiungi il progetto alla solution:

   ```bash
   dotnet sln add modules/<nome>/backend/EnterpriseFramework.Modules.<Nome>.csproj
   ```

7. **Scrivi il README del modulo.** Non è burocrazia: è il posto dove vivono le
   decisioni («perché un outbox invece dell'invio inline»), che altrimenti
   restano nella testa di chi l'ha scritto. Guarda
   `modules/realtime/README.md` per il livello di dettaglio atteso.

8. **Se ha frontend** → package `@enterprise/module-<nome>` in `frontend/`,
   più la dipendenza in `apps/web/package.json`. Il modulo si aggancia ai seam
   che il core espone, mai il contrario.

## Come si verifica

```bash
dotnet build && dotnet test
```

Poi la prova che conta davvero, cioè che il modulo sia rimovibile:

```jsonc
// appsettings.Development.json
"Modules": { "<Nome>": { "Enabled": false } }
```

L'applicazione deve partire e funzionare, con solo quella capacità assente. Se
qualcos'altro si rompe, hai una dipendenza nascosta.

## Errori tipici

**`ModuleLoader` non trova il modulo.** Il `module.json` non è embedded, oppure
il `LogicalName` non è esattamente `module.json`.

**Le classi del frontend non hanno stili.** Tailwind non scansiona i package
del workspace: aggiungi il percorso alle direttive `@source` in
`apps/web/src/assets/styles/main.css`. Nessun errore di build — semplicemente
i pulsanti diventano invisibili.

**`dotnet build` chiede un database.** La build genera l'OpenAPI avviando il
host, e in Development le migrations partono all'avvio. Serve Postgres attivo,
oppure `Modules:AutoMigrate=false`.

**Ordine di avvio inatteso.** Gli hosted service partono nell'ordine di
caricamento dei moduli, che è un grafo di **dipendenze**, non di seeding: un
modulo senza dipendenze parte per primo. Se il tuo lavoro di avvio ha bisogno
che altri moduli siano pronti, agganciati a `ApplicationStarted` (vedi
`BootstrapAdminSeeder` in `modules/auth`).

## Correlati

- [Persistenza di un modulo](module-persistence.md)
- [Far parlare due moduli](module-to-module.md)
- [Aggiungere un endpoint](add-endpoint.md)
