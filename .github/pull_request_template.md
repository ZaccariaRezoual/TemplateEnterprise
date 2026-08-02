<!--
  La checklist non è burocrazia: ogni voce nasce da qualcosa che è già andato
  storto in questo repository. Se una voce non si applica, cancellala.
  Guida completa: wiki/patterns/git-workflow.md
-->

## Cosa cambia

<!-- Una o due frasi. Il "perché" va nel messaggio di commit. -->

## Checklist

- [ ] La CI passerebbe: `pnpm lint && pnpm format:check && pnpm typecheck && pnpm test`
      e `dotnet build && dotnet test`
- [ ] **Test**: unit sempre; integration se ho toccato il backend; e2e se ho
      toccato un flusso principale
- [ ] **Contratto API cambiato** → SDK rigenerato (`node scripts/generate-sdk.mjs`)
      e **entrambi** i file generati committati
- [ ] **Documentazione aggiornata nello stesso commit**: `docs/` se è cambiata
      una regola, il README del modulo se è cambiato un contratto,
      `wiki/patterns/` se è cambiato un pattern
- [ ] **Endpoint nuovi**: `RequirePermission(...)`, oppure `AllowAnonymous()`
      con un commento che lo motiva
- [ ] **Design system**: nessun valore letterale nel markup; token nuovi
      documentati in `docs/design-system.md`
- [ ] **Responsive**: pagina nuova o modificata verificata a **375px**
- [ ] **Confini fra moduli** rispettati: nessun riferimento al progetto
      `backend` di un altro modulo, nessuna FK cross-schema
