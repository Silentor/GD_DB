using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using GDDB.Editor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GDDB.SourceGenerator
{
    [Generator]
    public class TreeStructureCodeGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor JsonParsingError = new ( 
                "GDDB001", 
                "Tree structure json parsing error", 
                "Error parsing GDDB type structure json {0}. Exception: {1}.", 
                "Parsing",
                DiagnosticSeverity.Error,
                true );
        private static readonly DiagnosticDescriptor BuildCategoryError = new ( 
                "GDDB002", 
                "Tree structure file parsing error", 
                "Error error building categories from json {0}. Exception: {1}.", 
                "Parsing",
                DiagnosticSeverity.Error,
                true );

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput( c =>
            {
                Console.WriteLine( $"[DemoSourceGenerator] RegisterPostInitializationOutput {DateTime.Now}" );
            } );
            var compilations = context.CompilationProvider.Select( (cmp, cancel) => cmp.AssemblyName );
            var jsonFileData = context.AdditionalTextsProvider
                                  .Where(static (text) => text.Path.EndsWith("TreeStructure.json"))
                                  .Select(static (text, cancellationToken) =>
                                   {
                                       var name = Path.GetFileNameWithoutExtension(text.Path);

                                       Console.WriteLine( $"[DemoSourceGenerator] Processing file {text.Path}..." );

                                       var code = text.GetText(cancellationToken)?.ToString();

                                       if( code ==  null )
                                           Console.WriteLine( $"[DemoSourceGenerator] Error reading file, source gen will be skipped" );

                                       var path = text.Path;
                                       return (name, code, path);
                                   }).Where( static json => json.code != null );

            var compilationAndJson = jsonFileData.Combine( compilations );
            
            context.RegisterSourceOutput(compilationAndJson,
                    static (context, pair) => 
                    {
                        // #if DEBUG
                        // if (!Debugger.IsAttached)                                            !
                        // {
                        //     Debugger.Launch();
                        // }
                        // #endif 

                        //Console.WriteLine( $"[DemoSourceGenerator] Parsing file {pair.name}, compilation {context.}" );

                        //Generate code for GDDB assembly only, because this code heavily depends on gddb types
                        var assemblyName = pair.Right;
                        if( assemblyName == null || assemblyName != "GDDB" )
                            return;

                        var json = pair.Left;

                        Category category;
                        try
                        {
                            var parser = new TreeStructureParser();
                            category = parser.ParseJson( json.code!, CancellationToken.None );
                        }
                        catch ( JsonException e )
                        {
                            context.ReportDiagnostic( Diagnostic.Create( JsonParsingError, null, json.path, e.ToString() ) );    
                            throw;
                        }
                        catch( Exception e ) when (e is not OperationCanceledException)
                        {
                            context.ReportDiagnostic( Diagnostic.Create( BuildCategoryError, null, json.path, e.ToString() ) );
                            throw;
                        }
                        
                        Console.WriteLine( $"[DemoSourceGenerator] Generating code for {json.name}" );
                        var emitter       = new CodeEmitter();
                        var allCategories = new List<Category>();
                        emitter.FlattenCategoriesTree( category, allCategories );
                        var categoryEnum = emitter.GenerateEnums( json.path, category, allCategories );
                        context.AddSource( $"Categories.g.cs", SourceText.From( categoryEnum, Encoding.UTF8 ) );
                        var filterClasses = emitter.GenerateEnumerators( json.path, category, allCategories );
                        context.AddSource( $"Enumerators.g.cs", SourceText.From( filterClasses, Encoding.UTF8 ) );
                        var gddbExtensions = emitter.GenerateGdDbExtensions( json.path, category, allCategories );
                        context.AddSource( $"GdDbExtensions.g.cs", SourceText.From( gddbExtensions, Encoding.UTF8 ) );
                        var gdTypeExtensions = emitter.GenerateGdTypeExtensions( json.path, category, allCategories );
                        context.AddSource( $"GdTypeExtensions.g.cs", SourceText.From( gdTypeExtensions, Encoding.UTF8 ) );

                        Console.WriteLine( $"[DemoSourceGenerator] Finished" );
                    } );
        }
      
    }

}