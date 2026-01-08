using System.Text.Json;
using Json.More;
using Json.Schema;
using Json.Schema.Generation;
using UndercutF1.Console;
using UndercutF1.Data;

namespace UndercutF1.CodeGenerator;

/// <summary>
/// Generates JSON Schema files for UndercutF1 configuration
/// </summary>
public sealed class JsonSchemaGenerator
{
    private static readonly JsonSerializerOptions _serializerOptions = new(
        JsonSerializerDefaults.Web
    );

    /// <summary>
    /// Generates JSON Schema files for UndercutF1 configuration
    /// </summary>
    public async Task GenerateOptionsSchemaAsync()
    {
        var repositoryRoot = Environment.ProcessPath;
        if (string.IsNullOrEmpty(repositoryRoot))
        {
            throw new InvalidOperationException("Unable to determine the path of the executable");
        }

        repositoryRoot = Directory.GetParent(repositoryRoot)!.ToString();
        repositoryRoot = Path.Join(repositoryRoot, "../../../../..");

        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase,
        };
        config.RegisterXmlCommentFile<LiveTimingOptions>(
            Path.Join(repositoryRoot, "UndercutF1.Data/bin/Debug/net9.0/UndercutF1.Data.xml")
        );
        config.RegisterXmlCommentFile<ConsoleOptions>(
            Path.Join(repositoryRoot, "UndercutF1.Console/bin/Debug/net9.0/undercutf1.xml")
        );

        var schemaBuilder = new JsonSchemaBuilder();
        var schema = schemaBuilder.FromType<ConsoleOptions>(config).Build();

        var schemaString = JsonSerializer.Serialize(schema.ToJsonDocument(), _serializerOptions);

        var outputPath = Path.Join(repositoryRoot, "config.schema.json");
        outputPath = Path.GetFullPath(outputPath);

        await Terminal.OutLineAsync($"Writing config JSON Schema to {outputPath}");

        await File.WriteAllTextAsync(outputPath, schemaString);
    }
}
