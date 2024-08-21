using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StronglyTypedIds;

namespace GDDB.SourceGenerator
{
    [Generator]
    public class DemoSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<CategoryData?> enumsToGenerate = context.SyntaxProvider
                                                                             .ForAttributeWithMetadataName( // 👈 use the new API
                                                                                      "GDDB.CategoryAttribute", // 👈 The attribute to look for
                                                                                      //predicate: (node, _) => node is EnumDeclarationSyntax, // 👈 A basic predicate, may not be necessary
                                                                                      predicate: (_, _) => true,
                                                                                      transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)); // 👈 The original transform
        }

        private static CategoryData? GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext ctx )
        {
            ctx.
        }
    }

    public readonly record struct CategoryData
    {
        public readonly String                 Name;
        public readonly EquatableArray<String> Items;

        public CategoryData(String name, EquatableArray<String> items )
        {
            Name  = name;
            Items = items;
        }
    }
}