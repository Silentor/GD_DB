using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;

namespace GDDB.Serialization
{
    public class DBDataSerializer
    {
        private readonly CustomSampler _deserializeSampler = CustomSampler.Create( $"{nameof(DBDataSerializer)}.{nameof(Deserialize)}" );
        private readonly CustomSampler _compressAnalyzeSampler = CustomSampler.Create( $"{nameof(DBDataSerializer)}.CompressAnalyze" );

#if UNITY_EDITOR
        public void Serialize( WriterBase writer, Folder rootFolder, IGdAssetResolver assetsResolver )
        {
            var hash  = rootFolder.GetFoldersChecksum();

            var timer = System.Diagnostics.Stopwatch.StartNew();

            var objectsSerializer = new GDObjectSerializer( writer );
            var folderSerializer  = new FolderSerializer( );

            List<SymbolData> typeSymbols    = null;
            var bWriter = writer as BinaryWriter;
            var              isCompressible = bWriter != null;
            //isCompressible = false;
            if ( isCompressible )
            {
                _compressAnalyzeSampler.Begin();
                //var compressTimer = timer.ElapsedMilliseconds;
                typeSymbols   = GetMostCommonSymbols( rootFolder, objectsSerializer ).Take( 100 ).ToList();       //Limit custom aliases to 100
                _compressAnalyzeSampler.End();
                // Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Serialize)}] Compressions analysis ended, time {timer.ElapsedMilliseconds - compressTimer} ms" );
                // Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Serialize)}] type symbols:" );
                // foreach ( var typeSymbol in typeSymbols )
                // {
                //     Debug.Log( $"{typeSymbol.Symbol}, count {typeSymbol.Count}, score {typeSymbol.Score}, token {typeSymbol.Token}" );
                // }

                //Set default aliases
                bWriter.SetAlias( 101, EToken.PropertyName, GDObjectSerializationCommon.NameTag );
                bWriter.SetAlias( 102, EToken.PropertyName, GDObjectSerializationCommon.IdTag );               
                bWriter.SetAlias( 103, EToken.PropertyName, FolderSerializer.FoldersTag );
                bWriter.SetAlias( 104, EToken.PropertyName, FolderSerializer.ObjectsTag );
                bWriter.SetAlias( 105, EToken.PropertyName, GDObjectSerializationCommon.TypeTag );
                bWriter.SetAlias( 106, EToken.PropertyName, GDObjectSerializationCommon.EnabledTag );
                bWriter.SetAlias( 107, EToken.PropertyName, GDObjectSerializationCommon.ComponentsTag );
                bWriter.SetAlias( 108, EToken.PropertyName, GDObjectSerializationCommon.LocalIdTag );
            }

            writer.WriteStartObject();
            writer.WritePropertyName( ".hash" );
            writer.WriteValue( hash );
            if ( isCompressible && typeSymbols != null && typeSymbols.Count > 0 )
            {
                var writeDictionaryTimer = timer.ElapsedMilliseconds;
                //Save aliases dictionary to output
                writer.WritePropertyName( ".aliases" );
                writer.WriteStartArray();
                for ( int i = 0; i < typeSymbols.Count; i++ )
                {
                    writer.WriteStartArray();
                    writer.WriteValue( (Byte)i );
                    writer.WriteValue( (Byte)typeSymbols[i].Token );
                    writer.WriteValue( typeSymbols[i].Symbol );
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
            
                //Actually set aliases for compression
                for ( int i = 0; i < typeSymbols.Count; i++ )
                {
                    bWriter.SetAlias( (Byte)i, typeSymbols[i].Token, typeSymbols[i].Symbol );
                    //Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Serialize)}] set alias id {i}, token {tokensToCompress[i].Token}, value '{tokensToCompress[i].Value}'" );
                }
            }

            var resultWriteTimer = timer.ElapsedMilliseconds;
            writer.WritePropertyName( ".folders" );
            folderSerializer.Serialize( rootFolder, objectsSerializer, writer );
            writer.WriteEndObject();
            //Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Serialize)}] Result write timer {timer.ElapsedMilliseconds - resultWriteTimer}" );

            timer.Stop();

            Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Serialize)}] serialized db to format {writer.GetType().Name}, objects {objectsSerializer.ObjectsWritten}, folders {rootFolder.EnumerateFoldersDFS(  ).Count()} referenced {assetsResolver.Count} assets, time {timer.ElapsedMilliseconds} ms" );
        }

