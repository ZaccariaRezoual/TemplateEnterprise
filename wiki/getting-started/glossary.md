# Glossario

Le parole che in questo repository hanno un significato **preciso**, diverso da
quello generico che hanno altrove. Tienilo aperto le prime settimane.

### Modulo

Unità installabile e disinstallabile in `modules/<nome>/`: manifest, backend,
eventualmente frontend, persistenza propria. Non è «una cartella di codice
correlato»: se non si può disabilitare senza rompere il resto, non è un modulo.

### Seam / extension point

Interfaccia dichiarata dal **host** in `Application/Abstractions/` su cui un
modulo si aggancia. Serve quando un modulo ha bisogno di un _comportamento_ che
non può fornirsi. Esempi: `IUserClaimsEnricher`, `IRealtimeEvent`,
`IDashboardWidgetProvider`.

Il punto è la direzione: entrambi i moduli dipendono dall'astrazione, mai l'uno
dall'altro.

### Contratto pubblico

Tipo che vive nel progetto `shared/` di un modulo e che altri moduli
referenziano: eventi, DTO. Cambiarlo è un breaking change per moduli che
potresti non conoscere — **si aggiungono campi, non se ne rimuovono**.

### Proiezione

Copia dei dati di un altro modulo, costruita sottoscrivendone gli eventi
pubblici e mantenuta nel proprio schema. È il modo — l'unico — in cui un modulo
usa dati altrui. `modules/users` ne è l'esempio.

Non è una cache: è un dato di cui il modulo diventa proprietario.

### Permesso vs ruolo

**Permesso** (`users.read`): il contratto stabile su cui si decide. È l'unica
cosa che endpoint e UI controllano.
**Ruolo** (`Admin`, `BasicUser`): un raggruppamento amministrativo che i
clienti riorganizzano. Non si controlla mai un ruolo nel codice.

### Bootstrap admin

L'account seedato all'avvio (`admin@example.com` in Development) che risolve il
problema dell'uovo e della gallina: un database nuovo non ha nessuno che possa
promuovere qualcun altro.

### Token: primitive / semantic / component

I tre livelli del design system.
**Primitive** = valore grezzo (`--color-brand-600`), lo conosce solo
`semantic.css`.
**Semantic** = significato (`--color-primary`, `--radius-control`), l'unico
livello che un tema ridefinisce e l'unico che le app usano.
**Component** = l'eccezione di un singolo componente (`--card-radius`).

Rebrandizzare un progetto = cambiare **solo** i semantic.

### Server state vs client state

**Server state**: viene dall'API, può cambiare senza che l'utente faccia nulla
→ TanStack Query.
**Client state**: esiste solo nel browser (UI, preferenze, sessione) → Pinia.
Non si mescolano mai.

### SDK

`packages/sdk`, **generato** da OpenAPI con `node scripts/generate-sdk.mjs`.
Non si modifica a mano e non si aggira: il frontend non costruisce URL.

### Feature service

Il file `features/<nome>/api/<nome>.api.ts`: l'unico punto della feature che sa
quali operazioni dell'SDK usa. Componenti e composable non chiamano l'SDK.

### `operationId`

Il nome del metodo nell'SDK, impostato da `WithName(...)` sull'endpoint.
È **API pubblica**: rinominare l'handler C# non lo cambia, cambiare `WithName`
sì.

### Fallback policy

La regola del host per cui **ogni endpoint richiede autenticazione** salvo
`AllowAnonymous()` esplicito. Un endpoint nasce protetto: non c'è un momento in
cui «va protetto poi».

### Audience (realtime)

Chi riceve un evento live: `ForUser`, `ForGroup`, `Everyone`. Si risolve **solo
lato server** — un client non può chiedere di ricevere il traffico di un
gruppo. Un'audience vuota non raggiunge nessuno, mai tutti.

### Outbox

Coda persistente per il lavoro differito (le email): si salva prima, si invia
poi. Evita che un fallimento del trasporto faccia fallire l'operazione di
dominio che era già riuscita.

### `Modules:AutoMigrate`

Interruttore che applica migrations e seeding all'avvio. **Vero in Development,
falso altrove**: in produzione le migrations sono uno step esplicito di
release, perché nessuna istanza deve modificare lo schema come effetto
collaterale del boot.

### Enterprise Feature

Capacità opt-in, **disattivata di default**: multi-tenant, feature flag, cache
distribuita, observability. Vivono nel host e in `Application/Abstractions`.

### Feature flag ≠ permesso

Un **feature flag** decide cosa esiste in questo deployment. Un **permesso**
decide cosa può fare questo utente. Confonderli produce controlli di sicurezza
che si possono disattivare da configurazione.

### Design system

`packages/ui`: l'unico punto di personalizzazione visiva. Se per ottenere un
risultato devi modificare un componente, quel componente ha un **buco di
tokenizzazione** — si sistema nel framework, non nel progetto.

### Template (questo repository)

Non è una dipendenza: `scripts/create-project.mjs` genera un progetto che
possiede la propria copia del codice. Nessun aggiornamento upstream può
romperlo, e ogni progetto può cambiare qualsiasi cosa, framework compreso.
