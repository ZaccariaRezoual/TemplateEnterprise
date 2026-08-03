# Email Module

Templated outbound email with a swappable transport.

## No endpoints, on purpose

Sending email is never a client-triggered operation — an endpoint that sends
mail on request is an open relay for spam. Other modules either add a message
to the outbox or, more usually, this module reacts to their public events (as
it does for `UserRegistered`).

## Outbox, not inline sending

Messages go into an in-process outbox drained by a background sender. Sending
inline would tie a user's request to a third-party mail server: registration
would fail because SMTP was slow.

The outbox is **not durable** — pending messages are lost if the process
stops. That is an acceptable trade for a welcome email and a terrible one for
an invoice, so projects with delivery guarantees swap it for the Background
Jobs infrastructure of Fase 7. `IEmailOutbox` stays the same, so callers do
not change.

## Transport

`IEmailSender` is the seam, and **the transport is chosen by configuration,
not by code**: a host in `Email:Smtp` means deliver, its absence means log.

```json
{
  "Email": {
    "FromAddress": "no-reply@acme.example",
    "FromName": "Acme",
    "Smtp": {
      "Host": "smtp.acme.example",
      "Port": 587,
      "UseImplicitTls": false,
      "UserName": "no-reply@acme.example",
      "TimeoutSeconds": 30
    }
  }
}
```

The password is **not** in that file. Supply it out of band:

```bash
export Email__Smtp__Password='…'      # or the platform's secret store
```

Turning real email on is therefore a deployment concern, and forgetting to
configure it **fails safe**: `LoggingEmailSender` writes the message to the
log instead of sending it, so a developer running against a seeded database
cannot email real people by accident.

### Why these are configuration, not admin settings

The Settings module exists and has an admin UI, and SMTP credentials
deliberately do not live there. A secret editable from a web form lives in a
table, and from there it reaches backups, replicas and audit logs. Host, port
and password belong in the environment — which is where the deployment
already keeps the database connection string.

What _is_ worth making admin-editable is the wording around a message, not the
plumbing: `app.name` is already a setting.

### TLS

`SecureSocketOptions.StartTls` is **required**, not "if available": the
permissive variant lets a server that does not offer TLS downgrade the session
to plaintext, and the password goes with it. Set `UseImplicitTls` for port 465.

There is deliberately **no option to skip certificate validation**. For an
internal CA, trust the CA in the operating system's store — where every other
client on that machine already looks. A local test server (MailHog, smtp4dev,
Papercut) needs no workaround: development uses the logging transport anyway.

### Something else entirely

A transactional provider (SendGrid, Postmark, SES) is a different
implementation of the same seam. Register it after the module and it wins:

```csharp
services.AddScoped<IEmailSender, PostmarkEmailSender>();
```

### Attachments

`EmailMessage.Attachments` carries text files — the case it exists for is the
`.ics` on an appointment confirmation, which turns "remember to note this
down" into one click. A transport needing binary attachments should extend the
record rather than base64 into it.

## Templates

`EmailTemplateRenderer` substitutes `{{placeholders}}` and **HTML-encodes every
value** in the HTML body: display names and emails reach these templates, and
unencoded output would make the message a script-injection vector in web mail
clients. Unknown placeholders are left visible rather than blanked — visibly
broken gets reported, silently wrong does not.

Projects needing layouts or loops replace the renderer with Razor or Fluid;
callers work with rendered strings, so nothing else changes.
