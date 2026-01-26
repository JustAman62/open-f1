using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UndercutF1.Data.SourceGeneration;

[Generator]
public class MergeWithGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static postInitializationContext =>
        {
            postInitializationContext.AddEmbeddedAttributeDefinition();
            postInitializationContext.AddSource(
                "MergeableAttribute.cs",
                SourceText.From(
                    """
                    using System;
                    using Microsoft.CodeAnalysis;

                    namespace UndercutF1.Data;

                    public interface IMergeable<T>
                    {
                        T MergeWith(T dest);
                    }

                    [AttributeUsage(AttributeTargets.Class), Embedded]
                    internal sealed class MergeableAttribute : Attribute
                    {
                        public bool IgnoreRoot { get; set; } = false;
                    }

                    [AttributeUsage(AttributeTargets.Property), Embedded]
                    internal sealed class MergeableIgnoreAttribute : Attribute { }
                    """,
                    Encoding.UTF8
                )
            );
        });

        var classes = context.SyntaxProvider.ForAttributeWithMetadataName(
            "UndercutF1.Data.MergeableAttribute",
            (sn, _ct) => sn is RecordDeclarationSyntax or ClassDeclarationSyntax,
            (ctx, _) =>
            {
                var symbol = (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(ctx.TargetNode)!;
                var model = GetMergeableClass(symbol);
                model.IsDictionary =
                    symbol
                        .BaseType?.ToDisplayString()
                        .StartsWith("System.Collections.Generic.Dictionary")
                    ?? false;
                model.Comment = symbol.BaseType?.ToDisplayString() ?? "null";
                return model;
            }
        );

        context.RegisterSourceOutput(
            classes,
            (context, model) =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("#nullable enable");
                sb.AppendLine("namespace UndercutF1.Data;");
                GenerateMergeWith(sb, model);
                sb.AppendLine("#nullable disable");
                var sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);

                context.AddSource($"{model.ModelName}_IMergeable.g.cs", sourceText);
            }
        );
    }

    private Model GetMergeableClass(INamedTypeSymbol symbol)
    {
        var properties = symbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x =>
                x
                    is {
                        Kind: SymbolKind.Property,
                        SetMethod: not null,
                        DeclaredAccessibility: Accessibility.Public,
                    }
                && !x.GetAttributes()
                    .Any(x => x.AttributeClass is { Name: "MergeableIgnoreAttribute" })
            )
            .Select(x => new Property(
                x.Name,
                x.Type.IsReferenceType && x.Type.NullableAnnotation == NullableAnnotation.Annotated
            ))
            .ToList();

        var innerTypes = symbol
            .GetTypeMembers()
            .Where(x => x.IsRecord)
            .Select(GetMergeableClass)
            .ToList();

        return new(symbol.Name, properties, innerTypes);
    }

    private void GenerateMergeWith(StringBuilder sb, Model model)
    {
        if (model.IsDictionary)
        {
            sb.AppendLine(
                $$"""
                // {{model.Comment}}
                partial class {{model.ModelName}}
                {
                """
            );
        }
        else
        {
            sb.AppendLine(
                $$"""
                // {{model.Comment}}
                partial record {{model.ModelName}} : IMergeable<{{model.ModelName}}>
                {
                """
            );

            sb.AppendLine(
                $$"""
                    public {{model.ModelName}} MergeWith({{model.ModelName}}? other)
                    {
                        if (other is null) return this;
                """
            );
            foreach (var (propertyName, _) in model.Properties)
            {
                sb.AppendLine(
                    $$"""
                            this.{{propertyName}} = this.{{propertyName}}.MergeWith(other.{{propertyName}});
                    """
                );
            }
            sb.AppendLine(
                """
                        return this;
                    }
                """
            );
        }

        foreach (var innerType in model.InnerTypes)
        {
            GenerateMergeWith(sb, innerType);
        }

        sb.AppendLine(
            """
            }
            """
        );
    }

    private record struct Model(string ModelName, List<Property> Properties, List<Model> InnerTypes)
    {
        public bool IsDictionary { get; set; } = false;
        public string Comment { get; set; } = string.Empty;
    }

    private record struct Property(string Name, bool NullableReferenceType);
}
