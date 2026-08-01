# Contribuire un widget alla dashboard

**Fonte normativa**: [`modules/dashboard/README.md`](../../modules/dashboard/README.md)
**Esempio nel codice**: `modules/users/backend/Features/Dashboard/UsersWidgetProvider.cs` ·
`modules/audit/backend/Features/Dashboard/AuditWidgetProvider.cs`

## Quando serve

Quando il tuo modulo ha un numero o un elenco breve che vale la pena vedere
appena si entra nell'applicazione.

**Quando NON serve**: se il dato richiede spiegazione, filtri o interazione.
La dashboard è una vetrina, non una pagina: se serve una tabella, la tile deve
diventare un link a quella tabella.

## Il principio

Il modulo Dashboard non conosce il tuo modulo. Chiede al container tutte le
implementazioni di `IDashboardWidgetProvider` e rende quello che arriva.
Nessuno referenzia il progetto Dashboard: l'astrazione sta in
`Application/Abstractions`, che il tuo modulo già referenzia.

## Procedura

1. **Implementa il provider** — `modules/<nome>/backend/Features/Dashboard/`

   ```csharp
   internal sealed class OrdersWidgetProvider(IOrderRepository orders, ICurrentUser user)
       : IDashboardWidgetProvider
   {
       public async Task<IReadOnlyList<DashboardWidget>> GetWidgetsAsync(CancellationToken ct)
       {
           // Non permesso = risultato VUOTO, mai un'eccezione.
           if (!user.Permissions.Contains(Permissions.Orders.Read)) return [];

           return
           [
               new DashboardWidget(
                   "orders.open",
                   "Open orders",
                   Value: (await orders.CountOpenAsync(ct)).ToString(CultureInfo.InvariantCulture),
                   Caption: "awaiting fulfilment",
                   Link: "/orders",
                   Order: 30),
           ];
       }
   }
   ```

2. **Registralo nel tuo modulo**, non altrove:

   ```csharp
   services.AddScoped<IDashboardWidgetProvider, Features.Dashboard.OrdersWidgetProvider>();
   ```

3. **Controlla tu i permessi.** Solo il tuo modulo sa cosa rivela la sua tile.
   L'endpoint della dashboard richiede soltanto l'autenticazione, e la pagina
   non dichiara permessi: un utente che ha diritto a **una sola** tile deve
   vedere la dashboard, non una pagina di errore.

4. **Scegli il tipo e l'ordine**

   | `Kind` | Resa                            | Campi usati                |
   | ------ | ------------------------------- | -------------------------- |
   | `Stat` | Una cifra grande con didascalia | `Value`, `Caption`, `Link` |
   | `List` | Poche voci recenti              | `Items`                    |

   `Order` ordina la griglia (più basso = prima); a parità vince il titolo,
   così il layout è stabile fra una richiesta e l'altra. L'`Id` va prefissato
   col nome del modulo (`orders.open`) per non collidere con nessuno.

5. **Frontend: niente da fare.** `DashboardGrid.vue` non ha un elenco di widget
   noti, fa dispatch sul `kind`. La tile compare da sola.

## Se il tuo provider fallisce

Viene loggato e la sua tile sparisce; le altre restano. Una dashboard parziale
è molto più utile di una pagina di errore. Non «aiutare» il meccanismo
restituendo dati finti in caso di errore: una cifra sbagliata è peggio di una
tile assente.

## Come si verifica

```bash
dotnet test
```

E a mano, con due account diversi — è l'unico modo di vedere il filtro dei
permessi funzionare:

```bash
curl -s http://localhost:5080/api/dashboard/widgets -H "Authorization: Bearer <token>"
```

## Errori tipici

**La tile non compare.** Il provider non è registrato, oppure il controllo di
permesso ritorna sempre `[]`: verifica cosa contiene davvero
`ICurrentUser.Permissions` con `/api/authorization/me`.

**La tile compare a chi non dovrebbe vederla.** Manca il controllo: l'endpoint
non ne fa nessuno per te, per costruzione.

**Hai lanciato un'eccezione per «non autorizzato».** Non fa cadere la
dashboard, ma sporca i log a ogni caricamento di ogni utente senza quel
permesso, cioè continuamente.

**Numeri che «ballano» nella griglia.** Il valore va formattato con
`CultureInfo.InvariantCulture` e la resa usa cifre tabellari: passare da 9 a 10
non deve spostare il layout.

## Correlati

- [Far parlare due moduli](module-to-module.md)
- [Aggiungere un permesso](add-permission.md)
