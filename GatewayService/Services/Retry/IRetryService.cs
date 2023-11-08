namespace GatewayService.Services.Retry;

public interface IRetryService
{
    Task<T> RetryOnExceptionAsync<T>(int retries, TimeSpan delay, Func<Task<T>> operation);
}