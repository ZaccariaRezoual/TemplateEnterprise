# Nascondere ciò che l'utente non può fare

**Fonte normativa**: [`modules/authorization/README.md`](../../modules/authorization/README.md) ·
[`CLAUDE.md`](../../CLAUDE.md) → «Autorizzazione su permessi, mai su ruoli»
**Esempio nel codice**:
`modules/authorization/frontend/src/composables/usePermissions.ts` ·
`modules/authorization/frontend/src/directives/vCan.ts`

## Quando serve

Quando un'azione o una schermata non è disponibile per tutti: meglio non
mostrarla che mostrarla e rifiutarla.

**Quando NON serve**: per proteggere qualcosa. Questo non protegge niente.

## La cosa da capire prima di tutto

> **Il confine di sicurezza è l'API.** I controlli lato client sono
> presentazione.

Nascondere un pulsante evita all'utente di sbattere contro un 403. Non impedisce
nulla a chi apre la console del browser. Se l'endpoint non ha
`RequirePermission`, il dato è pubblico — indipendentemente da quanto sia
curata la UI.

Corollario pratico: **non nascondere mai qualcosa lato client come unica
misura**. Prima l'endpoint, poi la UI.

## I tre strumenti

### 1. `v-can` — per un elemento

```vue
<button v-can="'users.write'">Edit</button>
<button v-can="['users.write', 'users.delete']">Manage</button>
<!-- uno qualsiasi -->
```

**Rimuove** l'elemento dal DOM, non lo nasconde: un elemento solo invisibile
resta nell'albero di accessibilità e raggiungibile da tastiera, il che
trasforma un'azione nascosta in un'azione confusa.

### 2. `usePermissions()` — per la logica

```ts
const { can } = usePermissions();
```

Quando la decisione non è «mostra o no» ma riguarda quale contenuto costruire:

```ts
const columns = computed(() => (can("users.write") ? editableColumns : readOnlyColumns));
```

### 3. `meta.permissions` sulla route — per una pagina intera

```ts
meta: { title: "Users", requiresAuth: true, permissions: ["users.read"] }
```

La guard del modulo Authorization la applica e reindirizza alla pagina
`/forbidden`. Il requisito sta accanto alla route che protegge, non in una
tabella centrale che nessuno aggiorna.

## Permessi, mai ruoli

Non scrivere mai `roles.includes("Admin")`. I clienti riorganizzano i ruoli e
ne creano di propri; i permessi sono il contratto stabile. Lo stesso vale lato
server.

## Attenzione: dati mancanti ≠ permesso mancante

Se l'utente non ha il permesso, mostrare una tabella vuota è una **bugia**:
sembra «non ci sono utenti». Mostra un messaggio esplicito. È il motivo per cui
`UsersPage` distingue i due casi, e c'è un test che lo verifica.

## Come si verifica

```bash
pnpm test
pnpm --filter @enterprise/web test:e2e   # e2e/permissions.spec.ts
```

E la prova che conta davvero, perché è quella che un utente malintenzionato
farebbe comunque:

```bash
curl -i http://localhost:5080/api/<risorsa> -H "Authorization: Bearer <token-senza-permesso>"
# deve essere 403, non 200
```

## Errori tipici

**Il pulsante è nascosto ma l'endpoint risponde 200.** Il caso grave: hai messo
il controllo solo dove non conta.

**`can()` ritorna sempre `false` appena dopo il login.** I permessi si caricano
in modo asincrono: usa `isLoaded` prima di trarre conclusioni, o mostra uno
skeleton.

**Un permesso appena assegnato non ha effetto.** I permessi viaggiano dentro
l'access token: diventano effettivi al refresh successivo (max 15 minuti). È
una scelta — check stateless — non un bug.

**Hai usato `v-if="can(...)"` su un contenitore enorme.** Funziona, ma valuta
`meta.permissions` sulla route: se l'intera pagina non è accessibile, meglio un
redirect che una pagina vuota.

**Tabella vuota al posto di «non hai il permesso».** Vedi sopra: sono due
informazioni diverse.

## Correlati

- [Aggiungere un permesso](add-permission.md)
- [Aggiungere una feature frontend](add-feature.md)
