using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
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
                "Error building categories from json {0}. Exception: {1}.", 
                "Parsing",
                DiagnosticSeverity.Error,
                true );
        private static readonly DiagnosticDescriptor DebugInfo = new ( 
                "GDDB003", 
                "Just info", 
                "Just info", 
                "Info",
                DiagnosticSeverity.Info,
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
                        //DEBUG
                        context.ReportDiagnostic( Diagnostic.Create( DebugInfo, null ) );

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

                        var      serializer = new FoldersSerializer();
                        Folder rootFolder;
                        try
                        {
                            rootFolder = serializer.Deserialize( pair.Left.code );
                        }
                        catch ( JsonException e )
                        {
                            context.ReportDiagnostic( Diagnostic.Create( JsonParsingError, null, json.path, e.ToString() ) );    
                            return;
                        }
                        catch( Exception e ) when (e is not OperationCanceledException)
                        {
                            context.ReportDiagnostic( Diagnostic.Create( BuildCategoryError, null, json.path, e.ToString() ) );
                            return;
                        }
                        
                        Console.WriteLine( $"[DemoSourceGenerator] Generating code for {json.name}" );
                        var emitter       = new CodeEmitter();
                        //var allCategories = new List<Category>();
                        var allFolders = serializer.Flatten( rootFolder ).ToArray();
                        //var categoryEnum = emitter.GenerateEnums( json.path, rootFolder, allCategories );
                        //context.AddSource( $"Categories.g.cs", SourceText.From( categoryEnum, Encoding.UTF8 ) );
                        var foldersClasses = emitter.GenerateFolders( json.path, allFolders );
                        context.AddSource( "Folders.g.cs", SourceText.From( foldersClasses, Encoding.UTF8 ) );
                        var gddbPartial = emitter.GenerateGdDbPartial( json.path, rootFolder );
                        context.AddSource( "GdDb.partial.g.cs", SourceText.From( gddbPartial, Encoding.UTF8 ) );
                        //var gddbExtensions = emitter.GenerateGdDbExtensions( json.path, allFolders );
                        //context.AddSource( $"GdDbExtensions.g.cs", SourceText.From( gddbExtensions, Encoding.UTF8 ) );
                        //var gdTypeExtensions = emitter.GenerateGdTypeExtensions( json.path, rootFolder, allCategories );
                        //context.AddSource( $"GdTypeExtensions.g.cs", SourceText.From( gdTypeExtensions, Encoding.UTF8 ) );

                        Console.WriteLine( $"[DemoSourceGenerator] Finished" );
                    } );
        }
      
    }

}