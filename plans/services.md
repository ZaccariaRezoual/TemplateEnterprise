# Piano — Gestione dei servizi

> Piano operativo per il modulo **Services**: amministrazione dei servizi
> (creazione, modifica, archiviazione) e loro vetrina pubblica. Stessa logica
> di [PLAN.md](../PLAN.md): fasi ordinate per dipendenza, ognuna con un
> criterio di done verificabile.
>
> **Prerequisito del piano successivo**: gli appuntamenti
> ([appointments.md](appointments.md)) prenotano _un servizio_, e la durata
> dichiarata qui è ciò che genera gli slot. Le scelte segnate «⟶ appuntamenti»
> esistono per quel piano, non per questo.

---

## 1. Cosa serve

Oggi la voce «Servizi» del sito pubblico è **testo statico** in
`apps/web/src/site.config.ts`: cambiarla richiede un deploy, e non c'è nulla da
prenotare. Serve che i servizi siano **dati**: amministrabili da `/admin`,
pubblicati sulla vetrina, e prenotabili domani.

## 2. Dove vive

**`modules/services/`**, modulo verticale completo: backend con persistenza
propria (schema `services`), contratti pubblici in `shared/`, frontend con le
pagine di amministrazione **e** quelle pubbliche.

Le pagine pubbliche stanno nel modulo, non nel modulo Site: chi possiede il
dato possiede la sua rappresentazione. Il modulo Site resta il guscio e la
vetrina «istituzionale» (chi siamo, contatti).

### La collisione da risolvere subito: `/services`

Oggi quella rotta è del modulo Site (pagina statica). Domani è del modulo
Services (elenco dai dati). **Due rotte con lo stesso path non convivono.**

Decisione: **il composition root sceglie**. `apps/web/src/router/index.ts`
registra le rotte di Services _al posto_ di quella statica di Site quando il
modulo è installato. Un progetto che non installa Services continua ad avere la
pagina statica — che è esattamente il comportamento utile per un sito senza
prenotazioni.

Conseguenza da scrivere in `site.config.ts`: la sezione `services` resta e vale
**solo** come fallback.

## 3. Il dominio

```csharp
Service
  Id                Guid
  Title             string        obbligatorio
  Slug              string        unico, generato dal titolo, editabile
  ShortDescription  string        una riga, per la card in elenco
  Description       string        testo lungo, per la pagina di dettaglio
  DurationMinutes   int?          ⟶ appuntamenti: genera gli slot
  Price             decimal?      + Currency
  IsPublished       bool          bozza vs pubblicato
  IsBookable        bool          ⟶ appuntamenti: alcuni servizi si raccontano soltanto
  SortOrder         int           ordine in vetrina
  IsArchived        bool          vedi §6
  Images            ServiceImage[]
  CreatedAtUtc / UpdatedAtUtc

ServiceImage
  Id, ServiceId, StorageFileId, AltText (obbligatorio), SortOrder, IsCover
```

Perché questi campi e non altri:

- **`ShortDescription` separata da `Description`**: senza, l'elenco tronca il
  testo lungo a metà frase. Due campi costano una riga di form e risolvono il
  problema alla fonte.
- **`DurationMinutes` nullable**: un servizio può essere descrittivo. Ma un
  servizio **prenotabile senza durata non ha slot**, quindi la validazione dirà
  «`IsBookable` richiede `DurationMinutes`».
- **`AltText` obbligatorio**: regola di accessibilità del design system, non
  una gentilezza. Un'immagine senza alt è invisibile a chi non la vede.
- **Niente `Category`/`Tag` per ora**: una tassonomia si aggiunge quando i
  servizi sono abbastanza da non stare in una pagina. Aggiungerla prima
  significa progettare un filtro per tre elementi.

## 4. Il problema delle immagini pubbliche

**Vincolo reale, verificato**: `GET /api/files/{id}` del modulo Storage è
autenticato (fallback policy del host). Un visitatore anonimo **non può
scaricare** l'immagine di un servizio. Senza risolverlo, la vetrina non ha
immagini.

Tre strade:

