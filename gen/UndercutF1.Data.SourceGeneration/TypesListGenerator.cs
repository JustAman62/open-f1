using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UndercutF1.Data.SourceGeneration;

[Generator]
public class TypesListGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) =>
        AddProcessorTypesList(context);

    private static void AddProcessorTypesList(IncrementalGeneratorInitializationContext context)
    {
        var processorTypeNames = context
            .SyntaxProvider.CreateSyntaxProvider<Model?>(
                static (sn, ct) => sn is ClassDeclarationSyntax,
                (ctx, _) =>
                {
                    var symbol = (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node)!;
                    var interfaceType = symbol.AllInterfaces.FirstOrDefault(x =>
                        x is { IsGenericType: true, MetadataName: "IProcessor`1" }
                    );
                    var dataPointType = interfaceType?.TypeArguments.FirstOrDefault();

                    var symbolName = symbol.ToDisplayString(
                        SymbolDisplayFormat.MinimallyQualifiedFormat
                    );
                    var dataPointSymbolName = dataPointType?.ToDisplayString(
                        SymbolDisplayFormat.MinimallyQualifiedFormat
                    );

                    return
                        !symbol.IsAbstract
                        && interfaceType is not null
                        && dataPointSymbolName is not null
                        ? new Model(symbolName, dataPointSymbolName)
                        : null;
                }
            )
            .Where(x => x is not null)
            .Collect();

        context.RegisterSourceOutput(
            processorTypeNames,
            (context, models) =>
            {
                var sb = new StringBuilder();
                sb.Append(
                    $$"""
                    namespace UndercutF1.Data;
                    using Microsoft.Extensions.DependencyInjection;
                    static partial class ServiceCollectionExtensions
                    {
                        private static IServiceCollection AddLiveTimingProcessors(this IServiceCollection services)
                        {
                    """
                );
                foreach (var model in models)
                {
                    var (processorType, dataPointType) = model!.Value;
                    sb.AppendLine(
                        $"services.AddSingleton<IProcessor>(x => x.GetRequiredService<{processorType}>());"
                    );
                    sb.AppendLine(
                        $"services.AddSingleton<IProcessor<{dataPointType}>>(x => x.GetRequiredService<{processorType}>());"
                    );
                    sb.AppendLine($"services.AddSingleton<{processorType}>();");
                }
                sb.Append(
                    """
                            return services;
                        }
                    }
                    """
                );
                var sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);

                context.AddSource($"ServiceCollectionExtensions_ProcessorTypes.g.cs", sourceText);
            }
        );
    }

    private record struct Model(string ProcessorTypeName, string DataPointTypeName);
}
