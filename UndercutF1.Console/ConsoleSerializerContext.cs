using System.Text.Json;
using System.Text.Json.Serialization;

namespace UndercutF1.Console;

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(MainDisplay.GitHubTagEntry))]
[JsonSerializable(typeof(MainDisplay.GitHubTagEntry[]))]
[JsonSerializable(typeof(ConsoleOptions))]
internal partial class ConsoleSerializerContext : JsonSerializerContext
{
    public static ConsoleSerializerContext Pretty { get; } =
        new(
            new(JsonSerializerDefaults.Web)
            {
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
                AllowTrailingCommas = true,
                WriteIndented = true,
            }
        );
}
