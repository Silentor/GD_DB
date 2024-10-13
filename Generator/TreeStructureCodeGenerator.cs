﻿using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using GDDB.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GDDB.SourceGenerator
{
    [Generator]
    public class TreeStructureCodeGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor JsonParsingError = new ( 
                "GDDB001", 
                "Folders json parsing error", 
                "Error parsing GDDB type structure json {0}. Exception: {1}.", 
                "Parsing",
                DiagnosticSeverity.Error,
                true );
        private static readonly DiagnosticDescriptor FoldersDeserializingError = new ( 
                "GDDB002", 
                "Folders file parsing error", 
                "Error building categories from json {0}. Exception: {1}.", 
                "Parsing",
                DiagnosticSeverity.Error,
                true );
        private static readonly DiagnosticDescriptor CodeEmittingError = new ( 
                "GDDB003", 
                "Folders access code emitting error", 
                "Error emitting code from json {0}. Exception: {1}.", 
                "Parsing",
                DiagnosticSeverity.Error,
                true );

        // private static readonly DiagnosticDescriptor DebugInfo = new ( 
        //         "GDDB003", 
        //         "Just info", 
        //         "Just info", 
        //         "Info",
        //         DiagnosticSeverity.Info,
        //         true );

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
                        //context.ReportDiagnostic( Diagnostic.Create( DebugInfo, null ) );

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

                        var      foldersSerializer = new FoldersJsonSerializer();
                        Folder rootFolder;
                        UInt64? dataHash = null;
                        try
                        {
                            var foldersJObject        = JObject.Parse( pair.Left.code );
                            rootFolder = foldersSerializer.Deserialize( foldersJObject, FoldersJsonSerializer.IgnoreGDObjects, out dataHash );
                        }
                        catch ( JsonException e )
                        {
                            context.ReportDiagnostic( Diagnostic.Create( JsonParsingError, null, json.path, e.ToString() ) );    
                            return;
                        }
                        catch( Exception e ) when (e is not OperationCanceledException)
                        {
                            context.ReportDiagnostic( Diagnostic.Create( FoldersDeserializingError, null, json.path, e.ToString() ) );
                            return;
                        }
                        
                        Console.WriteLine( $"[DemoSourceGenerator] Generating code for {json.name}" );
                        var now = DateTime.Now;
                        var hash = dataHash ?? 0;
                        var emitter       = new CodeEmitter();
                        //var allCategories = new List<Category>();
                        var allFolders = rootFolder.EnumerateFoldersDFS(  ).ToArray();

                        try
                        {
                            var foldersClasses = emitter.GenerateFolders( json.path, hash, now, allFolders );
                            context.AddSource( "Folders.g.cs", SourceText.From( foldersClasses, Encoding.UTF8 ) );
                            var gddbPartial = emitter.GenerateGdDbPartial( json.path, hash, now, rootFolder );
                            context.AddSource( "GdDb.partial.g.cs", SourceText.From( gddbPartial, Encoding.UTF8 ) );
                        }
                        catch( Exception e ) when (e is not OperationCanceledException)
                        {
                            context.ReportDiagnostic( Diagnostic.Create( CodeEmittingError, null, json.path, e.ToString() ) );
                        }

                        Console.WriteLine( $"[DemoSourceGenerator] Finished" );
                    } );
        }
      
    }

}