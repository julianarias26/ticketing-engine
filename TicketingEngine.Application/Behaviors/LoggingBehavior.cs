using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TicketingEngine.Application.Behaviors
{
    public sealed class LoggingBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
            => _logger = logger;

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            var name = typeof(TRequest).Name;
            _logger.LogInformation("Handling {CommandName}: {@Command}", name, request);

            var sw = Stopwatch.StartNew();
            var response = await next();
            sw.Stop();

            _logger.LogInformation(
                "Handled {CommandName} in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);

            return response;
        }
    }
}
