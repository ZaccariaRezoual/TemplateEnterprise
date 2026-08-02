# Design System — superficie privata

Come si progetta l'**area riservata**: dashboard, elenchi, form di lavoro,
tutto ciò che vive sotto `/admin`.

> Questo documento **non** ridefinisce il sistema. La base — architettura dei
> token, contratto di accessibilità, convenzioni dei componenti, theming — è in
> [design-system.md](design-system.md), e in caso di conflitto vince quella.
> Qui c'è solo ciò che è specifico di questa superficie.
>
> La controparte è [design-system-public.md](design-system-public.md).

---

## 1. Il carattere della superficie

È uno **strumento di lavoro**: qualcuno ci passa la giornata.

| Principio              | In pratica                                               |
| ---------------------- | -------------------------------------------------------- |
| Densità                | Più informazione per schermata, meno scroll              |
| Titoli che etichettano | Un `h1` qui nomina la schermata, non annuncia niente     |
| Prevedibilità          | La stessa cosa nello stesso posto in ogni pagina         |
| Nessuna sorpresa       | Niente movimento che non spieghi un cambiamento di stato |

È l'opposto della vetrina, e la differenza è deliberata: chi lavora vuole
vedere di più e cliccare meno; chi visita vuole capire in fretta.

**Il default è questo.** `:root` porta i valori dell'area privata, e la
vetrina li sovrascrive nel proprio sottoalbero — non il contrario.

## 2. Tipografia e ritmo

| Utility        | Privata | Pubblica           |
| -------------- | ------- | ------------------ |
| `text-display` | 1.5rem  | 2 → 3.25rem fluido |
| `text-heading` | 1.25rem | 1.5 → 2rem fluido  |
| `text-title`   | 1rem    | 1.25 → 1.5rem      |
| `py-band`      | 1.5rem  | 2.5rem             |
| `py-band-lg`   | 2.5rem  | 5rem               |

Un titolo grande in un gestionale non aiuta: sposta in basso il contenuto che
la persona è venuta a leggere.

## 3. Il guscio

`AdminLayout`:

- **Header** con nome del prodotto, navigazione (inline da `md`), stato della
  connessione, notifiche, account, tema.
- **Bottom bar sotto `md`**: la navigazione primaria a portata di pollice, con
  **icona e testo**. Solo icone non si capisce, solo testo non si scansiona.
- `main` riserva il padding inferiore (`pb-20 md:pb-10`) perché il contenuto
  non finisca sotto la barra fissa.
- `env(safe-area-inset-bottom)` sulla barra, o l'home indicator di iOS si
  mangia l'ultima fila.

Regola della bottom bar: **massimo 5 voci**. Oltre, si perde la scansione a
colpo d'occhio, e l'area di tocco di ciascuna scende sotto i 44px su un
telefono piccolo.

## 4. Densità e dati

- **Cifre tabellari** (`tabular-nums`) per qualsiasi numero che cambia:
  contatori, importi, durate. Senza, passare da 9 a 10 sposta il layout sotto
  gli occhi di chi guarda.
- **Troncare, non andare a capo**, in tabelle ed elenchi: una voce
  insolitamente lunga non deve cambiare l'altezza della riga e riflusso tutto
  il resto.
- **Skeleton, non spinner**, quando la forma del risultato è nota: lo spazio è
  riservato in anticipo e il contenuto non salta all'arrivo.
- **Stato vuoto esplicito.** Una tabella vuota che significa «non hai il
  permesso» è una bugia: sono due informazioni diverse e vanno dette in modo
  diverso.

## 5. Form di lavoro

Valgono le regole generali del design system (§7 di
[design-system.md](design-system.md)) con due accenti:

- **Label sempre visibile.** Un placeholder non è una label: sparisce al focus,
  proprio quando serve.
- **L'errore accanto al campo**, non solo in cima. Un riepilogo in testa serve
  solo quando gli errori sono molti.

## 6. Widget di dashboard

La landing dell'area privata è composta dai moduli installati (vedi
[modules/dashboard/README.md](../modules/dashboard/README.md)):

- griglia 1 → 2 (`sm`) → 4 (`lg`) colonne;
- una tile `List` occupa due colonne da `sm`, perché una colonna di etichette
  troncate a larghezza di tile-statistica non si legge;
- ordinamento stabile (`Order`, poi titolo) — una griglia che si rimescola a
  ogni caricamento è disorientante;
- un provider che fallisce perde la sua tile, non la pagina.

## 7. Verifica

Prima di dire che una pagina privata è finita:

- [ ] **375px**: nessuno scroll orizzontale, ultimo controllo non coperto dalla
      bottom bar
- [ ] Numeri con `tabular-nums`
- [ ] Stati di caricamento, errore e vuoto **tutti e tre** gestiti
- [ ] Le azioni che l'utente non può compiere non sono renderizzate (`v-can`)
- [ ] Tema chiaro e scuro
- [ ] `pnpm --filter @enterprise/web test:e2e` — `e2e/responsive.spec.ts`
