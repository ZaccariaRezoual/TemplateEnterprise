using EnterpriseFramework.Application.Behaviors;
using FluentValidation;
using MediatR;
using Shouldly;
using Xunit;
using ValidationException = EnterpriseFramework.Application.Exceptions.ValidationException;

namespace EnterpriseFramework.UnitTests.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithoutValidators_InvokesHandler()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);

        var result = await behavior.Handle(
            new TestRequest(""),
            () => Task.FromResult("handled"),
            CancellationToken.None
        );

        result.ShouldBe("handled");
    }

    [Fact]
    public async Task Handle_WithValidRequest_InvokesHandler()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);

        var result = await behavior.Handle(
            new TestRequest("valid"),
            () => Task.FromResult("handled"),
            CancellationToken.None
        );

        result.ShouldBe("handled");
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ThrowsValidationExceptionWithFieldErrors()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);
        var handlerInvoked = false;

        var act = () =>
            behavior.Handle(
                new TestRequest(""),
                () =>
                {
                    handlerInvoked = true;
                    return Task.FromResult("handled");
                },
                CancellationToken.None
            );

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ContainsKey(nameof(TestRequest.Text)).ShouldBeTrue();
        handlerInvoked.ShouldBeFalse();
    }

    private sealed record TestRequest(string Text) : IRequest<string>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(r => r.Text).NotEmpty();
        }
    }
}
