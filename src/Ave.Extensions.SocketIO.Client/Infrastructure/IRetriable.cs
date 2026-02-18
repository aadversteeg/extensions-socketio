using System;
using System.Threading.Tasks;

namespace Ave.Extensions.SocketIO.Client.Infrastructure;

/// <summary>
/// Abstracts retry logic.
/// </summary>
public interface IRetriable
{
    /// <summary>
    /// Retries the specified function up to a given number of times.
    /// </summary>
    Task RetryAsync(int times, Func<Task> func);
}
