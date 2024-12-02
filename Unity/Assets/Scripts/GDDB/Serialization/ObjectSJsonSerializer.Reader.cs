using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Object = System.Object;

namespace GDDB.Serialization
{
    //Reader part of GDJson
    public partial class ObjectsJsonSerializer
    {
        private readonly List<UnresolvedGDObjectReference> _unresolvedReferences = new ();
        private readonly List<GDObject>                    _loadedObjects        = new ();     //To resolve loaded references in place
        private readonly CustomSampler _deserObjectSampler     = CustomSampler.Create( "ObjectsJsonSerializer.Deserialize" );
        private readonly CustomSampler _resolveObjectSampler   = CustomSampler.Create( "ObjectsJsonSerializer.ResolveGDObjectReferences" );

#region JSON-LINQ reader

        public IReadOnlyList<GDObject> LoadedObjects => _loadedObjects;

        public GDObject Deserialize( String json, IGdAssetResolver assetResolver = null )
        {
            return Deserialize( JSONNode.Parse( json ).AsObject, assetResolver );
        }

        public GDObject Deserialize( JsonReader json, IGdAssetResolver assetResolver = null )
        {
            _assetResolver    = assetResolver ?? NullGdAssetResolver.Instance;

            _deserObjectSampler.Begin();
            var result = ReadGDObjectFromJson( json );
            _deserObjectSampler.End();

            if( result is ISerializationCallbackReceiver serializationCallbackReceiver)
                serializationCallbackReceiver.OnAfterDeserialize();

            foreach ( var gdComponent in result.Components )
                if( gdComponent is ISerializationCallbackReceiver componentSerializationCallbackReceiver )
                    componentSerializationCallbackReceiver.OnAfterDeserialize();

            _loadedObjects.Add( result );
            return result;
        }

        public void ResolveGDObjectReferences( )
        {
            _resolveObjectSampler.Begin();
            foreach ( var unresolvedReference in _unresolvedReferences )
            {
                if( unresolvedReference.Guids != null )                //Collection target field
                {
                    if ( unresolvedReference.Field.FieldType.IsArray )
                    {
                        var result = Array.CreateInstance( unresolvedReference.Field.FieldType.GetElementType(), unresolvedReference.Guids.Length );
                        for ( int i = 0; i < unresolvedReference.Guids.Length; i++ )
                        {
                            var guid           = unresolvedReference.Guids[i];
                            var resolvedObject = _loadedObjects.FirstOrDefault( gdo => gdo.Guid == guid );
                            if ( resolvedObject )
                            {
                                result.SetValue( resolvedObject, i );
                            }
                            else
                            {
                                Debug.LogError( $"[{nameof(ObjectsJsonSerializer)}]-[{nameof(ResolveGDObjectReferences)}] cannot resolve GDObject reference {guid}. Target object {unresolvedReference.TargetObject.GetType().Name}, field {unresolvedReference.Field}" );
                            }
                        }
                        unresolvedReference.Field.SetValue( unresolvedReference.TargetObject, result );
                    }
                    else
                    {
                        var result = (IList)Activator.CreateInstance( unresolvedReference.Field.FieldType );
                        for ( int i = 0; i < unresolvedReference.Guids.Length; i++ )
                        {
                            var guid           = unresolvedReference.Guids[i];
                            var resolvedObject = _loadedObjects.FirstOrDefault( gdo => gdo.Guid == guid );
                            if ( resolvedObject )
                            {
                                result.Add( resolvedObject );
                            }
                            else
                            {
                                Debug.LogError( $"[{nameof(ObjectsJsonSerializer)}]-[{nameof(ResolveGDObjectReferences)}] cannot resolve GDObject reference {guid}. Target object {unresolvedReference.TargetObject.GetType().Name}, field {unresolvedReference.Field}" );
                            }
                        }
                        unresolvedReference.Field.SetValue( unresolvedReference.TargetObject, result );
                    }
                }
                else                                 //Scalar target field
                {
                    var resolvedObject = _loadedObjects.FirstOrDefault( gdo => gdo.Guid == unresolvedReference.Guid );
                    if( resolvedObject )
                        unresolvedReference.Field.SetValue( unresolvedReference.TargetObject, resolvedObject );
                    else
                    {
                        Debug.LogError( $"[{nameof(ObjectsJsonSerializer)}]-[{nameof(ResolveGDObjectReferences)}] cannot resolve GDObject reference {unresolvedReference.Guid}. Target object {unresolvedReference.TargetObject.GetType().Name}, field {unresolvedReference.Field}" );
                    }
                }
            }
            _resolveObjectSampler.End();
        }

