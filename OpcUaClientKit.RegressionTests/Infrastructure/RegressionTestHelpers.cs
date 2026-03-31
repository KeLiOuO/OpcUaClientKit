using System.Globalization;

namespace OpcUaClientKit.RegressionTests.Infrastructure;

internal static class RegressionTestHelpers
{
    public static async Task<T> WaitAsync<T>(Task<T> task, TimeSpan timeout, string timeoutMessage)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
        if (!ReferenceEquals(completedTask, task))
        {
            throw new TimeoutException(timeoutMessage);
        }

        return await task.ConfigureAwait(false);
    }

    public static async Task<T?> TryWaitAsync<T>(Task<T> task, TimeSpan timeout)
        where T : class
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
        if (!ReferenceEquals(completedTask, task))
        {
            return null;
        }

        return await task.ConfigureAwait(false);
    }

    public static short ToInt16(object? value, string nodeId)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"Node '{nodeId}' returned null.");
        }

        return Convert.ToInt16(value, CultureInfo.InvariantCulture);
    }

    public static double ToDouble(object? value, string context)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"{context} returned null.");
        }

        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }
}
