using System;
using Newtonsoft.Json;

namespace GDDB.Serialization
{
    public class JsonComponentException : Exception
    {
        public Int32      ComponentIndex { get; }
        public JsonReader Json           { get; }
        public String     Path           { get; }

        public JsonComponentException()
        {
        }

        public JsonComponentException( Int32 index, JsonReader json, string message)
                : base(message)
        {
            ComponentIndex = index;
            Json           = json;
            Path           = json.Path;
        }

        public JsonComponentException( Int32 index, JsonReader json, string message, Exception inner)
                : base(message, inner)
        {
            ComponentIndex = index;
            Json           = json;
            Path           = json.Path;
        }
    }

    public class JsonObjectException : Exception
    {
        public String     ObjectName { get; }
        public Type       ObjectType { get; }
        public JsonReader Json       { get; }
        public String     Path       { get; }

        public JsonObjectException( )
        {
        }

        public JsonObjectException( String objectName, Type objectType, JsonReader json, string message)
                : base( message )
        {
            Json       = json;
            ObjectName = objectName;
            ObjectType = objectType;
            Path       = json.Path;
        }

        public JsonObjectException( String objectName, Type objectType, JsonReader json, string message, Exception inner)
                : base( message, inner )
        {
            ObjectName = objectName;
            ObjectType = objectType;
            Json       = json;
            Path       = json.Path;
        }
    }

    public class JsonTokenException : Exception
    {
        public String     TokenName     { get; }
        public JsonReader Json         { get; }
        public String     Path         { get; }

        public JsonTokenException( )
        {
        }

        public JsonTokenException( String tokenName, JsonReader json, string message )
                : base( message )
        {
            TokenName = tokenName;
            Json         = json;
            Path         = json.Path;
        }

        public JsonTokenException(String tokenName, JsonReader json, string message, Exception inner )
                : base( message, inner )
        {
            TokenName = tokenName;
            Json         = json;
            Path         = json.Path;
        }
    }


    public class JsonPropertyException : Exception
    {
        public String     PropertyName { get; }
        public JsonReader Json         { get; }
        public String     Path         { get; }

        public JsonPropertyException( )
        {
        }

        public JsonPropertyException( String propertyName, JsonReader json, string message )
                : base( message )
        {
            PropertyName = propertyName;
            Json         = json;
            Path         = json.Path;
        }

        public JsonPropertyException(String propertyName, JsonReader json, string message, Exception inner )
                : base( message, inner )
        {
            PropertyName = propertyName;
            Json         = json;
            Path         = json.Path;
        }
    }


    public class JsonFolderException : Exception
    {
        public String     FolderName { get; }
        public Guid       Guid { get; }
        public JsonReader Json       { get; }
        public String     JsonPath       { get; }
        public String     FolderPath       { get; }

        public JsonFolderException( )
        {
        }

        public JsonFolderException( Folder folder, JsonReader json, string message)
                : base( message )
        {
            Json       = json;
            FolderName = folder.Name;
            Guid       = folder.FolderGuid;
            JsonPath   = json.Path;
            FolderPath = folder.GetPath();
        }

        public JsonFolderException( Folder folder, JsonReader json, string message, Exception inner)
                : base( message, inner )
        {
            Json       = json;
            FolderName = folder.Name;
            Guid       = folder.FolderGuid;
            JsonPath   = json.Path;
            FolderPath = folder.GetPath();
        }
    }

}