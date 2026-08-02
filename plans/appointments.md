# Piano — Gestione degli appuntamenti

> Piano operativo per il modulo **Appointments**: prenotazione pubblica di un
> servizio su slot reali, calendario amministrativo con spostamento degli
> appuntamenti, notifiche via email e in tempo reale.
>
> **Dipende da** [services.md](services.md): si prenota un servizio, e la sua
> `DurationMinutes` è ciò che genera gli slot. Senza quel piano, questo non
> parte.

---

## 1. Cosa serve

Un visitatore sceglie un servizio, vede **quando è davvero possibile**,
prenota. L'amministratore vede tutto su un calendario, conferma, sposta,
annulla. Entrambi vengono avvisati a ogni passaggio.

Le due parti difficili non sono l'interfaccia: sono **il calcolo della
disponibilità** e **il fatto che due persone possano prenotare lo stesso slot
nello stesso istante**. Il resto è lavoro noto.

## 2. Il dominio

```csharp
Appointment
  Id                Guid
  ServiceId         Guid          per id, mai FK: è di un altro modulo
  CustomerUserId    Guid          chi prenota
  StartUtc, EndUtc  DateTime      sempre UTC (§4)
  Status            enum          Requested → Confirmed → Completed
                                            ↘ Cancelled / NoShow
  CustomerNote      string?       scritta in prenotazione
  AdminNote         string?       mai mostrata al cliente
  ReminderSentAtUtc DateTime?     §8: rende il promemoria idempotente
  CreatedAtUtc / UpdatedAtUtc

AppointmentHistory                 chi ha fatto cosa e quando
  AppointmentId, ChangedByUserId, FromStatus, ToStatus, FromStartUtc, ToStartUtc, AtUtc

AvailabilityRule                   orario settimanale ricorrente
  DayOfWeek, StartLocal, EndLocal

AvailabilityException              chiusure e aperture straordinarie
  DateLocal, StartLocal?, EndLocal?, IsClosed, Reason

ServiceProjection                  proiezione da Services (titolo, durata, bookable)
```

**`AppointmentHistory` non è un lusso**: «me l'avevate spostato voi» è una
discussione che si chiude solo con un registro. Il modulo Audit registra i
comandi, ma qui serve una storia leggibile _dentro_ l'appuntamento.

**`ServiceProjection`** è la regola dei moduli applicata: Appointments non
interroga Services, si costruisce la propria copia sottoscrivendone gli eventi
pubblici.

## 3. Chi può prenotare — decisione da prendere subito

L'utente ha chiesto notifiche **via email e via SignalR** al cliente. SignalR
richiede un'identità: **senza account non c'è canale realtime**, e non c'è
nemmeno un modo sicuro di far annullare un appuntamento a distanza di giorni.

| Opzione                                | Conseguenze                                                                                  |
| -------------------------------------- | -------------------------------------------------------------------------------------------- |
| **Account obbligatorio** (consigliata) | Notifiche complete, storico, annullamento sicuro. Attrito: registrazione prima di confermare |
| Ospite con link firmato                | Zero attrito, ma niente realtime, e ogni azione passa da un token nell'email                 |

Consigliata la prima, con la registrazione **all'ultimo passo**: si sceglie
servizio, giorno e ora _prima_ di chiedere qualsiasi dato. Chiedere l'account
all'inizio è ciò che fa abbandonare le prenotazioni.

## 4. Fusi orari — la parte che si sbaglia sempre

Regole non negoziabili:

- **Si persiste UTC.** Sempre.
- L'attività ha **un fuso configurato** (`Modules:Appointments:TimeZone`), e gli
  orari di disponibilità sono **locali a quel fuso**: «apro alle 9» significa
  le 9 lì, non 9 UTC.
- Gli slot si calcolano **nel fuso dell'attività**, poi si convertono. Farlo in
  UTC produce orari sbagliati di un'ora per metà anno.
- **L'ora legale non è un dettaglio**: nel giorno del cambio una giornata dura
  23 o 25 ore, e un'ora locale può non esistere o esistere due volte. Il codice
  usa `TimeZoneInfo` con conversione esplicita, mai aritmetica su `DateTime`.
