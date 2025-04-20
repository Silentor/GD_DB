using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Gddb
{
    /// <summary>
    /// Full info about gddb object
    /// </summary>
    public readonly struct GdObjectInfo : IComparable<GdObjectInfo>, IEquatable<GdObjectInfo>
    {
        public readonly Guid             Guid;
        public readonly ScriptableObject Object;
        public readonly GdFolder         Folder;

        public          String           Name => Object.name;

        public GdObjectInfo(Guid guid, ScriptableObject obj, GdFolder folder )
        {
            Assert.IsNotNull( obj );
            Assert.IsNotNull( folder );

            Guid   = guid;
            Object = obj;
            Folder = folder;
        }

        private GdObjectInfo( Guid guid )
        {
            Guid = guid;
            Object = null;
            Folder = null;
        }

        /// <summary>
        /// For search by guid
        /// </summary>
        public static implicit operator GdObjectInfo( Guid guid ) => new ( guid );

        public int CompareTo(GdObjectInfo other)
        {
            return Guid.CompareTo( other.Guid );
        }

        public bool Equals(GdObjectInfo other)
        {
            return Guid.Equals( other.Guid );
        }

        public override bool Equals(object obj)
        {
            return obj is GdObjectInfo other && Equals( other );
        }

        public override int GetHashCode( )
        {
            return Guid.GetHashCode();
        }

        public static bool operator ==(GdObjectInfo left, GdObjectInfo right)
        {
            return left.Equals( right );
        }

        public static bool operator !=(GdObjectInfo left, GdObjectInfo right)
        {
            return !left.Equals( right );
        }
    }
}