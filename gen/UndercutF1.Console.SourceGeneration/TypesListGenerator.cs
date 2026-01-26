using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UndercutF1.Console.SourceGeneration;

[Generator]
public class TypesListGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        RegisterImplementingTypes(context, "IInputHandler", "AddInputHandlersCore");
        RegisterImplementingTypes(context, "IDisplay", "AddDisplaysCore");
    }

    private static void RegisterImplementingTypes(
        IncrementalGeneratorInitializationContext context,
        string interfaceName,
        string methodName
    )
    {
        var typeNames = context
            .SyntaxProvider.CreateSyntaxProvider(
                static (sn, ct) => sn is ClassDeclarationSyntax,
                (ctx, _) =>
                {
                    var symbol = (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node)!;

                    return
                        !symbol.IsAbstract
                        && symbol.Interfaces.Any(x => x.MetadataName.EndsWith(interfaceName))
                        ? symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        : null;
                }
            )
            .Where(x => x is not null)
            .Collect();

        context.RegisterSourceOutput(
            typeNames,
            (context, model) =>
            {
                var sb = new StringBuilder();
                sb.Append(
                    $$"""
                    namespace UndercutF1.Console;
                    static partial class ServiceCollectionExtensions
                    {
                        private static IServiceCollection {{methodName}}(this IServiceCollection services)
                        {
                            
                    """
                );
                foreach (var s in model)
                {
                    sb.AppendLine($"services.AddSingleton<{s}>();");
                    sb.AppendLine(
                        $"services.AddSingleton<{interfaceName}>(sp => sp.GetRequiredService<{s}>());"
                    );
                }
                sb.Append(
                    """
                            return services;
                        }
                    }
                    """
                );
                var sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);

                context.AddSource($"ServiceCollectionExtensions_{interfaceName}.g.cs", sourceText);
            }
        );
    }
}
