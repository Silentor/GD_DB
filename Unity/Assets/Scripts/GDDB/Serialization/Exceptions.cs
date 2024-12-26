using System;
using Newtonsoft.Json;

namespace GDDB.Serialization
{
    public class ReaderComponentException : Exception
    {
        public Int32      ComponentIndex { get; }
        public ReaderBase Reader           { get; }
        public String     Path           { get; }

        public ReaderComponentException()
        {
        }

        public ReaderComponentException( Int32 index, ReaderBase reader, string message)
                : base(message)
        {
            ComponentIndex = index;
            Reader           = reader;
            Path           = reader.Path;
        }

        public ReaderComponentException( Int32 index, ReaderBase reader, string message, Exception inner)
                : base(message, inner)
        {
            ComponentIndex = index;
            Reader           = reader;
            Path           = reader.Path;
        }
    }

    public class ReaderObjectException : Exception
    {
        public String     ObjectName { get; }
        public Type       ObjectType { get; }
        public ReaderBase Reader       { get; }
        public String     Path       { get; }

        public ReaderObjectException( )
        {
        }

        public ReaderObjectException( String objectName, Type objectType, ReaderBase reader, string message)
                : base( message )
        {
            Reader       = reader;
            ObjectName = objectName;
            ObjectType = objectType;
            Path       = reader.Path;
        }

        public ReaderObjectException( String objectName, Type objectType, ReaderBase reader, string message, Exception inner)
                : base( message, inner )
        {
            ObjectName = objectName;
            ObjectType = objectType;
            Reader       = reader;
            Path       = reader.Path;
        }
    }

    public class ReaderTokenException : Exception
    {
        public String     TokenName     { get; }
        public ReaderBase Reader         { get; }
        public String     Path         { get; }

        public ReaderTokenException( )
        {
        }

        public ReaderTokenException( String tokenName, ReaderBase reader, string message )
                : base( message )
        {
            TokenName = tokenName;
            Reader         = reader;
            Path         = reader.Path;
        }

        public ReaderTokenException(String tokenName, ReaderBase reader, string message, Exception inner )
                : base( message, inner )
        {
            TokenName = tokenName;
            Reader         = reader;
            Path         = reader.Path;
        }
    }


    public class ReaderPropertyException : Exception
    {
        public String     PropertyName { get; }
        public ReaderBase Reader         { get; }
        public String     Path         { get; }

        public ReaderPropertyException( )
        {
        }

        public ReaderPropertyException( String propertyName, ReaderBase reader, string message )
                : base( message )
        {
            PropertyName = propertyName;
            Reader         = reader;
            Path         = reader.Path;
        }

        public ReaderPropertyException(String propertyName, ReaderBase reader, string message, Exception inner )
                : base( message, inner )
        {
            PropertyName = propertyName;
            Reader         = reader;
            Path         = reader.Path;
        }
    }


    public class ReaderFolderException : Exception
    {
        public String     FolderName { get; }
        public Guid       Guid { get; }
        public ReaderBase Reader       { get; }
        public String     ReaderPath       { get; }
        public String     FolderPath       { get; }

        public ReaderFolderException( )
        {
        }

        public ReaderFolderException( Folder folder, ReaderBase reader, string message)
                : base( message )
        {
            Reader       = reader;
            FolderName = folder.Name;
            Guid       = folder.FolderGuid;
            ReaderPath   = reader.Path;
            FolderPath = folder.GetPath();
        }

        public ReaderFolderException( Folder folder, ReaderBase reader, string message, Exception inner)
                : base( message, inner )
        {
            Reader       = reader;
            FolderName = folder.Name;
            Guid       = folder.FolderGuid;
            ReaderPath   = reader.Path;
            FolderPath = folder.GetPath();
        }
    }

}