        private GDObject ReadGDObjectFromJson( JsonReader json )
        {
            EnsureToken( json, JsonToken.StartObject );
            var name = ReadPropertyString(json, ".Name", false );

            var type = typeof(GDObject);
            var  propName = json.ReadAsString();        Assert.IsTrue( json.TokenType == JsonToken.PropertyName );
            if ( propName == ".Type" )                
                type     = Type.GetType( json.ReadAsString()! );
            var guid = Guid.ParseExact( ReadPropertyString( json, ".Ref", true ), "D" );

            var obj     = GDObject.CreateInstance( type ).SetGuid( guid );
            obj.hideFlags     = HideFlags.HideAndDontSave;
            obj.name          = name;

            try
            {
                //Components should be read from reserved property
                EnsureNextToken( json, JsonToken.PropertyName );
                Assert.IsTrue( (String)json.Value == ".Components" );
                EnsureNextToken( json, JsonToken.StartArray );

                json.Read();
                var objectStartOrArrayEnd = json.TokenType;
                while( objectStartOrArrayEnd == JsonToken.StartObject )
                {
                    try
                    {
                        var component = (GDComponent)ReadObjectFromJson( json, typeof(GDComponent) );
                        obj.Components.Add( component );
                    }
                    catch ( Exception e )
                    {
                        throw new JsonComponentException( obj.Components.Count, json,
                                $"Error reading component index {obj.Components.Count} of GDObject {obj.Name} ({obj.Guid}) from json {json}", e );
                    }
                    
                    objectStartOrArrayEnd = json.TokenType;
                }
                EnsureToken( json, JsonToken.EndArray );

                if ( type != typeof(GDObject) )         //It can be descendants properties
                {
                    throw new NotImplementedException();
                }
                
                return obj;
            }
            catch ( Exception e )
            {
                throw new JsonObjectException( obj.Name, obj.GetType(), gdObj,
                        $"Error reading object {obj.Name} of type {obj.GetType()} from jobject {gdObj}", e );
            }
        }

        private Object ReadObjectFromJson( JsonReader json, Type propertyType )
        {
            EnsureToken( json, JsonToken.StartObject );
            json.Read();

            if( json.TokenType == JsonToken.EndObject )
                return new Object();                    //Unity doesn't deserialize null objects
            
            EnsureToken( json, JsonToken.PropertyName );

            var objectType = propertyType;
            if ( (String)json.Value == ".Type" )
            {
                var typeStr = json.ReadAsString()!;
                objectType = Type.GetType( typeStr );
                if( objectType == null )
                    throw new InvalidOperationException( $"[{nameof(ObjectsJsonSerializer)}]-[{nameof(ReadObjectFromJson)}] Cannot create Type from type string '{typeStr}'" );
            }

            var defaultConstructor =
                    objectType.GetConstructor( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Array.Empty<Type>(), null );
            if ( defaultConstructor == null )
            {
                Debug.LogError( $"Default constructor not found for type {objectType} read null object" );
                return null;
            }

            var obj = defaultConstructor.Invoke( Array.Empty<Object>() );

            ReadObjectPropertiesFromJson( json, obj );

            EnsureToken( json, JsonToken.EndObject );

            return obj;
        }

        private void ReadObjectPropertiesFromJson( JsonReader json, Object obj )
        {
            var propNameOrEndObject = json.TokenType;
            if( propNameOrEndObject == JsonToken.EndObject )
                return;

            var fields = GetTypeFields( obj.GetType() );
            var propName = (String)json.Value;
            var field = fields.FirstOrDefault( f => f.Name == propName );
            if ( field == null )
            {
                json.Skip();
            }
            else
            {
                if ( IsFieldSerializable( field ) )
                {
                    try
                    {
                        var value = ReadSomethingFromJson( json, field.FieldType );
                        if ( value is UnresolvedGDObjectReference gdRef )                        //Should be resolved after all objects deserialization
                        {
                            gdRef.TargetObject = obj;
                            gdRef.Field        = field;
                            _unresolvedReferences.Add( gdRef );
                        }
                        else
                            field.SetValue( obj, value );
                    }
                    catch ( Exception ex )
                    {
                        throw new JsonPropertyException( field.Name, json, $"Error setting field {field} from json {json}", ex );
                    }
                }
                else
                {
                    json.Skip();
                }
            }


            EnsureToken( json, JsonToken.EndObject );
        }