| Opzione                                                                                                         | Costo                                                          | Giudizio        |
| --------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------- | --------------- |
| **Storage impara il concetto di file pubblico** (`Visibility` sull'upload, endpoint anonimo per quelli marcati) | Tocca un modulo del framework                                  | **Consigliata** |
| Services espone un proprio endpoint anonimo che rilegge dal provider                                            | Duplica la logica di streaming, due strade per servire un file | No              |
| Le immagini vanno su un CDN esterno                                                                             | Dipendenza esterna in un template                              | No              |

Consigliata la prima: **ogni progetto costruito su questo template avrà prima o
poi un'immagine pubblica**, quindi la lacuna è del framework, non di questo
modulo. È lo stesso ragionamento del «buco di tokenizzazione» del design
system.

Regole per quell'aggiunta: la visibilità si decide **all'upload** ed è
esplicita; il default resta **privato** (un default pubblico trasforma una
distrazione in una fuga di dati); l'endpoint pubblico serve solo i file marcati
e non accetta id arbitrari.

## 5. API

**Pubbliche** (anonime, solo pubblicati e non archiviati):

| Metodo | Rotta                  | Nota                            |
| ------ | ---------------------- | ------------------------------- |
| GET    | `/api/services`        | Elenco per la vetrina, ordinato |
| GET    | `/api/services/{slug}` | Dettaglio                       |

**Amministrative** (`RequirePermission`):

| Metodo | Rotta                                       | Permesso          |
| ------ | ------------------------------------------- | ----------------- |
| GET    | `/api/admin/services`                       | `services.read`   |
| POST   | `/api/admin/services`                       | `services.write`  |
| PUT    | `/api/admin/services/{id}`                  | `services.write`  |
| POST   | `/api/admin/services/{id}/images`           | `services.write`  |
| DELETE | `/api/admin/services/{id}/images/{imageId}` | `services.write`  |
| POST   | `/api/admin/services/{id}/archive`          | `services.delete` |

Due rotte separate per pubblico e amministrazione, non un filtro sullo stesso
endpoint: il pubblico non deve mai poter chiedere le bozze, e un parametro
`?includeDrafts=true` è esattamente il tipo di cosa che qualcuno dimentica di
controllare.

Permessi nuovi nel catalogo: `services.read`, `services.write`,
`services.delete`.

## 6. Archiviare, non cancellare

⟶ appuntamenti: un appuntamento referenzia il servizio **per id**. Cancellare
un servizio prenotato lascerebbe appuntamenti che puntano al nulla, e il modulo
Services non può sapere se qualcuno lo referenzia — è la stessa ragione per cui
non ci sono foreign key fra schemi.

Quindi: `IsArchived`. Un servizio archiviato sparisce dalla vetrina e dai nuovi
appuntamenti, resta leggibile per quelli già presi. **Gli id non si riusano
mai.**

## 7. Contratti pubblici

In `modules/services/shared/Events/`:

- `ServicePublished(ServiceId, Title, Slug, DurationMinutes)`
- `ServiceUpdated(ServiceId, Title, Slug, DurationMinutes, IsBookable)`
- `ServiceArchived(ServiceId)`

⟶ appuntamenti: da questi si costruisce la **proiezione** del titolo e della
durata, così la pagina di prenotazione e il calendario mostrano il nome del
servizio senza interrogare un altro modulo. `ServiceArchived` è ciò che ferma
le nuove prenotazioni.

## 8. Frontend

### Amministrazione (`/admin/services`)

- Elenco con stato (bozza/pubblicato/archiviato), ricerca, ordinamento.
- Form di creazione/modifica: titolo, slug (precompilato, modificabile),
  descrizioni, durata, prezzo, flag, immagini con caricamento e alt text.
- **Lo slug avvisa quando cambia** su un servizio già pubblicato: i link
  esistenti si rompono. Meglio un avviso che una regola che impedisce.
- Superficie densa → [docs/design-system-admin.md](../docs/design-system-admin.md).

### Vetrina (`/services`, `/services/{slug}`)

- Elenco: griglia di card con immagine di copertina, titolo, riga breve,
  durata e prezzo se presenti.
- Dettaglio: galleria, descrizione, dati pratici, **una sola CTA**.
- ⟶ appuntamenti: quella CTA sarà «Prenota». Fino ad allora punta a
  `/contact`, e cambierà in una riga.
- Superficie vetrina → [docs/design-system-public.md](../docs/design-system-public.md).

## 9. SEO delle pagine dinamiche — una lacuna da colmare

Oggi `applySeo` legge `meta.title` e `meta.description` **statici dalla rotta**.
Una pagina di dettaglio ha titolo e descrizione **nei dati**: servono meta
impostabili a runtime.

E il prerender (`apps/web/prerender.config.mjs`) elenca rotte fisse: gli slug
non si conoscono a build time.

Due aggiunte, entrambe piccole:

1. `setSeo({ title, description })` chiamabile da una pagina quando i dati
   arrivano — stessa implementazione di `applySeo`, invocata a mano.
2. Il prerender **chiede gli slug all'API** quando è raggiungibile; se non lo
   è, prerendera le rotte statiche e lo dice, invece di fallire.

Senza queste, ogni servizio condiviso su Slack mostra lo stesso titolo
generico.

## 10. Fasi

### Fase 0 — Dominio e API amministrative

Modulo, schema `services`, migrations, entity, permessi, CRUD amministrativo,
contratti pubblici. Niente immagini, niente vetrina.

✅ **Done quando**: si crea, modifica e archivia un servizio via API con i
permessi giusti; un utente senza `services.write` riceve 403; `dotnet test`
verde.

### Fase 1 — Immagini pubbliche

La modifica a Storage (§4), poi l'associazione immagini al servizio con alt
text e copertina.

✅ **Done quando**: un'immagine marcata pubblica si scarica **senza token**, una
privata continua a rispondere 401, e c'è un test per entrambe.

### Fase 2 — Amministrazione

Pagine `/admin/services`: elenco, form, caricamento immagini, pubblicazione.

✅ **Done quando**: si porta un servizio da inesistente a pubblicato senza
toccare il database, e la voce compare nella navigazione solo con
`services.read`.

### Fase 3 — Vetrina

Rotte pubbliche del modulo, sostituzione della pagina statica, elenco e
dettaglio.

✅ **Done quando**: i servizi pubblicati si vedono da anonimo, le bozze no,
verificato a 375px; disinstallando il modulo torna la pagina statica.

### Fase 4 — SEO dinamico

`setSeo` runtime + prerender che scopre gli slug (§9).

✅ **Done quando**: `dist/services/<slug>/index.html` contiene titolo,
descrizione e testo del servizio senza eseguire JavaScript.

### Fase 5 — Composizione e documentazione

Widget di dashboard («servizi pubblicati»), `modules/services/README.md`,
pattern page se emerge qualcosa di non ovvio, aggiornamento di
`create-project.md`.

✅ **Done quando**: aggiungere un servizio si fa seguendo solo la
documentazione.

## 11. Test

| Livello     | Cosa                                                                                                     |
| ----------- | -------------------------------------------------------------------------------------------------------- |
| Unit        | Generazione e unicità dello slug; validazione «bookable ⇒ durata»                                        |
| Integration | Le bozze non escono dall'endpoint pubblico; 403 senza permesso; immagine pubblica scaricabile da anonimo |
| e2e         | Ciclo completo: crea → pubblica → visibile in vetrina; 375px                                             |

Il primo test d'integrazione è il più importante: **una bozza che finisce in
vetrina è il fallimento che questo modulo deve rendere impossibile**.

## 12. Decisioni prese

1. **Prezzo**: campo presente e nullable, mostrato **solo quando valorizzato**.
   Costa una riga di form e una condizione nel markup, e un progetto che non lo
   usa non lo vede. Toglierlo dopo, invece, è una migration.
2. **Riordino in amministrazione**: campo numerico. Il trascinamento si aggiunge
   se l'elenco supera la decina — prima di allora è lavoro speso per riordinare
   cinque righe.
3. **Immagini pubbliche**: si procede con la modifica al modulo Storage (§4),
   perché la lacuna è del framework: ogni progetto avrà prima o poi
   un'immagine pubblica. Default **privato**, visibilità esplicita all'upload.

⟶ Conseguenza delle decisioni sugli appuntamenti
([appointments.md](appointments.md) §14): un servizio prenotabile richiede
`DurationMinutes`, e la CTA del dettaglio porterà a `/book/{slug}`.
