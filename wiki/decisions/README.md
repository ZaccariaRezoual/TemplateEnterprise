# Decisioni architetturali (ADR)

Le decisioni che hanno **alternative ragionevoli** e che qualcuno, fra un anno,
proverà a ribaltare senza sapere cosa costò sceglierle.

## Cosa va qui, e cosa no

| Va qui                                        | Va altrove                                 |
| --------------------------------------------- | ------------------------------------------ |
| «Perché un hub SignalR e non uno per feature» | Come si rende live un evento → `patterns/` |
| «Perché i permessi nel token e non a DB»      | Quali permessi esistono → `docs/`          |
| «Perché il template si copia, non si importa» | Come si crea un progetto → `patterns/`     |

Una decisione con una sola opzione sensata non è una ADR: è una regola, e le
regole stanno in `docs/`.

## Dove sono, oggi

La maggior parte delle decisioni di questo framework è già motivata **dove
serve leggerla**, nei README dei moduli, sotto «Design decisions»:

- [`modules/realtime`](../../modules/realtime/README.md) — un hub invece di
  molti; audience risolte lato server; eventi non replayati; riconnessione con
  backoff proprio
- [`modules/authorization`](../../modules/authorization/README.md) — permessi
  nel token invece che a database; assenza di regole di deny
- [`modules/dashboard`](../../modules/dashboard/README.md) — permessi
  controllati dal contributore; un provider che fallisce non fa cadere la pagina
- [`modules/email`](../../modules/email/README.md) — outbox invece di invio
  inline; trasporto sostituibile
- [`modules/auth`](../../modules/auth/README.md) — access token in memoria,
  refresh in cookie httpOnly; seeding del bootstrap admin su `ApplicationStarted`
- [`docs/create-project.md`](../../docs/create-project.md) — il template si
  copia, non si importa; il modulo Demo non si rimuove automaticamente
- [`plans/wiki.md`](../../plans/wiki.md) §2 — il confine `docs/` ↔ `wiki/`

**Non le duplichiamo qui.** Una decisione descritta in due posti diverge, e la
copia sbagliata è sempre quella che qualcuno legge.

Questa cartella serve per le decisioni **trasversali**, che non appartengono a
nessun modulo: quando ne prendi una, scrivila qui.

## Formato

Un file per decisione, `NNN-titolo-in-kebab-case.md`:

```markdown
# NNN — Titolo della decisione

**Stato**: proposta · accettata · sostituita da NNN
**Data**: AAAA-MM-GG

## Contesto

Il problema, e i vincoli reali. Cosa era vero quando si è deciso.

## Decisione

Cosa si è scelto, all'indicativo presente.

## Alternative considerate

Ognuna con il motivo dello scarto. **È la sezione che conta**: senza, la ADR
non impedisce a nessuno di riproporre l'opzione già scartata.

## Conseguenze

Cosa diventa facile, cosa diventa difficile, cosa siamo obbligati a fare per
sempre.
```

Una ADR **non si modifica** quando la decisione cambia: se ne scrive una nuova
che sostituisce la vecchia, e la vecchia resta con lo stato aggiornato. Il
valore sta nel poter ricostruire _perché allora sembrava giusto_.
