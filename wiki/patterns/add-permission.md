# Aggiungere un permesso

**Fonte normativa**: [`modules/authorization/README.md`](../../modules/authorization/README.md) ·
[`CLAUDE.md`](../../CLAUDE.md) → «Autorizzazione su permessi, mai su ruoli»
**Esempio nel codice**: `modules/authorization/backend/Domain/Permissions.cs`

## Quando serve

Quando una nuova operazione non deve essere accessibile a tutti gli utenti
autenticati.

**Quando NON serve**: se la risorsa è di proprietà del chiamante (le _sue_
notifiche, il _suo_ profilo). Lì non serve un permesso: serve filtrare per
`ICurrentUser.UserId`. Un permesso `notifications.read.own` sarebbe un permesso
che hanno tutti, cioè nessun permesso con un costo di manutenzione.

## Procedura

1. **Aggiungi la costante al catalogo** —
   `modules/authorization/backend/Domain/Permissions.cs`

   Il nome è sempre `resource.action`, minuscolo:

   ```csharp
   /// <summary>Permissions over the audit trail.</summary>
   public static class Audit
   {
       /// <summary>Read the audit trail.</summary>
       public const string Read = "audit.read";
   }
   ```

2. **Aggiungilo a `Permissions.All`** — nello stesso file

   ```csharp
   public static IReadOnlyList<string> All { get; } =
       [
           Users.Read,
           Users.Write,
           // …
           Audit.Read,
       ];
   ```

   Dimenticarlo è l'errore più comune: il permesso esiste, l'endpoint lo
   pretende, ma il ruolo `Admin` non lo riceve — perché il seeder costruisce
   `Admin` proprio da `All`. Risultato: 403 per chiunque, amministratore
   compreso.

3. **Proteggi l'endpoint** — nel `MapEndpoints` del modulo

   ```csharp
   .RequirePermission(Permissions.Audit.Read)
   ```

   `RequirePermission` sta in `Modules.Abstractions`, quindi è disponibile a
   qualunque modulo, e il `PermissionPolicyProvider` del host genera la policy
   su richiesta: non c'è nessun elenco centrale da aggiornare.

4. **Riavvia.** Il seeder di Authorization è idempotente e ri-sincronizza i
   permessi dei ruoli built-in a ogni avvio: il permesso nuovo raggiunge anche
   i database esistenti.

5. **Lato frontend**, nascondi ciò che l'utente non può fare:

   ```ts
   const { can } = usePermissions();
   ```

   ```vue
   <button v-can="'audit.read'">…</button>
   ```

   Questo è **presentazione, non sicurezza**: il confine di sicurezza è l'API.
   Un controllo lato client evita all'utente di trovare un 403; non impedisce
   niente a chi sa usare `curl`.

## Ruoli: non toccarli per questo

Un permesso nuovo non richiede un ruolo nuovo. I ruoli built-in sono due
(`Admin`, `BasicUser`) e sono di proprietà del catalogo in codice; i ruoli
custom li crea il cliente dall'interfaccia.

E non scrivere mai un check su un nome di ruolo: i clienti riorganizzano i
ruoli, i permessi restano.

## Come si verifica

```bash
dotnet build && dotnet test
```

Poi la prova end-to-end, che è quella che conta:

```bash
# con un account senza il permesso → 403
curl -i http://localhost:5080/api/<risorsa> -H "Authorization: Bearer <token>"
```

E controlla che l'admin lo abbia davvero:

```bash
curl -s http://localhost:5080/api/authorization/me -H "Authorization: Bearer <token-admin>"
```

## Errori tipici

**403 anche da amministratore.** La costante non è in `Permissions.All`.

**Il permesso non compare in `/me` dopo averlo aggiunto a un ruolo.** I
permessi viaggiano **dentro l'access token**: diventano effettivi al refresh
successivo (al massimo 15 minuti), non immediatamente. È una scelta — check
stateless, zero round-trip — non un bug.

**Hai scritto un check su `roles.Contains("Admin")`.** Funziona finché un
cliente non rinomina il ruolo o ne crea uno equivalente. Usa il permesso.

## Correlati

- [Aggiungere un endpoint](add-endpoint.md)
- [Errori](errors.md)
