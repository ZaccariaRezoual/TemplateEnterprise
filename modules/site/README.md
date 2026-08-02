# Site Module

Il sito pubblico: home, chi siamo, servizi, contatti, privacy.

È la faccia che chiunque raggiunge senza account. L'area riservata vive sotto
`/admin` ed è un altro guscio — vedi
[plans/public-and-admin-areas.md](../../plans/public-and-admin-areas.md).

## Personalizzare i contenuti

**Le pagine sono del framework, le parole sono del progetto.** Un progetto
nuovo riscrive un solo file:

```ts
// apps/web/src/site.config.ts
export const siteContent: SiteContent = {
  name: "Acme",
  claim: "…",
  about: { title: "Chi siamo", intro: "…", highlights: [...] },
  // …
};
```

Non serve toccare un componente, una rotta o una guardia. Se una pagina non
basta, un progetto scrive la propria e registra quella al posto della nostra:
il modulo è un punto di partenza, non una gabbia.

Il testo predefinito è un **segnaposto che dice di esserlo**. È deliberato:
una copia che si legge come prosa vera è il modo in cui il lorem ipsum arriva
in produzione.

## Decisioni

**Configurazione, non CMS.** Un editor a database si ripaga quando è il
_cliente_ a dover cambiare i testi senza sviluppatori. Finché non è un
requisito, aggiunge uno schema, una UI di editing e una cache da invalidare
per un problema che si risolve con un file tipizzato. Il modulo Settings
copre già il caso delle poche stringhe (indirizzo, email) modificabili senza
deploy.

**Il modulo non conosce l'autenticazione.** Il sito deve offrire una via
d'ingresso, ma non importa il modulo Auth per sapere se c'è una sessione: lo
chiede al host tramite `SiteLinks`. Così il sito pubblico continua a
funzionare in un'applicazione assemblata senza autenticazione.

**Nessuna rotta dichiara `requiresAuth`.** È ciò che le tiene alla radice
mentre tutto il privato viene ricollocato sotto `/admin` dal composition root.
`publicSite: true` è solo un marcatore di guscio, non un permesso.

**Il form contatti non salva niente.** `POST /api/site/contact` pubblica
`ContactMessageReceived` e il modulo Email lo recapita. Persistere i messaggi
significherebbe uno schema, una schermata per leggerli e un permesso per
aprirla: un piccolo CRM, che un progetto aggiunge quando sa di volerlo.

**Serve un subscriber.** Senza il modulo Email (o un altro che ascolti
l'evento) il form accetta i messaggi e nessuno li trasporta. È il prezzo
dell'indipendenza fra moduli, ed è scritto qui perché non si scopra dal
silenzio.

**Honeypot, non captcha.** Un campo nascosto che i bot compilano: zero
dipendenze, zero attrito per il visitatore, e ferma la maggior parte del
traffico automatico. Una submission con l'honeypot pieno viene scartata **e
risponde ugualmente successo** — dire a un bot che è stato riconosciuto
insegna solo a chi l'ha scritto a riprovare meglio. Un captcha si aggiunge se
e quando i log mostrano che serve.

**Rate limit proprio, più stretto del globale.** È l'unico endpoint anonimo
che accetta testo libero, cioè la superficie di abuso dell'intera API. Un
essere umano scrive un messaggio, non cinque al minuto.

**`data-surface="public"` c'è già, e per ora non fa niente.** È il gancio su
cui la Fase 3 appoggerà gli override dei token semantic che danno alla vetrina
scala e ritmo propri. Il seam viaggia prima dello stile: aggiungerlo dopo
avrebbe voluto dire toccare ogni pagina invece di un foglio di stile.

## Struttura

| Cosa                | Dove                                    |
| ------------------- | --------------------------------------- |
| Contratto contenuti | `frontend/src/content.ts`               |
| Guscio pubblico     | `frontend/src/layouts/PublicLayout.vue` |
| Header e footer     | `frontend/src/components/`              |
| Pagine              | `frontend/src/pages/`                   |
| Rotte               | `frontend/src/routes.ts`                |

## Installazione nel host

```ts
installSiteModule({
  content: siteContent,
  links: {
    entryPath: () => (session.isAuthenticated ? "/admin" : "/login"),
    entryLabel: () => (session.isAuthenticated ? "Area riservata" : "Accedi"),
  },
});
```

Più `siteRoutes` nel registro delle rotte e `PublicLayout` nella mappa dei
layout di `App.vue`.

**Un progetto che è solo un gestionale non installa questo modulo**: la radice
torna a reindirizzare all'area riservata, senza cancellare codice.

## Configurazione

```json
{
  "Modules": {
    "Site": {
      "ContactRecipient": "info@example.com",
      "ContactPermitLimit": 3,
      "ContactRateLimitWindowSeconds": 300
    }
  }
}
```

`ContactRecipient` sta qui e non nel modulo Email: **chi** riceve i messaggi è
una decisione del sito, e viaggia dentro l'evento perché il modulo che li
recapita non debba sapere nulla di questo sito.

## Da fare prima di andare online

1. `Modules:Site:ContactRecipient` — altrimenti i messaggi vanno
   all'indirizzo di esempio.
2. `privacy` — un sito che raccoglie nome ed email deve pubblicare
   un'informativa reale. Il segnaposto è peggio del nulla.