- Il fuso del **client** serve solo a _mostrare_ l'orario, mai a calcolarlo.

Questi casi vanno nei test unitari fin dalla prima fase, non «dopo».

## 5. Il calcolo della disponibilità

`GET /api/appointments/availability?serviceId={id}&from={date}&to={date}` →
giorni con i loro slot liberi.

Ingredienti:

1. le `AvailabilityRule` del giorno della settimana;
2. meno le `AvailabilityException` (chiusure) e più quelle straordinarie;
3. meno gli appuntamenti che **occupano** (§6);
4. passo della griglia (`SlotGranularityMinutes`, es. 15);
5. durata del servizio **+ buffer** (`BufferMinutes`: il tempo fra due
   appuntamenti — pulizia, spostamento, respiro);
6. **preavviso minimo** (`MinimumNoticeHours`): non si prenota fra dieci
   minuti;
7. **orizzonte massimo** (`MaxAdvanceDays`): non si prenota fra due anni.

Il calcolo vive in una **classe pura**, senza database né HTTP: prende regole,
eccezioni, appuntamenti e parametri, restituisce slot. È l'unico modo di
testarne davvero i casi limite — e i casi limite qui sono molti.

## 6. Doppia prenotazione — il problema di correttezza

Due visitatori chiedono lo stesso slot nello stesso istante. Entrambi vedono
«libero», entrambi confermano. Un controllo «esiste già un appuntamento?»
seguito da un `INSERT` **non lo impedisce**: fra la lettura e la scrittura c'è
spazio.

**Soluzione: lo dice il database.** PostgreSQL ha i vincoli di esclusione:

```sql
ALTER TABLE appointments.appointments
  ADD CONSTRAINT no_overlap
  EXCLUDE USING gist (
    tstzrange(start_utc, end_utc) WITH &&
  ) WHERE (status IN ('Requested', 'Confirmed'));
```

Due appuntamenti sovrapposti diventano **impossibili**, non improbabili. Il
codice applicativo gestisce la violazione e risponde «lo slot è appena stato
preso», che è la verità.

Decisione dentro il vincolo: **anche `Requested` occupa**. Altrimenti dieci
richieste non confermate sullo stesso slot sono legittime, e nove persone
riceveranno un rifiuto dopo aver aspettato. Il rischio opposto — slot bloccati
da richieste mai confermate — si gestisce con una scadenza (`ExpiresAtUtc`) o
con la conferma rapida; va deciso, non ignorato.

⟶ Se un giorno l'attività avrà **più operatori**, il vincolo diventa per
operatore. La migration di allora aggiunge la colonna al vincolo: vale la pena
saperlo ora, non progettarlo ora.

## 7. API

**Pubbliche / cliente** (autenticato, dati propri, nessun permesso):

| Metodo | Rotta                            | Cosa                  |
| ------ | -------------------------------- | --------------------- |
| GET    | `/api/appointments/availability` | Slot liberi (anonimo) |
| POST   | `/api/appointments`              | Prenota → `Requested` |
| GET    | `/api/appointments/mine`         | I miei appuntamenti   |
| POST   | `/api/appointments/{id}/cancel`  | Annulla il mio        |

**Amministrative** (`appointments.read` / `appointments.write`):

| Metodo  | Rotta                                     | Cosa                |
| ------- | ----------------------------------------- | ------------------- |
| GET     | `/api/admin/appointments?from=&to=`       | Calendario          |
| POST    | `/api/admin/appointments/{id}/confirm`    | Conferma            |
| POST    | `/api/admin/appointments/{id}/reschedule` | Sposta              |
| POST    | `/api/admin/appointments/{id}/cancel`     | Annulla             |
| GET/PUT | `/api/admin/availability`                 | Regole ed eccezioni |

`availability` è **anonima**: serve alla pagina di prenotazione prima del
login. Espone solo «libero/occupato», mai chi ha prenotato — è la differenza
fra un calendario pubblico e una fuga di dati.

Lo spostamento **rivalida sempre lato server**: il trascinamento nel calendario
è un'intenzione, non un'autorizzazione.

## 8. Notifiche

