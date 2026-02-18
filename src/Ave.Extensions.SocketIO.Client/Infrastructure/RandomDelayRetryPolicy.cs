using System;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Infrastructure;

/// <summary>
/// Retry policy with random delay between attempts.
/// </summary>
public class RandomDelayRetryPolicy : IRetriable
{
    private readonly IRandom _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomDelayRetryPolicy"/> class.
    /// </summary>
    public RandomDelayRetryPolicy(IRandom random)
    {
        _random = random;
    }

    /// <inheritdoc />
    public async Task RetryAsync(int times, Func<Task> func)
    {
        if (times < 1)
        {
            throw new ArgumentException("Times must be greater than 0", nameof(times));
        }
        for (var i = 1; i < times; i++)
        {
            try
            {
                await func().ConfigureAwait(false);
                return;
            }
            catch
            {
                await Task.Delay(_random.Next(3)).ConfigureAwait(false);
            }
        }
        await func().ConfigureAwait(false);
    }
}
