# Pattern di sviluppo

Indice per **attività**: cerca quello che devi fare, non l'area tecnica in cui
ricade.

Ogni pagina segue lo stesso schema (fonte normativa → quando serve → procedura
→ come si verifica → errori tipici). Per aggiungerne una, copia
[`_template.md`](_template.md) e leggi «Come si contribuisce» nella
[home della wiki](../README.md).

## Backend

| Devo…                                          | Pagina                                                  |
| ---------------------------------------------- | ------------------------------------------------------- |
| esporre una nuova operazione dell'API          | [Aggiungere un endpoint](add-endpoint.md)               |
| creare un modulo nuovo da zero                 | [Creare un modulo](create-module.md)                    |
| far comunicare due moduli                      | [Far parlare due moduli](module-to-module.md)           |
| proteggere qualcosa con un permesso nuovo      | [Aggiungere un permesso](add-permission.md)             |
| dare al modulo le sue tabelle                  | [Persistenza di un modulo](module-persistence.md)       |
| segnalare un errore al client                  | [Errori](errors.md)                                     |
| far arrivare un evento ai client senza refresh | [Rendere un evento realtime](make-it-realtime.md)       |
| mostrare qualcosa in dashboard                 | [Contribuire un widget](contribute-dashboard-widget.md) |
| scrivere i test giusti                         | [Testare il backend](test-backend.md)                   |

## Frontend

| Devo…                                        | Pagina                                                            |
| -------------------------------------------- | ----------------------------------------------------------------- |
| aggiungere una feature che consuma l'API     | [Aggiungere una feature frontend](add-feature.md)                 |
| aggiungere una pagina del sito pubblico      | [Aggiungere una pagina pubblica](add-public-page.md)              |
| decidere dove tenere uno stato               | [Dove tenere lo stato](state.md)                                  |
| aggiornare l'SDK dopo un cambio di contratto | [Rigenerare l'SDK](regenerate-sdk.md)                             |
| aggiungere un componente al design system    | [Aggiungere un componente al design system](add-ui-component.md)  |
| scegliere il token giusto                    | [Scegliere il token giusto](use-design-tokens.md)                 |
| verificare che una pagina sia responsive     | [Checklist responsive](responsive-checklist.md)                   |
| nascondere qualcosa a chi non può vederlo    | [Nascondere ciò che l'utente non può fare](client-permissions.md) |
| aggiungere una stringa tradotta              | [Aggiungere una stringa tradotta](i18n.md)                        |
| scrivere i test giusti                       | [Testare il frontend](test-frontend.md)                           |

## Trasversali

| Devo…                                 | Pagina                                                  |
| ------------------------------------- | ------------------------------------------------------- |
| aprire una PR come si deve            | [Aprire una PR come si deve](git-workflow.md)           |
| rivedere il codice di qualcun altro   | [Rivedere il codice di qualcun altro](code-review.md)   |
| creare un progetto nuovo dal template | [Creare un progetto nuovo dal template](new-project.md) |
| capire un errore che non ha senso     | [Errori che non hanno senso](troubleshooting.md)        |

---

Se una pagina manca, [aggiungila](_template.md) — le regole sono nella
[home della wiki](../README.md).
