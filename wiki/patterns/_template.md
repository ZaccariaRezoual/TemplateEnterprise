# <Attività all'infinito, es. «Aggiungere un endpoint»>

> Template per una nuova pattern page. Copia questo file, rinominalo in
> `kebab-case.md`, cancella queste due righe e riempi le sezioni.
> Le sezioni ci sono **tutte**, sempre, in quest'ordine: una wiki in cui ogni
> pagina è organizzata a modo suo si legge come una raccolta di appunti.

**Fonte normativa**: `docs/<file>.md` §… · `CLAUDE.md` §…
**Esempio nel codice**: `modules/<modulo>/…`

## Quando serve

Una o due frasi su quando si applica questo pattern.

E soprattutto **quando NON si applica**, con l'alternativa da preferire. È la
parte che evita il danno più costoso: applicare correttamente il pattern
sbagliato.

## Procedura

Passi numerati, nell'ordine in cui si eseguono davvero.

1. **<Cosa si fa>** — file: `percorso/reale/File.cs`

   ```csharp
   // Snippet COPIATO dal repository, non scritto a memoria.
   ```

2. **<Passo successivo>** …

Se un passo esiste per un motivo non ovvio, dillo in una riga: chi legge deve
poter decidere se il motivo vale ancora nel suo caso.

## Come si verifica

Il comando che dimostra che è fatto bene, non l'impressione che lo sia.

```bash
dotnet build && dotnet test
```

## Errori tipici

**<Sintomo che si vede davvero>** — la causa, e la correzione.

Non elencare errori teorici: solo quelli in cui qualcuno è già inciampato. Se
il sintomo non suggerisce la causa, questa sezione vale da sola l'intera
pagina.

## Correlati

- [Aggiungere un endpoint](add-endpoint.md) ← sostituisci con i link veri
