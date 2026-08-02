# Rivedere il codice di qualcun altro

**Fonte normativa**: [`CLAUDE.md`](../../CLAUDE.md) → «Regole architetturali
non negoziabili» e «Standard di Codifica»

## Cosa non serve guardare

Formattazione, ordine degli import, stile: li impongono già eslint, prettier,
gli analyzer .NET e gli hook. Se lo verifica una macchina, non è materia da
review.

## Cosa guardare, in ordine di costo dell'errore

Sono ordinati per quanto costa accorgersene tardi. Le prime tre non si scoprono
mai da sole: nessun test fallisce.

### 1. Confini fra moduli

- Il modulo A referenzia il progetto `backend` del modulo B? **Blocca.** I
  contratti stanno in `shared/` → [Far parlare due moduli](module-to-module.md).
- C'è una foreign key verso lo schema di un altro modulo? **Blocca.**
- Il subscriber di un evento è facoltativo? Disabilitando il suo modulo,
  l'operazione originale deve continuare a funzionare.

### 2. Sicurezza

- Ogni endpoint nuovo ha `RequirePermission(...)` oppure un `AllowAnonymous()`
  **con un commento che lo motiva**.
- Il controllo è su un **permesso**, mai su un nome di ruolo.
- Un'entity EF esce dall'API? Verso il client viaggiano solo DTO.
- Un messaggio d'errore rivela dettagli interni? Il messaggio arriva al client.
- Un'audience realtime è più larga del necessario? `ForGroup("role:…")`
  attraversa i tenant.

### 3. Il contratto pubblico

- Un campo rimosso o riusato in un evento `shared/` o in un DTO: è
  **breaking** per moduli che chi scrive potrebbe non conoscere.
- L'handler restituisce il tipo concreto (`Ok<T>`) e non `IResult`? Con
  `IResult` l'SDK esce senza tipi e nessuno se ne accorge subito.
- `WithName(...)` è cambiato? È il nome del metodo nell'SDK, cioè API pubblica.
- Contratto cambiato → SDK rigenerato e **entrambi** i file generati committati.

### 4. Stato e catena, lato frontend

- Dati del server dentro Pinia? → [Dove tenere lo stato](state.md).
- Un componente o un composable che importa l'SDK direttamente, saltando il
  feature service.
- Dopo una mutation si invalida (non si applica una patch alla cache).

### 5. Design system e responsive

- Valori letterali nel markup (`#3b82f6`, `16px`, `rounded-md`,
  `duration-300`): sono bug, non scorciatoie.
- Token nuovo → documentato in `docs/design-system.md` **nello stesso commit**.
- Il focus visibile è ancora lì.
- Informazione affidata al solo colore, senza testo o icona.
- Pagina nuova: c'è evidenza che sia stata guardata a 375px?

### 6. Test

- Non «ci sono test», ma: **testano il comportamento o l'implementazione?**
  Un test che verifica che un composable ne chiami un altro diventa rosso al
  primo refactor legittimo.
- Il caso di errore e il caso vuoto sono coperti, non solo quello felice.

### 7. Documentazione

- Ogni tipo e metodo pubblico ha il suo `<summary>` / JSDoc.
- I commenti inline spiegano **il perché**, mai il cosa.
- Una decisione non ovvia è motivata da qualche parte — nel README del modulo
  se riguarda il modulo.

## Come si scrive un commento di review

Distingui ciò che blocca da ciò che è un'opinione. «Questo va cambiato perché
rompe X» e «io l'avrei scritto così» meritano parole diverse, e chi legge deve
poterle distinguere senza chiedere.

Se il problema è che una regola non era chiara, la correzione non finisce nella
PR: aggiorna `docs/` o la pattern page, così la prossima persona non ci
inciampa.

## Quando approvare

Quando l'insieme rende il repository migliore di come l'hai trovato. Non quando
è come l'avresti scritto tu.

## Correlati

- [Aprire una PR come si deve](git-workflow.md)
- [Far parlare due moduli](module-to-module.md)
- [Scegliere il token giusto](use-design-tokens.md)
