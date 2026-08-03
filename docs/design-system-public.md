# Design System — superficie pubblica

Come si progetta la **vetrina**: home, chi siamo, servizi, contatti.

> Questo documento **non** ridefinisce il sistema. La base — architettura dei
> token, contratto di accessibilità, convenzioni dei componenti, theming — è in
> [design-system.md](design-system.md), e in caso di conflitto vince quella.
> Qui c'è solo ciò che è specifico di questa superficie.
>
> La controparte è [design-system-admin.md](design-system-admin.md).

---

## 1. Il carattere della superficie

Direzione: **Trust & Authority, colonna singola minimale**.

| Principio          | In pratica                                            |
| ------------------ | ----------------------------------------------------- |
| Una sola colonna   | Il contenuto scorre; niente layout a mosaico          |
| Tipografia grande  | Il titolo porta il messaggio, non un'illustrazione    |
| Molto respiro      | Lo spazio vuoto è ciò che fa leggere «curato»         |
| **Una sola CTA**   | Per pagina, per schermata                             |
| Niente decorazione | Nessun gradiente, nessuna animazione fine a sé stessa |

Il visitatore di un sito B2B sta valutando se fidarsi. Fiducia si comunica con
chiarezza e ordine, non con effetti: un hero con tre pulsanti equivalenti gli
chiede di decidere prima di sapere qualcosa, e il risultato abituale è che non
decide nulla.

**Da evitare** (anti-pattern della direzione scelta): design giocoso,
gradienti viola/rosa da prodotto AI, credenziali nascoste in fondo.

## 2. Come la superficie è realizzata

`PublicLayout` marca la propria radice con `data-surface="public"`. Dentro
quel sottoalbero un blocco di `semantic.css` **ridefinisce solo token
semantic** — esattamente il meccanismo del tema scuro.

```css
[data-surface="public"] {
  --semantic-text-display: var(--font-size-primitive-display);
  --semantic-space-band: var(--space-primitive-band);
  /* … */
}
```

Conseguenza pratica: **lo stesso markup** rende come strumento di lavoro dentro
`/admin` e come brochure fuori. Una pagina non conosce la propria superficie, e
non deve.

## 3. Tipografia

Tre ruoli, non tre dimensioni. Il nome dice **a cosa serve**, così una
superficie può cambiare cosa significa «display» senza toccare una pagina.

| Utility        | Ruolo                           | Pubblica           | Privata |
| -------------- | ------------------------------- | ------------------ | ------- |
| `text-display` | Il titolo della pagina (`h1`)   | 2 → 3.25rem fluido | 1.5rem  |
| `text-heading` | Il titolo di una sezione (`h2`) | 1.5 → 2rem fluido  | 1.25rem |
| `text-title`   | Il titolo di una card (`h3`)    | 1.25 → 1.5rem      | 1rem    |

**Fluide con `clamp()`, non con i breakpoint.** Un titolo che cresce con il
viewport non ha bisogno di varianti `sm:`/`lg:` a ogni chiamata — e soprattutto
non può essere dimenticato su una pagina. Il minimo è la dimensione del
telefono; il massimo ferma la crescita prima che una riga diventi lunga tre
parole.

Il corpo del testo resta quello di base (≥16px): la vetrina non ha bisogno di
un testo più grande, ha bisogno di **righe più corte** — vedi §5.

## 4. Ritmo verticale

Una **banda** è una fascia orizzontale della pagina: hero, sezione servizi,
CTA finale. Il ritmo si decide una volta sola.

| Utility      | Pubblica | Privata |
| ------------ | -------- | ------- |
| `py-band`    | 2.5rem   | 1.5rem  |
| `py-band-lg` | 5rem     | 2.5rem  |

Si usano su `PageSection`, che è l'unico componente autorizzato a decidere la
spaziatura verticale di una banda. Una pagina che scrive `py-16` a mano
introduce la sesta spaziatura leggermente diversa dalle altre cinque.

Bande alternate: sfondo `bg-surface` su `bg-background` separa due sezioni
senza bisogno di un bordo. Non usare più di due livelli — una pagina a strisce
sembra un modulo, non una presentazione.

## 5. Misura del testo

`max-w-prose` sui paragrafi (45–75 caratteri per riga). Su un laptop un
paragrafo a tutta larghezza è **l'errore di leggibilità più comune** di una
pagina di marketing: l'occhio perde la riga tornando a capo.

I titoli possono essere più larghi: una riga sola non ha il problema del
ritorno a capo.

## 6. Struttura di una pagina

```
Hero          h1 (text-display) + una frase + UNA CTA
Sezione       h2 + intro + griglia di 3 punti
[Sezione]     alternata di sfondo se serve separare
CTA finale    ripete l'azione, per chi ha letto tutto
Footer        contatti + privacy (in ogni pagina)
```

Tre punti nella griglia, non sei: `HighlightGrid` va a 1 colonna sul telefono,
2 da `sm`, 3 da `lg` e si ferma lì, perché oltre le tre colonne la riga di
prosa dentro la card diventa illeggibile.

## 7. Navigazione

- **Inline da `md`**, dietro un disclosure sotto: quattro voci non stanno su
  375px senza comprimere il nome del prodotto.
- Il menu si apre **al tap** e si chiude **dopo la navigazione**. Su touch
  l'hover non esiste, e un pannello lasciato aperto sopra la pagina appena
  richiesta è il bug classico dei menu mobili.
- `aria-expanded` e `aria-controls` sul pulsante: senza, per uno screen reader
  è un quadrato senza nome.
- Una sola azione primaria nell'header — l'ingresso all'area riservata.

## 8. Componenti

Quelli specifici della vetrina vivono in
`modules/site/frontend/src/components/`, **non** in `packages/ui`: li usa un
modulo solo, e un design system che accoglie tutto diventa un contenitore di
casi particolari. Salgono a `packages/ui` il giorno in cui una seconda
superficie ne ha bisogno.

| Componente      | Dove                         | Cosa fa                                            |
| --------------- | ---------------------------- | -------------------------------------------------- |
| `PageSection`   | `packages/ui` — **promosso** | Una banda: ritmo, titolo del ruolo giusto, intro   |
| `HighlightGrid` | `modules/site/frontend`      | La griglia di punti, come card                     |
| `SiteHeader`    | `modules/site/frontend`      | Navigazione pubblica + ingresso all'area riservata |
| `SiteFooter`    | `modules/site/frontend`      | Contatti, privacy, copyright                       |
| `ContactForm`   | `modules/site/frontend`      | Il form contatti, con i suoi tre esiti             |

`PageSection` è salito applicando esattamente quella regola: la vetrina dei
servizi (`modules/services`) è la seconda superficie che ha bisogno dello
stesso ritmo verticale, e l'alternativa — un modulo che importa i componenti
di un altro modulo — è la dipendenza che rende i moduli non più rimovibili.

## 9. Verifica

Prima di dire che una pagina pubblica è finita:

- [ ] **375px**: nessuno scroll orizzontale, menu apribile al tap, CTA
      raggiungibile col pollice
- [ ] I paragrafi hanno `max-w-prose`
- [ ] Una sola CTA primaria per schermata
- [ ] Tema chiaro **e** scuro (la vetrina eredita il tema del sistema)
- [ ] Nessun valore letterale nel markup: se manca un token, si aggiunge il
      token
- [ ] `pnpm --filter @enterprise/web test:e2e` — `e2e/site.spec.ts`
