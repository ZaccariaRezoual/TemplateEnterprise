using FluentValidation;
using MediatR;
using ValidationException = EnterpriseFramework.Application.Exceptions.ValidationException;

namespace EnterpriseFramework.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs every FluentValidation validator
/// registered for the incoming request BEFORE its handler executes.
///
/// On failure it throws <see cref="ValidationException"/> (mapped to HTTP 400
/// by the API layer), so handlers can assume their input is always valid and
/// never re-validate.
/// </summary>
/// <typeparam name="TRequest">MediatR request type.</typeparam>
/// <typeparam name="TResponse">Handler response type.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes the behavior with every validator registered for <typeparamref name="TRequest"/>.
    /// </summary>
    /// <param name="validators">Validators discovered via DI (may be empty).</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Validates the request, then invokes the next step of the pipeline.
    /// </summary>
    /// <param name="request">Incoming request.</param>
    /// <param name="next">Continuation to the handler (or next behavior).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The handler response when validation passes.</returns>
    /// <exception cref="ValidationException">Thrown when at least one rule fails.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (!_validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))
            )
            .ConfigureAwait(false);

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next().ConfigureAwait(false);
    }
}
