using System.Reflection;

namespace OpcUaClientKit.UnitTests.Infrastructure;

internal static class ReflectionTestHelpers
{
    public static object? InvokePrivateStatic(
        Type type,
        string methodName,
        params object?[]? arguments)
    {
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        try
        {
            return method.Invoke(null, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }

    public static object? InvokePrivateInstance(
        object instance,
        string methodName,
        params object?[]? arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        try
        {
            return method.Invoke(instance, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }

    public static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (T)field.GetValue(instance)!;
    }

    public static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field.SetValue(instance, value);
    }

    public static T CreateNonPublic<T>(params object?[]? arguments)
    {
        var instance = Activator.CreateInstance(
            typeof(T),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: arguments,
            culture: null);

        Assert.NotNull(instance);
        return (T)instance!;
    }
}
