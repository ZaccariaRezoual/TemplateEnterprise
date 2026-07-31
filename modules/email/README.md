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

`IEmailSender` is the seam. The default is `LoggingEmailSender`, which writes
the message to the log instead of delivering it — deliberately, so a developer
running against a seeded database cannot email real users. Register your own
implementation after the module to override it:

```csharp
services.AddScoped<IEmailSender, SmtpEmailSender>();
```

## Templates

`EmailTemplateRenderer` substitutes `{{placeholders}}` and **HTML-encodes every
value** in the HTML body: display names and emails reach these templates, and
unencoded output would make the message a script-injection vector in web mail
clients. Unknown placeholders are left visible rather than blanked — visibly
broken gets reported, silently wrong does not.

Projects needing layouts or loops replace the renderer with Razor or Fluid;
callers work with rendered strings, so nothing else changes.
