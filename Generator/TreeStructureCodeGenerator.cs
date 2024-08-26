using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using GDDB.Editor;
using Newtonsoft.Json.Linq;

namespace GDDB.SourceGenerator
{
    [Generator]
    public class TreeStructureCodeGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor JsonParsingError = new ( 
                "GDDB001", 
                "Tree structure file parsing error", 
                "Error parsing GDDB type structure json {0}. Exception: {1}", 
                "Parsing",
                DiagnosticSeverity.Error,
                true );

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //throw new System.NotImplementedException();

            context.RegisterPostInitializationOutput( c =>
            {
                Console.WriteLine( $"[DemoSourceGenerator] RegisterPostInitializationOutput {DateTime.Now}" );
            } );
            var pipeline = context.AdditionalTextsProvider
                                  .Where(static (text) => text.Path.EndsWith("TreeStructure.json"))
                                  .Select(static (text, cancellationToken) =>
                                   {
                                       var name = Path.GetFileNameWithoutExtension(text.Path);

                                       Console.WriteLine( $"[DemoSourceGenerator] Processing file {text.Path}" );

                                       var code = text.GetText(cancellationToken)?.ToString();
                                       var path = text.Path;
                                       return (name, code, path);
                                   });

            context.RegisterSourceOutput(pipeline,
                    static (context, pair) => 
                    {
                        // #if DEBUG
                        // if (!Debugger.IsAttached)
                        // {
                        //     Debugger.Launch();
                        // }
                        // #endif 

                        Console.WriteLine( $"[DemoSourceGenerator] Parsing file {pair.name}" );

                        Category category;
                        try
                        {
                            var parser = new TreeStructureParser();
                            category = parser.ParseJson( pair.code );
                        }
                        catch ( Exception e )
                        {
                            context.ReportDiagnostic( Diagnostic.Create( JsonParsingError, null, pair.path, e.ToString() ) );    
                            return;
                        }
                        
                        Console.WriteLine( $"[DemoSourceGenerator] Generating code for {pair.name}" );
                        var emitter       = new CodeEmitter();
                        var allCategories = new List<Category>();
                        emitter.FlattenCategoriesTree( category, allCategories );
                        var categoryEnum = emitter.GenerateEnums( pair.path, category, allCategories );
                        context.AddSource( $"Categories.g.cs", SourceText.From( categoryEnum, Encoding.UTF8 ) );
                        var filterClasses = emitter.GenerateEnumerators( pair.path, category, allCategories );
                        context.AddSource( $"Filters.g.cs", SourceText.From( filterClasses, Encoding.UTF8 ) );
                        var gddbExtensions = emitter.GenerateGdDbExtensions( pair.path, category, allCategories );
                        context.AddSource( $"GdDbExtensions.g.cs", SourceText.From( gddbExtensions, Encoding.UTF8 ) );
                        var gdTypeExtensions = emitter.GenerateGdTypeExtensions( pair.path, category, allCategories );
                        context.AddSource( $"GdTypeExtensions.g.cs", SourceText.From( gdTypeExtensions, Encoding.UTF8 ) );

                        Console.WriteLine( $"[DemoSourceGenerator] Finished" );
                    } );
        }
      
    }

}