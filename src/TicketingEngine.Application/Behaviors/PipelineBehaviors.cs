using System.Diagnostics;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TicketingEngine.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();
        var ctx      = new ValidationContext<TRequest>(request);
        var failures = _validators.Select(v => v.Validate(ctx))
            .SelectMany(r => r.Errors).Where(f => f is not null).ToList();
        if (failures.Count != 0) throw new ValidationException(failures);
        return await next();
    }
}

public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        _logger.LogInformation("Handling {Name} {@Request}", name, request);
        var sw  = Stopwatch.StartNew();
        var res = await next();
        sw.Stop();
        if (sw.ElapsedMilliseconds > 500)
            _logger.LogWarning("Slow request {Name} took {Ms}ms", name, sw.ElapsedMilliseconds);
        else
            _logger.LogInformation("Handled {Name} in {Ms}ms", name, sw.ElapsedMilliseconds);
        return res;
    }
}
