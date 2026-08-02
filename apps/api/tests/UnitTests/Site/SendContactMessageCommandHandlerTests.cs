using EnterpriseFramework.Application.Abstractions;
using EnterpriseFramework.Domain.Common;
using EnterpriseFramework.Modules.Site.Contracts.Events;
using EnterpriseFramework.Modules.Site.Features.Contact;
using EnterpriseFramework.Modules.Site.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Site;

public sealed class SendContactMessageCommandHandlerTests
{
    /// <summary>Event bus that records what was published.</summary>
    private sealed class RecordingEventBus : IEventBus
    {
        public List<IDomainEvent> Published { get; } = [];

        public Task PublishAsync<TEvent>(
            TEvent domainEvent,
            CancellationToken cancellationToken = default
        )
            where TEvent : IDomainEvent
        {
            Published.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private static (SendContactMessageCommandHandler Handler, RecordingEventBus Bus) Create(
        string recipient = "info@example.com"
    )
    {
        var bus = new RecordingEventBus();
        var options = Options.Create(new SiteOptions { ContactRecipient = recipient });
        return (
            new SendContactMessageCommandHandler(
                bus,
                options,
                NullLogger<SendContactMessageCommandHandler>.Instance
            ),
            bus
        );
    }

    private static SendContactMessageCommand Message(string? website = null) =>
        new("Ada", "ada@example.com", "Hello", website);

    [Fact]
    public async Task PublishesTheMessageForWhoeverDeliversIt()
    {
        var (handler, bus) = Create("desk@acme.test");

        await handler.Handle(Message(), CancellationToken.None);

        var published = bus.Published.ShouldHaveSingleItem().ShouldBeOfType<ContactMessageReceived>();
        published.SenderName.ShouldBe("Ada");
        published.SenderEmail.ShouldBe("ada@example.com");
        published.Body.ShouldBe("Hello");
        // The recipient travels with the event: who receives contact messages
        // is the site's decision, not the delivering module's.
        published.Recipient.ShouldBe("desk@acme.test");
    }

    [Fact]
    public async Task AFilledHoneypot_DropsTheSubmission()
    {
        var (handler, bus) = Create();

        await handler.Handle(Message(website: "http://spam.example"), CancellationToken.None);

        bus.Published.ShouldBeEmpty();
    }

    [Fact]
    public async Task AFilledHoneypot_StillSucceeds()
    {
        var (handler, _) = Create();

        // No exception: an error would tell whoever wrote the bot exactly
        // which field gave them away, and the next attempt would leave it
        // empty.
        await Should.NotThrowAsync(
            () => handler.Handle(Message(website: "x"), CancellationToken.None)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnEmptyHoneypot_IsARealVisitor(string website)
    {
        var (handler, bus) = Create();

        await handler.Handle(Message(website), CancellationToken.None);

        bus.Published.ShouldHaveSingleItem();
    }
}

public sealed class SendContactMessageCommandValidatorTests
{
    private static readonly SendContactMessageCommandValidator Validator = new();

    [Fact]
    public void AcceptsAWellFormedMessage()
    {
        var result = Validator.Validate(new SendContactMessageCommand("Ada", "ada@example.com", "Hi"));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "ada@example.com", "Hi")]
    [InlineData("Ada", "not-an-email", "Hi")]
    [InlineData("Ada", "ada@example.com", "")]
    public void RejectsMissingOrMalformedFields(string name, string email, string body)
    {
        var result = Validator.Validate(new SendContactMessageCommand(name, email, body));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RejectsABodyBeyondTheLimit()
    {
        // The endpoint is anonymous: without an upper bound a single request
        // can post a megabyte.
        var result = Validator.Validate(
            new SendContactMessageCommand("Ada", "ada@example.com", new string('x', 5001))
        );

        result.IsValid.ShouldBeFalse();
    }
}
