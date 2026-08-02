# Wiki di sviluppo

Questa wiki risponde a una domanda sola: **«devo fare X, come si fa qui
dentro?»**.

Non spiega perché il framework è fatto così — per quello ci sono `docs/` e i
README dei moduli. Mostra la procedura, nell'ordine in cui la si esegue, con i
file veri del repository.

## Da dove partire

| Se sei…                        | Vai a                                                                           |
| ------------------------------ | ------------------------------------------------------------------------------- |
| al primo giorno sul progetto   | [Primo giorno](getting-started/day-one.md)                                      |
| davanti a un'attività concreta | [patterns/](patterns/README.md)                                                 |
| bloccato da un errore strano   | [Errori che non hanno senso](patterns/troubleshooting.md)                       |
| perso fra le cartelle          | [Giro del repository](getting-started/tour.md)                                  |
| davanti a una parola oscura    | [Glossario](getting-started/glossary.md)                                        |
| curioso del «perché»           | [decisions/](decisions/README.md), [`docs/`](../docs/) e i README in `modules/` |

## Il confine con `docs/` — leggilo prima di scrivere qui

> **La wiki non ridefinisce nulla: linka.**

- **`docs/` è normativo.** Dice cosa è vero e cosa è vietato. In caso di
  conflitto fra una pagina di questa wiki e `docs/`, **vince `docs/`**, e la
  pagina va corretta.
- **`wiki/` è operativo.** Dice come si fa, nell'ordine in cui si fa.

Ogni pattern apre con la sua **fonte normativa**. Se ti accorgi di stare
_spiegando una regola_ invece che applicarla, quella regola appartiene a
`docs/`: scrivila lì e linkala da qui.

Il motivo è lo stesso che `CLAUDE.md` usa per il design system: due copie
divergono sempre. Una wiki che riscrive le regole diventa, nel giro di qualche
mese, la versione sbagliata che però tutti leggono.

**Verifica del confine**: cancellando una pagina di questa wiki non si deve
perdere nessuna informazione normativa — solo tempo.

## Com'è organizzata

| Cartella           | Cosa contiene                                                                                                                        |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------ |
| `getting-started/` | [primo giorno](getting-started/day-one.md), [giro del repository](getting-started/tour.md), [glossario](getting-started/glossary.md) |
| `patterns/`        | [il cuore](patterns/README.md): una pagina per attività                                                                              |
| `decisions/`       | [ADR](decisions/README.md): decisioni con alternative ragionevoli                                                                    |

## Le altre fonti, e a chi parlano

| Dove               | Cosa contiene                              | Quando aprirlo                        |
| ------------------ | ------------------------------------------ | ------------------------------------- |
| `wiki/`            | Procedure operative                        | Devi fare qualcosa                    |
| `docs/`            | Regole normative                           | Devi sapere cosa è permesso           |
| `modules/*/README` | Contratti e decisioni di un modulo         | Stai lavorando su quel modulo         |
| `Struttura.md`     | Visione e architettura complessiva         | Vuoi il quadro d'insieme              |
| `PLAN.md`          | Piano di costruzione del framework, a fasi | Vuoi sapere cosa esiste e cosa no     |
| `CLAUDE.md`        | Contratto operativo per le sessioni AI     | Lavori con Claude Code su questo repo |

## Come si contribuisce

1. Copia [`patterns/_template.md`](patterns/_template.md) e riempilo.
2. **Gli snippet si copiano dal repository**, non si scrivono a memoria. Uno
   snippet che non compila insegna a scrivere codice che non compila.
3. La sezione **«Errori tipici» non è opzionale**: è quella che fa risparmiare
   davvero tempo, ed è l'unica informazione che non si può ricavare leggendo il
   codice.
4. Aggiungi la pagina all'indice in [`patterns/README.md`](patterns/README.md).
5. **Stessa PR del codice.** Una pagina aggiornata «dopo» è una pagina
   aggiornata mai.
6. Controlla i link prima di aprire la PR — la CI fallisce se uno è rotto:

   ```bash
   pnpm check:links
   ```

Lingua: italiano. Gli identificatori restano in inglese come nel codice —
si scrive «il validator di `UpdateUserCommand`», mai la sua traduzione, perché
un identificatore tradotto non è più cercabile.
