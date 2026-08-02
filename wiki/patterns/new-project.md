# Creare un progetto nuovo dal template

**Fonte normativa**: [`docs/create-project.md`](../../docs/create-project.md) —
opzioni complete e passi post-scaffolding
**Esempio nel codice**: `scripts/create-project.mjs`

## Quando serve

Quando parte un progetto nuovo che deve basarsi sul framework.

**Quando NON serve**: per aggiungere una funzionalità a un progetto esistente.
E soprattutto: **il framework non è una dipendenza**. Il progetto generato
possiede la propria copia del codice, quindi nessun aggiornamento upstream può
romperlo — e nessun aggiornamento upstream arriva da solo.

## Procedura

```bash
node scripts/create-project.mjs --name "Acme CRM" --target ../acme-crm
```

Cosa viene rinominato, derivato da `--name`:

| Template               | Diventa       | Dove                               |
| ---------------------- | ------------- | ---------------------------------- |
| `EnterpriseFramework`  | `AcmeCrm`     | namespace .NET, assembly, percorsi |
| `@enterprise/…`        | `@acme-crm/…` | package del workspace e import     |
| `enterprise-framework` | `acme-crm`    | progetto Docker Compose, container |
| `Enterprise Framework` | `Acme CRM`    | nome prodotto nella UI             |

Vengono copiati **solo i file tracciati da git**: è ciò che tiene fuori
`node_modules`, `bin/`, `obj/` e le impostazioni locali senza mantenere qui un
secondo elenco di esclusioni che divergerebbe da `.gitignore`.

Prima di eseguirlo davvero:

```bash
node scripts/create-project.mjs --name "Acme CRM" --target ../acme-crm --dry-run
```

## Subito dopo

```bash
cd ../acme-crm
git init && git add -A && git commit -m "chore: initial commit from template"
pnpm install
dotnet build
docker compose -f docker/docker-compose.yml up -d
pnpm --filter @acme-crm/web dev
```

## Poi, nell'ordine

1. **Brand** — un foglio di stile che ridefinisce i token **semantic**,
   importato dopo `@enterprise/ui/tokens.css` (che nel progetto avrà il tuo
   scope). Nient'altro dovrebbe servire: se un componente resiste, ha un buco
   di tokenizzazione → [Scegliere il token giusto](use-design-tokens.md).
2. **Password del bootstrap admin.** Quella del template è pubblica in questo
   repository: cambiala in `Modules:Auth:BootstrapAdmin`, o disabilita il seed.
3. **Moduli di business** in `modules/`. Quelli del framework restano.
4. **Widget di dashboard** per i moduli nuovi, così la landing page parla del
   tuo prodotto → [Contribuire un widget](contribute-dashboard-widget.md).

## Il modulo Demo resta

Non viene rimosso, ed è una scelta: il test e2e del realtime usa il pulsante
«Notify me» della pagina demo come unico trigger per una notifica lato server.
Uno script che cancellasse il modulo consegnerebbe un repository con la suite
rossa.

Quando non ti serve più, la rimozione è una lista di sei punti in
[`docs/create-project.md`](../../docs/create-project.md) → «Removing the Demo
module».

## Come si verifica

Nel progetto generato:

```bash
dotnet build && pnpm -r typecheck && pnpm -r test
```

Devono passare **prima** di scrivere una riga di codice tuo. Se qualcosa è
rosso appena scaffoldato, il problema è il template, non il progetto: correggilo
nel template.

## Errori tipici

**La cartella di destinazione non è vuota.** Lo scaffolder si rifiuta, di
proposito: sovrascrivere un repository esistente non è recuperabile.

**Manca un file appena aggiunto.** Vengono copiati solo i file **tracciati**:
un file non ancora `git add`-ato non esiste per lo scaffolder.

**Il namespace generato non è valido.** Deriva da `--name`: caratteri strani
vengono ripuliti, ma un nome che inizia con una cifra viene prefissato. Se il
risultato non ti piace, cambia `--name`.

**Hai eseguito lo scaffolder dentro il progetto generato.** Non c'è: lo script
non viene copiato, proprio per impedirlo.

## Correlati

- [Scegliere il token giusto](use-design-tokens.md)
- [Creare un modulo](create-module.md)