**Nessun trasporto nuovo da scrivere.** Il modulo Notifications espone già
`NotificationRequested` come contratto pubblico, e `NotificationCreated`
implementa `IRealtimeEvent`: pubblicare l'uno produce la notifica persistita e
il push realtime. L'email si ottiene come per il form contatti — il modulo
Email sottoscrive gli eventi pubblici di Appointments.

Eventi pubblici in `modules/appointments/shared/Events/`:

| Evento                   | Al cliente              | All'amministratore          |
| ------------------------ | ----------------------- | --------------------------- |
| `AppointmentRequested`   | «Richiesta ricevuta»    | «Nuova richiesta»           |
| `AppointmentConfirmed`   | «Confermato»            | —                           |
| `AppointmentRescheduled` | «Spostato al …»         | se l'ha spostato il cliente |
| `AppointmentCancelled`   | se annullato dall'admin | se annullato dal cliente    |
| `AppointmentReminderDue` | «Domani alle …»         | «Domani hai …»              |

Due dettagli che valgono:

- **Allegato ICS** nella mail di conferma: l'appuntamento entra nel calendario
  del cliente con un click. Costo basso, valore alto, nessuna dipendenza.
- **Audience realtime**: al cliente `ForUser`; all'amministratore
  `ForGroup("role:Admin")`, con la cautela già documentata — in
  un'installazione multi-tenant quel gruppo attraversa i tenant.

### I promemoria: l'unica parte con un tempo proprio

Il framework **non ha uno scheduler** (rimandato in Fase 7 di PLAN.md, per
scelta). Serve un `BackgroundService` che ogni minuto cerca gli appuntamenti
che iniziano entro N ore e pubblica `AppointmentReminderDue`.

**Idempotenza per costruzione**: si marca `ReminderSentAtUtc` con un
`UPDATE … WHERE ReminderSentAtUtc IS NULL RETURNING …`. Chi vince la riga manda
il promemoria. Con due istanze dell'API, la seconda non trova nulla da
mandare — senza questo, il cliente riceve due email identiche, che è il modo
più veloce per far disattivare le notifiche.

## 9. Frontend

### Pubblico — prenotazione

`/services/{slug}` → «Prenota» → `/book/{slug}`:

1. **Giorno**: un calendario mensile che mostra solo i giorni con slot.
2. **Ora**: gli slot del giorno, come pulsanti (target ≥44px: si prenota dal
   telefono).
3. **Conferma**: riepilogo, nota facoltativa, accesso o registrazione.
4. **Esito**: cosa succede adesso — «riceverai una conferma».

Tre schermate, un passo per volta: un modulo unico con calendario, orari e
registrazione è illeggibile su 375px.

**Lo slot può sparire mentre si compila.** Il messaggio deve dirlo con chiarezza
e riportare alla scelta dell'ora, non a un errore generico.

### Amministrazione — calendario

`/admin/appointments`: viste mese/settimana/giorno, trascinamento per spostare,
pannello di dettaglio con conferma/annulla/note, gestione della disponibilità.

**Decisione: libreria o costruzione?** Un calendario con trascinamento scritto a
mano è settimane di lavoro e di casi limite. Una libreria è giustificata, con
due vincoli non negoziabili:

- **incapsulata in un nostro componente**, così l'app non dipende dalla sua
  API;
- **tematizzata dai token**: molte librerie portano CSS proprio, ed è
  esattamente il modo in cui rientrano dalla finestra i valori letterali che il
  design system tiene fuori dalla porta.

Candidate da valutare in Fase 3: Schedule-X e vue-cal (leggere, native Vue),
FullCalendar (matura, più pesante, licenza da leggere per le funzioni
premium).

### Utente — i miei appuntamenti

Elenco in `/admin/…`? **No**: è area riservata ma non amministrativa. Vive
sotto `/admin/my-appointments` — dentro l'area autenticata, senza permessi,
perché sono dati propri. Ci si annulla entro il limite consentito.

## 10. Configurazione

```json
{
  "Modules": {
    "Appointments": {
      "TimeZone": "Europe/Rome",
      "SlotGranularityMinutes": 15,
      "BufferMinutes": 0,
      "MinimumNoticeHours": 2,
      "MaxAdvanceDays": 60,
      "ReminderHoursBefore": [24],
      "CustomerCancellationCutoffHours": 24
    }
  }
}
```