        private Object ReadSomethingFromJson( JsonReader value, Type propertyType )
        {
            if ( propertyType == typeof(Char) )
            {
                return value.ReadAsString()![0];
            }
            else if ( propertyType == typeof(String) )
            {
                return value.ReadAsString();
            }
            else if ( propertyType.IsPrimitive )
            {
                if( propertyType == typeof(Boolean) )
                    return value.ReadAsBoolean();
                if( propertyType == typeof(Single) || propertyType == typeof(Double) )
                    return value.ReadAsDouble();
                if ( propertyType == typeof(Int64) )
                {
                    value.Read();
                    return (Int64)value.Value;
                }
                if ( propertyType == typeof(UInt64) )
                {
                    value.Read();
                    return (UInt64)value.Value;
                }
                if ( propertyType == typeof(UInt32) )
                {
                    value.Read();
                    return (UInt32)value.Value;
                }
                return value.ReadAsInt32();
            }
            else if ( propertyType.IsEnum )
            {
                if( Enum.TryParse( propertyType, value.ReadAsString(), out var enumValue ) )
                    return enumValue;
                else
                    return null;                                //do not change default value
            }
            else if ( propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() ==  typeof(List<>) ) )
            {
                return ReadCollectionFromJson( value, propertyType );
            }
            else if ( typeof(GDObject).IsAssignableFrom( propertyType ) )
            {
                var guidStr = value.ReadAsString()!;
                var guid    = Guid.ParseExact( guidStr, "D" );
                var referencedObj = _loadedObjects.FirstOrDefault( gdo => gdo.Guid == guid );
                if( referencedObj )
                    return referencedObj;
                else
                    return new UnresolvedGDObjectReference(){Guid = guid};
            }
            else if ( _serializers.TryGetValue( propertyType, out var deserializer ) )
            {
                return deserializer.Deserialize( value );
            }
            else if ( typeof(UnityEngine.Object).IsAssignableFrom( propertyType ) )      //Read Unity asset by reference
            {
                EnsureNextToken( value, JsonToken.StartObject );
                value.Read();

                if( value.TokenType == JsonToken.EndObject )
                    return null;

                var guidStr = ReadPropertyString( value, ".Ref", true );
                var localId = ReadPropertyLong( value, ".Id", false );

                if( !_assetResolver.TryGetAsset( guidStr, localId, out var asset ) )
                    Debug.LogError( $"Error resolving Unity asset reference {guidStr} : {localId}, type {propertyType}" );
                return asset;
            }
            else
            {
                return ReadObjectFromJson( value, propertyType );
            }
        }
 
        private Object ReadCollectionFromJson( JsonReader json, Type type )
        {
            EnsureNextToken( json, JsonToken.StartArray );

            if ( type.IsArray )                   //Array class not handled
            {
                var elementType = type.GetElementType() ?? typeof(Object);
                if ( elementType == typeof(GDObject) )
                    return GetUnresolvedGDORefCollection( jArray);

                var bufferType = typeof(List<>).MakeGenericType( elementType );
                var list       = (IList)Activator.CreateInstance( bufferType );

                json.Read();
                while ( json.TokenType != JsonToken.EndArray)
                {
                    list.Add( ReadSomethingFromJson( json, elementType ) );
                }
                
                return list.;
            }
            else if ( type.IsGenericType && type.GetGenericTypeDefinition() ==  typeof(List<>) )
            {
                var elementType = type.GetGenericArguments()[ 0 ];
                if ( elementType == typeof(GDObject) )
                    return GetUnresolvedGDORefCollection( jArray);

                var list        = (IList)Activator.CreateInstance( type );
                foreach ( var valueToken in jArray )
                {
                    list.Add( ReadSomethingFromJson( valueToken.Value, elementType ) );
                }

                return list;
            }

            return null;

            UnresolvedGDObjectReference GetUnresolvedGDORefCollection( JsonReader jsonArray )
            {
                var guids = new Guid[ jsonArray.Count ];
                for ( int i = 0; i < jsonArray.Count; i++ )                        
                    guids[ i ] = Guid.ParseExact( jsonArray[ i ].Value, "D" );
                return new UnresolvedGDObjectReference()
                                         {
                                                 Guids = guids,
                                         };
            }
        }

        private IReadOnlyList<FieldInfo> GetTypeFields( Type type )           //Make it cached
        {
            var result = type.GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
            return result;
        }

#endregion

        private struct UnresolvedGDObjectReference
        {
            //GDObject guid
            public Guid         Guid;
            public Guid[]       Guids;          //Collection field

            //Target info
            public Object  TargetObject;
            public FieldInfo Field;
        }

#region Fast forward reader

