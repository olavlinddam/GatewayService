using Polly;
using Polly.CircuitBreaker;

namespace GatewayService.Services.Retry;

public class RetryService : IRetryService
{
    private AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    public RetryService()
    {
        _circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 1,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, duration) =>
                {
                    // Log on break
                    Console.WriteLine($"Circuit broken due to: {exception.Message}. Circuit will be broken for: {duration}.");
                },
                onReset: () =>
                {
                    // Log on reset
                    Console.WriteLine($"Circuit reset.");
                },
                onHalfOpen: () =>
                {
                    // Log on half-open
                    Console.WriteLine($"Circuit is half-open. Next call is a trial.");
                }
            );
    }

    public async Task<T> RetryOnExceptionAsync<T>(int retries, TimeSpan delay, Func<Task<T>> operation)
    {
        var retryPolicy = Policy
            .Handle<TimeoutException>()
            .WaitAndRetryAsync(retries, retryAttempt => delay, (exception, timeSpan, retryAttempt, context) =>
            {
                // Log each retry attempt here.
                Console.WriteLine($"Retry {retryAttempt} due to: {exception.Message}. Waiting {timeSpan} before next retry.");
            });

        // Wrap retry policy with circuit breaker policy
        var wrappedPolicy = Policy.WrapAsync(_circuitBreakerPolicy, retryPolicy);

        try
        {
            return await wrappedPolicy.ExecuteAsync(operation);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }
}