using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Newtonsoft.Json.Linq;

namespace GDDB.SourceGenerator
{
    [Generator]
    public class TreeStructureCodeGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor JsonParsingError = new ( 
                "GDDB001", 
                "Tree structure file parsing error", 
                "Some error parsing json {0}. Exception: {1}", 
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
                        Console.WriteLine( $"[DemoSourceGenerator] Parsing file {pair.name}" );

                        Parser.Category category;
                        try
                        {
                            category = Parser.ParseJson(pair.code);
                        }
                        catch ( Exception e )
                        {
                            context.ReportDiagnostic( Diagnostic.Create( JsonParsingError, null, pair.path, e.ToString() ) );    
                            throw;
                        }
                        
                        Console.WriteLine( $"[DemoSourceGenerator] Generating code for {pair.name}" );
                        var categoryEnum = Parser.GenerateEnums( pair.path, category );
                        context.AddSource( $"Categories.gen.cs", SourceText.From( categoryEnum, Encoding.UTF8 ) );
                        var filterClasses = Parser.GenerateFilters( pair.path, category );
                        context.AddSource( $"Filters.gen.cs", SourceText.From( filterClasses, Encoding.UTF8 ) );
                        Console.WriteLine( $"[DemoSourceGenerator] Writing file Categories.gen.cs" );
                    } );
        }
      
    }

}