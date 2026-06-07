using System.Reflection;

namespace RabbitFlow.Tests.TestHelpers;

internal static class ReflectionHelper
{
    public static void SetPrivateField<T>(T instance, string fieldName, object? value)
    {
        var field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {typeof(T).Name}.");

        field.SetValue(instance, value);
    }
}
