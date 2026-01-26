using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace UndercutF1.Data;

public static class JsonSerializerContextExtensions
{
    /// <summary>
    /// Utility function to get a strongly typed <see cref="JsonTypeInfo"/> from the context.
    /// This method <b>WILL</b> throw at runtime if the type info cannot be found, so only used this
    /// if absolutely certain the type info is in this context.
    /// </summary>
    /// <typeparam name="T">The type being (de)serialized</typeparam>
    /// <param name="context">The <see cref="JsonSerializerContext"/> containing the <see cref="JsonTypeInfo"/></param>
    /// <returns>A strongly typed <see cref="JsonTypeInfo{T}"/></returns>
    public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerContext context) =>
        (JsonTypeInfo<T>)context.GetTypeInfo(typeof(T))!;
}