#endif

        // public (Folder rootFolder, IReadOnlyList<GDObject> objects) Deserialize( String jsonString, IGdAssetResolver assetsResolver )
        // {
        //     return Deserialize( new StringReader( jsonString ), assetsResolver );
        // }
        //
        // public (Folder rootFolder, IReadOnlyList<GDObject> objects) Deserialize( Stream jsonFile, IGdAssetResolver assetsResolver )
        // {
        //     return Deserialize( new StreamReader( jsonFile, Encoding.UTF8, false, 1024, true ), assetsResolver );
        // }

        public (Folder rootFolder, IReadOnlyList<GDObject> objects) Deserialize( ReaderBase reader, IGdAssetResolver assetsResolver, out UInt64? hash )
        {
            _deserializeSampler.Begin();
            var       timer             = System.Diagnostics.Stopwatch.StartNew();
            var       objectsSerializer = new GDObjectDeserializer( reader );
            var       foldersSerializer = new FolderSerializer();

            //Set default aliases
            if ( reader is BinaryReader bReader )
            {
                bReader.SetAlias( 101, EToken.PropertyName, GDObjectSerializationCommon.NameTag );
                bReader.SetAlias( 102, EToken.PropertyName, GDObjectSerializationCommon.IdTag );
                bReader.SetAlias( 103, EToken.PropertyName, FolderSerializer.FoldersTag );
                bReader.SetAlias( 104, EToken.PropertyName, FolderSerializer.ObjectsTag );
                bReader.SetAlias( 105, EToken.PropertyName, GDObjectSerializationCommon.TypeTag );
                bReader.SetAlias( 106, EToken.PropertyName, GDObjectSerializationCommon.EnabledTag );
                bReader.SetAlias( 107, EToken.PropertyName, GDObjectSerializationCommon.ComponentsTag );
                bReader.SetAlias( 108, EToken.PropertyName, GDObjectSerializationCommon.LocalIdTag );
            }
            else 
                bReader = null;

            reader.ReadStartObject();
            var propName = reader.ReadPropertyName();
            if ( propName == ".version" )        //Not implemented for now
            {
                reader.SkipProperty(  );
                propName = reader.ReadPropertyName();
            }

            hash = null;
            if ( propName == ".hash" )
            {
                hash = reader.ReadUInt64Value( );
                propName = reader.ReadPropertyName();
            }

            if( propName == ".aliases" && bReader != null )              //Read compression aliases if data is compressed
            {
                var aliases = new List<(Byte, SymbolData)>();
                reader.ReadStartArray();
                while ( reader.ReadNextToken() != EToken.EndArray )
                {
                    reader.EnsureStartArray();
                    var id    = reader.ReadUInt8Value();
                    var token = (EToken)reader.ReadUInt8Value();
                    var value = reader.ReadStringValue();
                    reader.ReadEndArray();
                    aliases.Add( (id, new SymbolData() { Token = token, Symbol = value } ));
                }
                reader.EnsureEndArray();

                foreach ( var alias in aliases )
                    bReader.SetAlias( alias.Item1, alias.Item2.Token, alias.Item2.Symbol );

                propName = reader.ReadPropertyName();
            }

            //Can be another properties in the future...

            reader.SeekPropertyName( ".folders" );
            var       rootFolder        = foldersSerializer.Deserialize( reader, objectsSerializer );
            reader.ReadEndObject();

            objectsSerializer.ResolveGDObjectReferences();

            timer.Stop();
            Debug.Log( $"[{nameof(DBDataSerializer)}]-[{nameof(Deserialize)}] deserialized db from {reader.GetType().Name}, objects {objectsSerializer.LoadedObjects.Count}, referenced {assetsResolver.Count} assets, time {timer.ElapsedMilliseconds} ms" );
            _deserializeSampler.End();

            return (rootFolder, objectsSerializer.LoadedObjects);
        }

        /// <summary>
        /// For now we just inspect GDObject/GDComponent types, count assembly, namespace, type name and field names of most common types
        /// </summary>
        /// <param name="rootFolder"></param>
        /// <param name="objectsSerializer"></param>
        /// <returns></returns>
        private List<SymbolData> GetMostCommonSymbols( Folder rootFolder, GDObjectSerializer objectsSerializer )
        {
            var assembliesCounter = new Dictionary<Assembly, SymbolData>();
            var nsCounter = new Dictionary<String, SymbolData>();
            var typesCounter = new Dictionary<String, SymbolData>();
            var fieldsCounter = new Dictionary<String, SymbolData>();
            foreach ( var folder in rootFolder.EnumerateFoldersDFS() )
            {
                foreach ( var gdObject in folder.Objects )
                {
                    var gdObjectType = gdObject.GetType();
                    if( gdObjectType != typeof(GDObject) )                          //We do not serialize GDObject type in folders
                        ProcessType( gdObjectType );
                    foreach ( var gdComponent in gdObject.Components )
                    {
                        ProcessType( gdComponent.GetType() );                        
                    }
                }
            }

            var result = new List<SymbolData>( assembliesCounter.Count + typesCounter.Count + nsCounter.Count + fieldsCounter.Count );
            foreach ( var typeStats in typesCounter.Values )
            {
                if ( typeStats.Count >= 2 )
                {
                    var typeStatCopy = new SymbolData { Symbol = typeStats.Symbol, Count = typeStats.Count, Score = typeStats.Symbol.Length * typeStats.Count, Token = EToken.String };
                    AddWithAccumulation( typeStatCopy );
                }
            }

            foreach ( var fieldStat in fieldsCounter.Values )
            {
                var fieldStatCopy = new SymbolData { Symbol = fieldStat.Symbol, Count = fieldStat.Count, Score = fieldStat.Symbol.Length * fieldStat.Count, Token = EToken.PropertyName };
                result.Add( fieldStatCopy );
            }

            foreach ( var asmStat in assembliesCounter.Values )
            {
                if( asmStat.Count >= 2 )
                {
                    var asmStatCopy = new SymbolData { Symbol = asmStat.Symbol, Count = asmStat.Count, Score = asmStat.Symbol.Length * asmStat.Count, Token = EToken.String };
                    AddWithAccumulation( asmStatCopy );
                }
            }

            foreach ( var nsStat in nsCounter.Values )
            {
                if( nsStat.Count >= 2 )
                {
                    var nsStatCopy = new SymbolData { Symbol = nsStat.Symbol, Count = nsStat.Count, Score = nsStat.Symbol.Length * nsStat.Count, Token = EToken.String };
                    AddWithAccumulation( nsStatCopy );
                }
            }

            result.Sort( ( a, b ) => b.Score.CompareTo( a.Score ) );
            return result;

            void ProcessType( Type type )
            {
                if ( assembliesCounter.TryGetValue( type.Assembly, out var assemblyStatistics ) )
                    assemblyStatistics.Count++;
                else
                    assembliesCounter[ type.Assembly ] = new SymbolData { Symbol = type.Assembly.GetName().Name, Count = 1 };
                if ( type.Namespace != null )
                {
                    if( nsCounter.TryGetValue( type.Namespace, out var nsStatistics ) )
                        nsStatistics.Count++;
                    else 
                        nsCounter[type.Namespace] = new SymbolData { Symbol = type.Namespace, Count = 1 };
                }
                if ( typesCounter.TryGetValue( type.Name, out var typeStatistics ) )
                {
                    typeStatistics.Count++;
                    if ( typeStatistics.Count >= 5 )     //Type looks promising, lets add field names to compress symbol table
                    {
                        var fields = objectsSerializer.GetSerializableFields( type );
                        foreach ( var field in fields )
                        {
                            if ( fieldsCounter.TryGetValue( field.Name, out var fieldStatistics ) )
                                fieldStatistics.Count++;
                            else
                                fieldsCounter[field.Name] = new SymbolData { Symbol = field.Name, Count = 5 };                                
                        }
                    }
                }
                else
                    typesCounter[type.Name] = new SymbolData { Symbol = type.Name, Count = 1 };
            }

            void AddWithAccumulation( SymbolData symbolData )
            {
                var existingIndex = result.FindIndex( s => s.Symbol == symbolData.Symbol );
                if( existingIndex >= 0 )
                {
                    var existing = result[existingIndex];
                    existing.Count += symbolData.Count;
                }
                else
                    result.Add( symbolData );
            }
        }

        private class SymbolData
        {
            public String Symbol;
            public Int32  Count;
            public Int32  Score;
            public EToken Token;
        }
    }
}