`ReminderHoursBefore` è una lista: «24 ore prima» e «1 ora prima» sono due
promemoria diversi, e il campo `ReminderSentAtUtc` diventa allora una tabella
di promemoria inviati. Va deciso in Fase 4: **uno solo** basta quasi sempre.

## 11. Fasi

### Fase 0 — Dominio e motore di disponibilità

Modulo, schema, entity, proiezione da Services, **classe pura di calcolo degli
slot** con i suoi test.

✅ **Done quando**: i test coprono buffer, preavviso, chiusure, giorno del
cambio ora legale e giornata già piena — e passano.

### Fase 1 — Prenotazione e concorrenza

Endpoint di disponibilità e prenotazione, vincolo di esclusione, gestione della
violazione.

✅ **Done quando**: un test d'integrazione lancia **due prenotazioni simultanee
sullo stesso slot** e ne conferma esattamente una, con un errore comprensibile
per l'altra.

### Fase 2 — Flusso pubblico

Le tre schermate, la CTA nel dettaglio del servizio, «i miei appuntamenti».

✅ **Done quando**: un e2e prenota dal browser a 375px e vede l'appuntamento
nel proprio elenco.

### Fase 3 — Calendario amministrativo

Scelta della libreria, componente incapsulato e tematizzato, viste,
trascinamento con rivalidazione, gestione della disponibilità.

✅ **Done quando**: spostare un appuntamento su uno slot occupato viene
rifiutato dal server e la UI torna indietro senza ricaricare.

### Fase 4 — Notifiche

Eventi pubblici, sottoscrizione di Email, ICS, promemoria idempotenti.

✅ **Done quando**: prenotazione, conferma, spostamento e annullamento
producono email (nel log dell'`IEmailSender` di sviluppo) **e** notifica
realtime; il promemoria eseguito due volte manda una sola email.

### Fase 5 — Composizione e documentazione

Widget «appuntamenti di oggi» (admin) e «prossimo appuntamento» (cliente),
`modules/appointments/README.md`, pattern page sul calcolo della disponibilità,
`create-project.md`.

✅ **Done quando**: la dashboard mostra gli appuntamenti senza che il modulo
Dashboard sappia che esistono.

## 12. Test

| Livello     | Cosa                                                                                    |
| ----------- | --------------------------------------------------------------------------------------- |
| Unit        | Motore degli slot: buffer, preavviso, orizzonte, chiusure, **ora legale**, giorno pieno |
| Unit        | Transizioni di stato lecite e vietate (confermare un annullato, ecc.)                   |
| Integration | Due prenotazioni simultanee → una sola vince                                            |
| Integration | `availability` anonima non rivela chi ha prenotato                                      |
| Integration | Promemoria eseguito due volte → una sola notifica                                       |
| e2e         | Prenotazione completa a 375px; spostamento nel calendario admin                         |

I tre test d'integrazione sono il cuore: sono le tre cose che **non si vedono
guardando l'interfaccia** e che rovinano la fiducia nel prodotto quando
falliscono.

## 13. Cosa questo piano NON prevede

- **Pagamenti**: un altro dominio, con i suoi requisiti legali.
- **Più operatori / risorse**: il vincolo di esclusione è già scritto per
  accoglierli (§6), ma il resto — chi è assegnato, chi vede cosa — è un piano a
  sé.
- **Sincronizzazione con Google/Outlook Calendar**: l'ICS copre il caso d'uso
  comune senza OAuth e senza token da rinnovare.
- **Liste d'attesa e prenotazioni ricorrenti**: si aggiungono quando qualcuno
  le chiede davvero.

## 14. Decisioni da confermare

1. **Account obbligatorio per prenotare** (§3)? Cambia il flusso pubblico e il
   perimetro delle notifiche.
2. **Le richieste non confermate occupano lo slot** (§6)? In caso affermativo,
   dopo quanto scadono?
3. **Conferma manuale o automatica**? Il piano assume che l'amministratore
   confermi. Con la conferma automatica, `Requested` sparisce e metà delle
   notifiche con lui.
4. **Quanti promemoria** e a che distanza (§10)?
5. **Libreria del calendario** (§9): valutazione in Fase 3, o preferenza già
   ora?
