# Rigenerare l'SDK

**Fonte normativa**: [`CLAUDE.md`](../../CLAUDE.md) → «Il frontend consuma solo
l'SDK»
**Esempio nel codice**: `scripts/generate-sdk.mjs` · `packages/sdk/`

## Quando serve

Ogni volta che cambia un contratto dell'API: un endpoint nuovo, un campo
aggiunto a un DTO, un tipo di ritorno modificato.

**Quando NON serve**: se hai cambiato solo la logica interna di un handler. Il
contratto è ciò che finisce nel documento OpenAPI; il resto non riguarda il
client.

## La catena

```
dotnet build → apps/api/openapi/v1.json → openapi-typescript → packages/sdk/src/generated/schema.ts
```

L'OpenAPI è generato **a build time**: l'API non deve essere in esecuzione.

## Procedura

1. **Cambia il contratto** lato backend → [Aggiungere un endpoint](add-endpoint.md)

2. **Rigenera**

   ```bash
   node scripts/generate-sdk.mjs
   ```

3. **Committa entrambi i file generati**: `apps/api/openapi/v1.json` e
   `packages/sdk/src/generated/schema.ts`.

   Il primo serve a rendere la modifica di contratto **visibile nel diff della
   PR**: è l'unico punto in cui un revisore vede che l'API pubblica è cambiata.

4. **Aggiorna il feature service** se i tipi sono cambiati. Il type-check te lo
   dirà: è il motivo per cui l'SDK è generato e non scritto a mano.

## In CI

```bash
node scripts/generate-sdk.mjs --check
```

Fallisce se il generato committato è disallineato dal contratto. Non è
pignoleria: un SDK disallineato compila e sbaglia a runtime, che è il modo
peggiore di sbagliare.

## Cosa NON fare mai

- **Modificare a mano** `packages/sdk/src/generated/`: viene sovrascritto.
- **Costruire un URL** in una feature. Se un path non esiste nell'SDK, il
  problema è il backend, non il client.
- **Aggiungere un tipo «a mano» perché l'SDK non ce l'ha.** Se manca, il
  contratto non lo espone: quasi sempre l'handler dichiara `Task<IResult>`
  invece del tipo concreto.

## Come si verifica

```bash
node scripts/generate-sdk.mjs --check
pnpm -r typecheck
```

E controlla nel diff che la risposta abbia uno `$ref` a uno schema, non un
oggetto vuoto.

## Errori tipici

**La CI dice «SDK is out of date».** Hai cambiato un contratto senza
rigenerare, o hai rigenerato senza committare uno dei due file.

**Il metodo esiste nell'SDK ma la risposta è `unknown`.** L'handler backend
restituisce `IResult`. Va cambiato in `Ok<T>` (o `NoContent`,
`FileStreamHttpResult`): solo il tipo concreto porta la forma nel documento.

**Il metodo dell'SDK ha cambiato nome dopo un refactor.** È l'`operationId`,
cioè `WithName(...)`: rinominare l'handler C# non lo tocca, cambiare
`WithName` sì. Trattalo come API pubblica.

**Un enum arriva come numero.** Non dovrebbe: l'API serializza gli enum per
nome. Se succede, il tipo esce da un percorso che non passa dalle opzioni JSON
del host.

**`dotnet build` chiede il database.** La generazione avvia il host, e in
Development le migrations partono all'avvio: serve Postgres, o
`Modules:AutoMigrate=false`.

## Correlati

- [Aggiungere un endpoint](add-endpoint.md)
- [Aggiungere una feature frontend](add-feature.md)
