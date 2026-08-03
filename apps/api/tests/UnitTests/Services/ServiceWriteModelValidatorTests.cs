using EnterpriseFramework.Modules.Services.Features;
using Shouldly;
using Xunit;

namespace EnterpriseFramework.UnitTests.Services;

/// <summary>
/// The conditional rules of the service form. They are the ones worth testing
/// because they are the ones a reader of the model cannot see: nothing about
/// two independent booleans and a nullable int says that one implies another.
/// </summary>
public sealed class ServiceWriteModelValidatorTests
{
    private static readonly ServiceWriteModelValidator Validator = new();

    private static ServiceWriteModel Model(
        int? durationMinutes = null,
        bool isBookable = false,
        decimal? price = null,
        string? currency = null,
        string? slug = null
    ) =>
        new(
            Title: "Consulenza strategica",
            Slug: slug,
            ShortDescription: "Un'ora per mettere a fuoco il problema.",
            Description: "Descrizione lunga.",
            DurationMinutes: durationMinutes,
            Price: price,
            Currency: currency,
            IsPublished: true,
            IsBookable: isBookable,
            SortOrder: 0
        );

    [Fact]
    public void RejectsABookableServiceWithoutADuration()
    {
        // The rule that matters most: a bookable service with no duration
        // produces a booking page with no slots on it, and the mistake is
        // invisible until someone tries to book.
        var result = Validator.Validate(Model(isBookable: true, durationMinutes: null));

        result.IsValid.ShouldBeFalse();
        result
            .Errors.ShouldContain(error =>
                error.PropertyName == nameof(ServiceWriteModel.DurationMinutes)
            );
    }

    [Fact]
    public void AcceptsABookableServiceWithADuration()
    {
        Validator.Validate(Model(isBookable: true, durationMinutes: 60)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AcceptsADescriptiveServiceWithoutADuration()
    {
        // Not every service is bookable: some are only told about.
        Validator.Validate(Model(isBookable: false, durationMinutes: null)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RejectsAPriceWithoutACurrency()
    {
        var result = Validator.Validate(Model(price: 120m, currency: null));

        result.IsValid.ShouldBeFalse();
        result
            .Errors.ShouldContain(error => error.PropertyName == nameof(ServiceWriteModel.Currency));
    }

    [Fact]
    public void AcceptsNoPriceAtAll()
    {
        Validator.Validate(Model(price: null, currency: null)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Consulenza Strategica")]
    [InlineData("consulenza strategica")]
    [InlineData("consulenza--strategica")]
    [InlineData("consulenza-")]
    public void RejectsASlugThatIsNotAlreadyNormalized(string slug)
    {
        // Typed slugs are used verbatim, so the form must refuse anything the
        // normalizer would have changed — otherwise what is stored differs
        // from what the administrator read back.
        Validator.Validate(Model(slug: slug)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void AcceptsAnEmptySlug()
    {
        // The normal case: no slug means "derive one from the title".
        Validator.Validate(Model(slug: null)).IsValid.ShouldBeTrue();
    }
}
