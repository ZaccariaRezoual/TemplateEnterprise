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
  ContactPhone      string        obbligatorio: §3
  CustomerNote      string?       scritta in prenotazione
  AdminNote         string?       mai mostrata al cliente
  CreatedAtUtc / UpdatedAtUtc

AppointmentReminder                un promemoria inviato, §8
  AppointmentId, Kind (DayBefore | SameDayMorning), SentAtUtc
  UNIQUE (AppointmentId, Kind)     ← l'idempotenza la garantisce il database

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

## 3. Chi può prenotare — **deciso: account obbligatorio**

Per prenotare serve un account con **email, password e numero di telefono**.

Il telefono è il canale che funziona quando gli altri no: un appuntamento
saltato si recupera con una chiamata, non con una mail non letta.

**Dove vive il numero.** Sull'appuntamento (`ContactPhone`), non solo sul
profilo: è il recapito _per quella prenotazione_, e chi prenota per conto di
qualcun altro lascia legittimamente un numero diverso. Se il profilo ne ha uno,
il campo arriva precompilato.

**La registrazione è l'ultimo passo.** Si sceglie servizio, giorno e ora
_prima_ di chiedere qualsiasi dato: chiedere l'account all'inizio è ciò che fa
abbandonare le prenotazioni. Chi è già registrato salta il passo e conferma.

Conseguenza tecnica: la registrazione dentro il flusso di prenotazione non deve
perdere la selezione. Lo slot scelto viaggia nella prenotazione, e al ritorno
dal login la conferma riparte da lì — non dalla prima schermata.

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
  ) WHERE (status = 'Confirmed');
```

Due appuntamenti **confermati** sovrapposti diventano impossibili, non
improbabili.

### Deciso: una richiesta non occupa lo slot

Il vincolo copre solo `Confirmed`, quindi più persone possono chiedere lo
stesso orario. Con la conferma manuale (§ decisione 3) è coerente: chi
amministra sceglie.

Ma va progettato, perché tre conseguenze non sono ovvie:

1. **Il conflitto si sposta alla conferma.** Confermando la seconda richiesta
   sovrapposta il database rifiuta. L'interfaccia deve dirlo prima: una
   richiesta in conflitto con un appuntamento già confermato si mostra
   **segnalata**, non come una qualsiasi.
2. **Confermarne una rifiuta le altre.** Alla conferma, le richieste
   sovrapposte passano automaticamente a `Cancelled` con motivo «slot non più
   disponibile», e i loro autori ricevono la notifica. Lasciarle appese
   significa persone che aspettano una risposta che non arriverà.
3. **La pagina pubblica deve essere onesta.** Dice «richiesta inviata», mai
   «prenotazione confermata»: finché l'amministratore non conferma, quell'ora
   non è di nessuno. È la differenza fra un'attesa e una promessa non
   mantenuta.

Il rovescio positivo: nessuno slot resta bloccato da richieste mai confermate,
quindi **non serve una scadenza** — una cosa in meno che può guastarsi.

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

### I promemoria — **decisi: due**

1. **Il giorno prima** (24 ore prima dell'orario).
2. **La mattina stessa**, a un'ora locale configurata (default 08:00).

Il secondo non è un «offset»: è un **orario del giorno**. Un appuntamento alle
9:30 e uno alle 18:00 devono ricevere lo stesso promemoria alle 8:00, non
rispettivamente alle 8:30 e alle 17:00. Modellarlo come «N ore prima»
funzionerebbe per un solo appuntamento della giornata.

Il framework **non ha uno scheduler** (rimandato in Fase 7 di PLAN.md, per
scelta). Serve un `BackgroundService` che ogni minuto valuta le due regole e
pubblica `AppointmentReminderDue`.

**L'idempotenza la garantisce il database**, non il codice: la tabella
`AppointmentReminder` ha un vincolo di unicità su `(AppointmentId, Kind)`, e
chi inserisce per primo manda la notifica. Con due istanze dell'API la seconda
viola il vincolo e non manda nulla. Senza, il cliente riceve due email
identiche — il modo più veloce per fargli disattivare le notifiche.

Un promemoria si manda **solo per gli appuntamenti confermati**: avvisare
qualcuno di un appuntamento che nessuno ha ancora accettato è peggio del
silenzio.

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

**Deciso: libreria, e la libreria è FullCalendar.**

Un calendario con trascinamento scritto a mano è settimane di lavoro e di casi
limite (settimane a cavallo di mesi, eventi sovrapposti, tutto-il-giorno, ora
legale). Le candidate valutate:

| Libreria         | Perché sì                                                                                                          | Perché no                                   |
| ---------------- | ------------------------------------------------------------------------------------------------------------------ | ------------------------------------------- |
| **FullCalendar** | Aspetto e interazioni da Google Calendar; trascinamento incluso nel pacchetto MIT; wrapper Vue ufficiale; maturità | Più pesante; porta CSS proprio              |
| Schedule-X       | Nativa Vue 3, leggera, molto simile a Google Calendar                                                              | Più giovane; alcuni plugin sono a pagamento |
| vue-cal          | Leggerissima                                                                                                       | Trascinamento e viste meno complete         |

Scelgo **FullCalendar** perché il trascinamento — che è il requisito — è nel
pacchetto libero, e perché su un calendario di lavoro la maturità vale più
della leggerezza.

**Da verificare in Fase 3, non da dare per scontato**: licenza delle viste che
useremo (le viste _timeline/risorse_ sono premium; mese, settimana e giorno non
lo sono) e peso reale del bundle. Se una delle due sorprende, si cambia — ed è
proprio per questo che vale il vincolo seguente.

Due vincoli non negoziabili:

- **Incapsulata in un nostro componente** (`AppointmentCalendar.vue`): l'app
  parla con la nostra interfaccia, non con quella della libreria. Sostituirla
  diventa un file, non una riscrittura.
- **Tematizzata dai token.** La libreria porta il proprio CSS, ed è esattamente
  il modo in cui i valori letterali rientrano dalla finestra dopo che il design
  system li ha tenuti fuori dalla porta. Le sue variabili CSS si mappano sui
  nostri token semantic, e il calendario segue il tema chiaro/scuro come tutto
  il resto.

### Aggiungere l'appuntamento al proprio calendario

Requisito esplicito: Google Calendar e calendario iOS. Due meccanismi, **zero
dipendenze e nessun OAuth**:

| Meccanismo                                                                                    | Copre                                         |
| --------------------------------------------------------------------------------------------- | --------------------------------------------- |
| **File `.ics`** (allegato alla mail di conferma + link «Aggiungi al calendario»)              | Apple Calendar, Outlook, e l'import di Google |
| **URL template di Google Calendar** (`calendar.google.com/calendar/render?action=TEMPLATE&…`) | Google Calendar con un click                  |

L'`.ics` porta anche gli **aggiornamenti**: uno spostamento rimanda il file con
lo stesso `UID` e `SEQUENCE` incrementato, e il calendario del cliente si
aggiorna da solo invece di accumulare doppioni.

Resta fuori la **sincronizzazione bidirezionale** (leggere il calendario del
cliente): richiede OAuth, token da rinnovare e un consenso che quasi nessuno
concede a un fornitore di servizi.

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
      "ReminderDayBeforeHours": 24,
      "ReminderSameDayLocalTime": "08:00",
      "CustomerCancellationCutoffHours": 24
    }
  }
}
```

