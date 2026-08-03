using EnterpriseFramework.Modules.Services.Contracts.Events;
using EnterpriseFramework.Modules.Services.Domain;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Services;

/// <summary>
/// Which public event a change produces. The rule is not obvious from the
/// fields — it depends on where the service WAS — and other modules build
/// their projections from these events, so getting it wrong is silent.
/// </summary>
public sealed class ServiceTests
{
    private static ServiceState State(
        bool isPublished = true,
        bool isBookable = false,
        int? durationMinutes = null,
        string slug = "consulenza"
    ) =>
        new(
            Title: "Consulenza",
            Slug: slug,
            ShortDescription: "Breve.",
            Description: "Lunga.",
            DurationMinutes: durationMinutes,
            Price: null,
            Currency: null,
            IsPublished: isPublished,
            IsBookable: isBookable,
            SortOrder: 0
        );

    [Fact]
    public void ADraftAnnouncesNothing()
    {
        // Nobody outside may learn that a draft exists: a booking link could
        // otherwise be built for a page the public cannot open.
        var service = Service.Create(State(isPublished: false));

        service.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void CreatingItPublishedAnnouncesThePublication()
    {
        var service = Service.Create(State(isPublished: true));

        service.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ServicePublished>();
    }

    [Fact]
    public void ADraftBecomingVisibleAnnouncesThePublication()
    {
        var service = Service.Create(State(isPublished: false));
        service.ClearDomainEvents();

        service.Update(State(isPublished: true));

        service.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ServicePublished>();
    }

    [Fact]
    public void EditingADraftIntoAnotherDraftStillAnnouncesNothing()
    {
        var service = Service.Create(State(isPublished: false));
        service.ClearDomainEvents();

        service.Update(State(isPublished: false, slug: "consulenza-b"));

        service.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void WithdrawingAPublishedServiceAnnouncesAnUpdateThatStopsBookings()
    {
        // There is no "unpublished" event on purpose: subscribers handle one
        // update event and cannot forget the case that matters most.
        var service = Service.Create(State(isPublished: true, isBookable: true, durationMinutes: 60));
        service.ClearDomainEvents();

        service.Update(State(isPublished: false, isBookable: true, durationMinutes: 60));

        var updated = service.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ServiceUpdated>();
        updated.IsBookable.ShouldBeFalse();
    }

    [Fact]
    public void ArchivingAnnouncesItOnceAndThenStaysSilent()
    {
        var service = Service.Create(State());
        service.ClearDomainEvents();

        service.Archive();
        service.Archive();

        service.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ServiceArchived>();
    }

    [Fact]
    public void AnArchivedServiceStopsAcceptingBookings()
    {
        var service = Service.Create(State(isBookable: true, durationMinutes: 30));
        service.AcceptsBookings.ShouldBeTrue();

        service.Archive();

        service.AcceptsBookings.ShouldBeFalse();
    }

    [Fact]
    public void TheFirstImageBecomesTheCover()
    {
        var service = Service.Create(State());

        var first = service.AddImage(Guid.NewGuid(), "Sala riunioni", 0);
        var second = service.AddImage(Guid.NewGuid(), "Scrivania", 1);

        first.IsCover.ShouldBeTrue();
        second.IsCover.ShouldBeFalse();
    }

    [Fact]
    public void RemovingTheCoverPromotesTheNextImage()
    {
        var service = Service.Create(State());
        var first = service.AddImage(Guid.NewGuid(), "Sala riunioni", 0);
        var second = service.AddImage(Guid.NewGuid(), "Scrivania", 1);

        service.RemoveImage(first.Id);

        // A card with no thumbnail reads as broken, so the gallery is never
        // left without a cover while it still has images.
        second.IsCover.ShouldBeTrue();
    }

    [Fact]
    public void ChoosingACoverDemotesThePreviousOne()
    {
        var service = Service.Create(State());
        var first = service.AddImage(Guid.NewGuid(), "Sala riunioni", 0);
        var second = service.AddImage(Guid.NewGuid(), "Scrivania", 1);

        service.SetCover(second.Id).ShouldBeTrue();

        first.IsCover.ShouldBeFalse();
        second.IsCover.ShouldBeTrue();
    }
}
