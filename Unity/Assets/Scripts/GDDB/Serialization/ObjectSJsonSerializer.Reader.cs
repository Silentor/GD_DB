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
            //json.Read();        Assert.IsTrue( json.TokenType == JsonToken.StartObject );
            json.Read();        Assert.IsTrue( json.TokenType == JsonToken.PropertyName && (String)json.Value == ".Name" );      
            var name = json.ReadAsString()!;

            Type type = typeof(GDObject);
            Guid guid;
            var  propName = json.ReadAsString();        Assert.IsTrue( json.TokenType == JsonToken.PropertyName );
            if ( propName == ".Type" )
            {
                type     = Type.GetType( json.ReadAsString()! );
                propName = json.ReadAsString();        Assert.IsTrue( json.TokenType == JsonToken.PropertyName );
            }
            Assert.IsTrue( propName == ".Ref" );
            guid = Guid.ParseExact( json.ReadAsString()!, "D" );

            var obj     = GDObject.CreateInstance( type ).SetGuid( guid );
            obj.hideFlags     = HideFlags.HideAndDontSave;
            obj.name          = name;

            try
            {
                //Components should be read from reserved property
                json.Read();        Assert.IsTrue( json.TokenType == JsonToken.PropertyName && (String)json.Value == ".Components" );    
                

                var componentsProp = gdObj[ ".Components" ].AsArray;
                for ( var i = 0; i < componentsProp.Count; i++ )
                {
                    var componentjToken = componentsProp[ i ];
                    try
                    {
                        var component = (GDComponent)ReadObjectFromJson( (JSONObject)componentjToken, typeof(GDComponent) );
                        obj.Components.Add( component );
                    }
                    catch ( Exception e )
                    {
                        throw new JsonComponentException( i, componentjToken,
                                $"Error reading component index {i} of GDObject {obj.Name} ({obj.Guid}) from jtoken {componentjToken} ", e );
                    }
                }

                ReadContentFromJson( gdObj, obj );
                return obj;
            }
            catch ( Exception e )
            {
                throw new JsonObjectException( obj.Name, obj.GetType(), gdObj,
                        $"Error reading object {obj.Name} of type {obj.GetType()} from jobject {gdObj}", e );
            }
        }

        private Object ReadObjectFromJson( JSONObject jObject, Type propertyType )
        {
            if ( jObject.Count == 0)
                return null;

            var objectType = propertyType;

            //Check polymorphic object
            var valueObj = jObject;
            var typeProp = jObject[ ".Type" ];
            if ( !String.IsNullOrEmpty(typeProp.Value) )
            {
                var typeValue = typeProp.Value;
                objectType = Type.GetType( typeValue );

                if( objectType == null )
                    throw new InvalidOperationException( $"[{nameof(ObjectsJsonSerializer)}]-[{nameof(ReadObjectFromJson)}] Cannot create Type from type string '{typeValue}'" );

                valueObj   = (JSONObject)jObject[ ".Value" ];
            }

            // if ( _serializers.TryGetValue( objectType, out var serializer ) )
            // {
            //     return serializer.Deserialize( token );
            // }

            var defaultConstructor =
                    objectType.GetConstructor( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Array.Empty<Type>(), null );
            if ( defaultConstructor == null )
            {
                Debug.LogError( $"Default constructor not found for type {objectType} read null object" );
                return null;
            }

            var obj = defaultConstructor.Invoke( Array.Empty<Object>() );

            ReadContentFromJson( valueObj, obj );

            return obj;
        }

        private void ReadContentFromJson( JSONObject jObject, Object obj )
        {
            foreach ( var field in obj.GetType().GetFields( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ) )
            {
                if ( IsFieldSerializable( field ) )
                {
                    var valueToken = jObject[ field.Name ];
                    if ( valueToken == null )                        //Incorrect name
                        field.SetValue( obj, null );
                    else
                        try
                        {
                            var value = ReadSomethingFromJson( valueToken, field.FieldType );
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
                            throw new JsonPropertyException( field.Name, valueToken, $"Error setting field {field} from jtoken {valueToken}", ex );
                        }
                }
            }
        }

        private Object ReadSomethingFromJson( JSONNode value, Type propertyType )
        {
            if ( propertyType == typeof(Char) )
            {
                return value.AsChar;
            }
            else if ( propertyType == typeof(String) )
            {
                return value.Value;
            }
            else if ( propertyType.IsPrimitive )
            {
                if( propertyType == typeof(Boolean) )
                    return value.AsBool;
                if( propertyType == typeof(Single) || propertyType == typeof(Double) )
                    return Convert.ChangeType( value.AsDouble, propertyType, CultureInfo.InvariantCulture );
                if ( propertyType == typeof(UInt64) )
                    return value.AsULong;
                return Convert.ChangeType( value.AsLong, propertyType, CultureInfo.InvariantCulture );
            }
            else if ( propertyType.IsEnum )
            {
                if( Enum.TryParse( propertyType, value.Value, out var enumValue ) )
                    return enumValue;
                else
                    return Activator.CreateInstance( propertyType );
            }
            else if ( propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() ==  typeof(List<>) ) )
            {
                return ReadCollectionFromJson( (JSONArray)value, propertyType );
            }
            else if ( typeof(GDObject).IsAssignableFrom( propertyType ) )
            {
                var guidStr = value.Value;
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
                if ( value.Count == 0 )                  //Seems like missed reference or null reference
                    return null;

                var guid    = value[ ".Ref" ].Value;
                var localId = value[ ".Id" ].AsLong;
                if( !_assetResolver.TryGetAsset( guid, localId, out var asset ) )
                    Debug.LogError( $"Error resolving Unity asset reference {guid} : {localId}, type {propertyType}" );
                return asset;
            }
            else
            {
                return ReadObjectFromJson( (JSONObject)value, propertyType );
            }
        }
 
        private Object ReadCollectionFromJson(JSONArray jArray, Type type )
        {
            if ( type.IsArray )                   //Array class not handled
            {
                var elementType = type.GetElementType() ?? typeof(Object);
                if ( elementType == typeof(GDObject) )
                    return GetUnresolvedGDORefCollection( jArray);

                var array       = Array.CreateInstance( elementType, jArray.Count );
                for ( int i = 0; i < jArray.Count; i++ )
                {
                    array.SetValue( ReadSomethingFromJson( jArray[ i ], elementType ), i );
                }

                return array;
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

            UnresolvedGDObjectReference GetUnresolvedGDORefCollection(JSONArray jsonArray )
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
        // private void EnsureToken( JsonReader reader, JsonToken tokenType )
        // {
        //     if ( reader.TokenType == tokenType )
        //     {
        //         //Its ok
        //     }
        //     else
        //         throw new Exception( $"Expected token {tokenType} but got {reader.TokenType} = {reader.Value}" );
        // }
        //
        // private void EnsureNextToken( JsonReader reader, JsonToken tokenType )
        // {
        //     if ( reader.Read() )
        //     {
        //         EnsureToken( reader, tokenType );
        //     }
        //     else
        //         throw new Exception( $"Unexpected end of file" );
        // }
        //
        // private String ReadPropertyString( JsonReader reader, String propertyName, Boolean alreadyOnStart )
        // {
        //     if( alreadyOnStart )
        //         EnsureToken( reader, JsonToken.PropertyName );
        //     else
        //         EnsureNextToken( reader, JsonToken.PropertyName );
        //     if ( String.Equals(reader.Value, propertyName )) 
        //     {
        //         EnsureNextToken( reader, JsonToken.String );
        //         return (String)reader.Value;
        //
        //     }
        //     else
        //         throw new Exception( $"Expected property {propertyName} but got {reader.Value}" );
        // }

#endregion

    }
}
