using System.Globalization;
using System.Reflection;

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

    public static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' was not found on '{instance.GetType().FullName}'.");
        }

        return (T)field.GetValue(instance)!;
    }

    public static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' was not found on '{instance.GetType().FullName}'.");
        }

        field.SetValue(instance, value);
    }

    public static async Task InvokePrivateTaskAsync(object instance, string methodName, params object?[]? arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException($"Method '{methodName}' was not found on '{instance.GetType().FullName}'.");
        }

        try
        {
            if (method.Invoke(instance, arguments) is Task task)
            {
                await task.ConfigureAwait(false);
                return;
            }

            throw new InvalidOperationException($"Method '{methodName}' did not return a Task.");
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }
}
