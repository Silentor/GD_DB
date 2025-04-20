using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gddb.Editor
{
    public class GDAssetPatcher
    {
        private readonly        GDObject _gdObject;
        private const           String   ComponentsIdsRegexStr         = @"Components:\s*(?:- rid:\s*(-*\d+)\s*)*references:";
        private const           String   ComponentIdTypeRegexStr       = @"rid:\s*{id}\s*type:\s*\{class:\s*(?<class>[^,]*),\s*ns:\s*(?<ns>[^,]*),\s*asm:\s*(?<asm>[^\}]*)\}";
        private const           String   ComponentIdTypeReplacementStr = "rid: {id}\n      type: {class: {class}, ns: {ns}, asm: {asm}}";
        private const           String   ComponentTypeRegexStr         = @"type:\s*\{class:\s*{class},\s*ns:\s*{ns},\s*asm:\s*{asm}\}";
        private const           String   ComponentTypeReplacementStr   = "type: {class: {class}, ns: {ns}, asm: {asm}}";
        private static readonly Regex    ComponentIdsRegex             = new Regex( ComponentsIdsRegexStr, RegexOptions.Compiled );

        private readonly long[] _ids;

        public long[] ComponentIds => _ids;

        public GDAssetPatcher( GDObject gdObject )
        {
            _gdObject = gdObject;
            _ids     = GetComponentIds( );
        }

        public ComponentType GetComponentType( Int32 componentIndex )
        {
            if( componentIndex < 0 || componentIndex >= ComponentIds.Length )
                return ComponentType.NullRef;
            var componentId        = ComponentIds[componentIndex];
            if( componentId == ManagedReferenceUtility.RefIdNull || componentId == ManagedReferenceUtility.RefIdUnknown )
                return ComponentType.NullRef;

            var path               = AssetDatabase.GetAssetPath( _gdObject );
            var fileText           = System.IO.File.ReadAllText( path );
            var searchStr          = ComponentIdTypeRegexStr.Replace( "{id}", componentId.ToString( CultureInfo.InvariantCulture ) );
            var componentTypeRegex = new Regex( searchStr );
            var match              = componentTypeRegex.Match( fileText );
            if( match.Success )
            {
                return new ComponentType( match.Groups["class"].Value, match.Groups["ns"].Value, match.Groups["asm"].Value );
            }

            return ComponentType.NullRef;
        }

        public void ReplaceComponentType( Int32 componentIndex, ComponentType newType )
        {
            var oldType            = GetComponentType( componentIndex );
            var componentId        = ComponentIds[componentIndex];
            var path               = AssetDatabase.GetAssetPath( _gdObject );
            var fileText           = System.IO.File.ReadAllText( path );
            var componentIdStr     = componentId.ToString( CultureInfo.InvariantCulture );
            var searchStr          = ComponentIdTypeRegexStr.Replace( "{id}", componentIdStr );
            var replaceStr         = ComponentIdTypeReplacementStr.Replace( "{id}", componentIdStr ).Replace( "{class}", newType.Type ).Replace( "{ns}", newType.Namespace ).Replace( "{asm}", newType.Assembly );
            var componentTypeRegex = new Regex( searchStr );
            fileText              = componentTypeRegex.Replace( fileText, replaceStr );
            System.IO.File.WriteAllText( path, fileText );

            AssetDatabase.Refresh( ImportAssetOptions.ForceSynchronousImport );
            Debug.Log( $"[{nameof(GDAssetPatcher)}]-[{nameof(ReplaceComponentType)}] Replaced component type {oldType} for {newType} at GDObject asset {_gdObject.name}" );
        }

        public static void ReplaceComponentTypeEverywhere( ComponentType oldType, ComponentType newType )
        {
             var searchStr      = ComponentTypeRegexStr.Replace( "{class}", oldType.Type ).Replace( "{ns}", oldType.Namespace ).Replace( "{asm}", oldType.Assembly );
             var replaceStr      = ComponentTypeReplacementStr.Replace( "{class}", newType.Type ).Replace( "{ns}", newType.Namespace ).Replace( "{asm}", newType.Assembly );
             var allObjects = EditorDB.AllObjects;

             AssetDatabase.SaveAssets();
             AssetDatabase.Refresh( ImportAssetOptions.ForceSynchronousImport );
             AssetDatabase.StartAssetEditing();
             var counter = 0;

             try
             {
                 var progressbarDescription = $"Replace component type {oldType} with {newType}";   
                foreach( var gdObject in allObjects )
                {
                    var path               = AssetDatabase.GetAssetPath( gdObject );
                    var fileText           = System.IO.File.ReadAllText( path );
                    var componentTypeRegex = new Regex( searchStr );
                    var newFileText              = componentTypeRegex.Replace( fileText, replaceStr );
                    if( newFileText != fileText )
                    {
                        System.IO.File.WriteAllText( path, newFileText );
                        counter++;
                    }
                    var isCancelled = EditorUtility.DisplayCancelableProgressBar( "GDDB component fix", progressbarDescription, counter / (Single)allObjects.Count );
                    if( isCancelled )
                        break;
                }
             }
             finally
             {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh( ImportAssetOptions.ForceSynchronousImport );
                EditorUtility.ClearProgressBar();
                Debug.Log( $"[{nameof(GDAssetPatcher)}]-[{nameof(ReplaceComponentTypeEverywhere)}] Replaced component type {oldType} for {newType} at {counter} GDObject assets" );
             }
        }

        private long[] GetComponentIds( )
        {
            var path     = AssetDatabase.GetAssetPath( _gdObject );
            var fileText = System.IO.File.ReadAllText( path );
            var match = ComponentIdsRegex.Match( fileText );

            if( match.Success )
            {
                var result = new long[match.Groups[1].Captures.Count];
                for( var i = 0; i < match.Groups[1].Captures.Count; i++ )
                {
                    result[i] = long.Parse( match.Groups[1].Captures[i].Value );
                }

                return result;
            }

            return Array.Empty<long>();
        }

        public readonly struct ComponentType : IEquatable<ComponentType>
        {
            public readonly string Namespace;
            public readonly string Type;
            public readonly string Assembly;

            public static readonly ComponentType NullRef = new ComponentType( );

            public ComponentType(  String type, String ns, String assembly )
            {
                Namespace = ns;
                Type      = type;
                Assembly  = assembly;
            }

            public ComponentType( Type type )
            {
                Namespace = type.Namespace;
                Type      = type.Name;
                Assembly  = type.Assembly.GetName().Name;
            }

            public override String ToString( )
            {
                if( String.IsNullOrEmpty( Namespace ) )
                    return $"{Assembly}.{Type}";
                else
                    return $"{Assembly}.{Namespace}.{Type}";
            }

            public bool Equals(ComponentType other)
            {
                return Namespace == other.Namespace && Type == other.Type && Assembly == other.Assembly;
            }

            public override bool Equals(object obj)
            {
                return obj is ComponentType other && Equals( other );
            }

            public override int GetHashCode( )
            {
                return HashCode.Combine( Namespace, Type, Assembly );
            }

            public static bool operator ==(ComponentType left, ComponentType right)
            {
                return left.Equals( right );
            }

            public static bool operator !=(ComponentType left, ComponentType right)
            {
                return !left.Equals( right );
            }
        } 
    }
}