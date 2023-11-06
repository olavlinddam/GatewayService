using Polly;

namespace GatewayService.Services.Retry;

public class RetryService : IRetryService
{
    public async Task<T> RetryOnExceptionAsync<T>(int retries, TimeSpan delay, Func<Task<T>> operation)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(retries, retryAttempt => delay, (exception, timeSpan, retryAttempt, context) =>
            {
                // Log each retry attempt here.
                Console.WriteLine($"Retry {retryAttempt} due to: {exception.Message}. Waiting {timeSpan} before next retry.");
            });

        return await policy.ExecuteAsync(operation);
    }
}