I due promemoria sono modellati diversamente **perché sono cose diverse**: uno
è una distanza dall'appuntamento, l'altro è un'ora del giorno (§8).

## 11. Fasi

### Fase 0 — Dominio e motore di disponibilità

Modulo, schema, entity, proiezione da Services, **classe pura di calcolo degli
slot** con i suoi test.

✅ **Done quando**: i test coprono buffer, preavviso, chiusure, giorno del
cambio ora legale e giornata già piena — e passano.

### Fase 1 — Prenotazione, conferma e concorrenza

Endpoint di disponibilità, prenotazione (`Requested`), conferma, vincolo di
esclusione e gestione della violazione, rifiuto automatico delle richieste
sovrapposte.

✅ **Done quando**: un test d'integrazione lancia **due conferme simultanee su
richieste sovrapposte** e ne va a buon fine esattamente una, con un errore
comprensibile per l'altra; e confermare una richiesta annulla le sovrapposte.

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

Eventi pubblici, sottoscrizione di Email, allegato `.ics` e link «Aggiungi al
calendario» (ICS + URL Google), i due promemoria idempotenti.

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

## 14. Decisioni prese

1. **Account obbligatorio** per prenotare, con email, password e **telefono**;
   registrazione all'ultimo passo (§3).
2. **Le richieste non occupano lo slot**: il vincolo copre solo `Confirmed`, e
   confermarne una rifiuta automaticamente le sovrapposte (§6).
3. **Conferma manuale**: `Requested` esiste, e la pagina pubblica dice
   «richiesta», mai «confermato».
4. **Due promemoria**: il giorno prima, e la mattina stessa alle 08:00 locali
   (§8).
5. **FullCalendar**, incapsulato e tematizzato; aggiunta al calendario del
   cliente via `.ics` e URL template di Google (§9).

Resta una sola cosa da verificare **durante** la Fase 3, non prima: licenza e
peso di FullCalendar per le viste che useremo.