        // public List<GDObject> JsonToGD( String json )
        // {
        //     var result = new List<GDObject>();
        //
        //     using (JsonReader reader = new JsonTextReader( new StringReader( json ) ))
        //     {
        //         EnsureNextToken( reader, JsonToken.StartArray );
        //
        //         while ( reader.Read() )
        //         {
        //             if ( reader.TokenType == JsonToken.StartObject )
        //             {
        //                 var gdObject = ReadGDObjectFromJson( reader );
        //                 result.Add( gdObject );
        //             }
        //             else if ( reader.TokenType == JsonToken.EndArray )
        //             {
        //                 break;
        //             }
        //         }
        //
        //         return result;
        //     }
        // }
        //
        // private GDObject ReadGDObjectFromJson( JsonReader reader )
        // {
        //     EnsureToken( reader, JsonToken.StartObject );
        //     var objectName = ReadPropertyString( reader, "_Name", false );
        //     var objectType = ReadPropertyString( reader, "_Type", false );
        //
        //     var result = (GDObject)ScriptableObject.CreateInstance( objectType );
        //     result.name = objectName;
        //
        //     Debug.Log( $"Start reading object {result.name}" );
        //
        //
        //     while ( reader.Read() )
        //     {
        //         if( reader.TokenType == JsonToken.PropertyName && reader.Depth == 2 )
        //         {
        //             var fieldInfo = result.GetType().GetField( (String)reader.Value, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
        //             var value = ReadPropertyFromJson( reader, fieldInfo );
        //
        //             Debug.Log( $"Read {result.name}.{fieldInfo.Name} = {value}" );
        //         }    
        //         else if( reader.TokenType == JsonToken.EndObject && reader.Depth == 1 )
        //             break;
        //     }
        //
        //     //DEBUG skip content
        //     // var objectDepth = reader.Depth - 1;
        //     // while ( reader.Read() )
        //     // {
        //     //     if( reader.Depth == objectDepth && reader.TokenType == JsonToken.EndObject )
        //     //         break;
        //     // }
        //     //DEBUG
        //
        //     EnsureToken( reader, JsonToken.EndObject );
        //     Debug.Log( $"End reading object {result.name}" );
        //
        //     
        //     
        //     return result;
        // }
        //
        // private Object ReadPropertyFromJson( JsonReader reader, FieldInfo fieldInfo )
        // {
        //     if ( reader.Read() )
        //     {
        //         switch ( reader.TokenType )
        //         {
        //             case JsonToken.Null:
        //                 return null;
        //             case JsonToken.Boolean:
        //             case JsonToken.String:
        //             case JsonToken.Integer:
        //             case JsonToken.Float:
        //                 return reader.Value;
        //                 
        //             default:
        //             {
        //
        //                 return null;            //DEBUG read complex objects
        //             }
        //         }
        //     }
        //
        //     return null;
        // }
        //
        private void EnsureProperty( JsonReader reader, String propertyName )
        {
            if ( reader.TokenType == JsonToken.PropertyName && String.Equals( reader.Value, propertyName ) )
            {
                //It's ok
            }
            else
                throw new Exception( $"Expected property {propertyName} but got token {reader.TokenType}, value {reader.Value}" );
        }
        private void EnsureNextProperty( JsonReader reader, String propertyName )
        {
            if ( reader.Read() )
            {
                EnsureProperty( reader, propertyName );
            }
            else
                throw new Exception( $"Unexpected end of file" );
        }
        private void EnsureToken( JsonReader reader, JsonToken tokenType )
        {
            if ( reader.TokenType == tokenType )
            {
                //It's ok
            }
            else
                throw new Exception( $"Expected token {tokenType} but got {reader.TokenType} = {reader.Value}" );
        }
        
        private void EnsureNextToken( JsonReader reader, JsonToken tokenType )
        {
            if ( reader.Read() )
            {
                EnsureToken( reader, tokenType );
            }
            else
                throw new Exception( $"Unexpected end of file" );
        }
        
        private String ReadPropertyString( JsonReader reader, String propertyName, Boolean alreadyOnStart )
        {
            if( alreadyOnStart )
                EnsureToken( reader, JsonToken.PropertyName );
            else
                EnsureNextToken( reader, JsonToken.PropertyName );
            if ( String.Equals(reader.Value, propertyName )) 
            {
                EnsureNextToken( reader, JsonToken.String );
                return (String)reader.Value;
            }
            else
                throw new Exception( $"Expected property {propertyName} but got {reader.Value}" );
        }

        private Int64 ReadPropertyLong( JsonReader reader, String propertyName, Boolean alreadyOnStart )
        {
            if( alreadyOnStart )
                EnsureToken( reader, JsonToken.PropertyName );
            else
                EnsureNextToken( reader, JsonToken.PropertyName );
            if ( String.Equals(reader.Value, propertyName )) 
            {
                EnsureNextToken( reader, JsonToken.String );
                return Int64.Parse((String)reader.Value, CultureInfo.InvariantCulture);
            }
            else
                throw new Exception( $"Expected property {propertyName} but got {reader.Value}" );
        }

#endregion

    }
}
