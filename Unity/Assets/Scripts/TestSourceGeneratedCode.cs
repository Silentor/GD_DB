using System;
using System.Collections;
using System.Collections.Generic;
using GDDB;
using UnityEngine;

namespace TestGdDb
{
    /// <summary>
    /// Examples of code that should be generated based on TestCategories.cs
    /// </summary>
    public static class TestSourceGeneratedCode
    {
        
        public static MobsFilter GetMobs( this GdDb gddb )
        {
            return new MobsFilter( gddb );
        }

        public static OrcsFilter GetOrcs( this MobsFilter mobsFilter )
        {
            return new OrcsFilter( mobsFilter._db );
        }

        public class MobsFilter : IEnumerable<GDObject>
        {
            internal readonly GdDb _db;

            public MobsFilter( GdDb db )
            {
                _db = db;
            }

            public IEnumerator<GDObject> GetEnumerator( )
            {
                foreach ( var gdo in _db.GetObjects( (Int32)MainCategory.Mobs ) )
                    yield return gdo;
            }

            IEnumerator IEnumerable.GetEnumerator( )
            {
                return GetEnumerator();
            }
        }

        public class OrcsFilter : IEnumerable<GDObject>
        {
            private readonly GdDb _db;

            public OrcsFilter( GdDb db )
            {
                _db = db;
            }

            public IEnumerator<GDObject> GetEnumerator( )
            {
                foreach ( var gdo in _db.GetObjects( (Int32)MainCategory.Mobs, (Int32)EMobs.Orcs ) )
                    yield return gdo;
            }

            IEnumerator IEnumerable.GetEnumerator( )
            {
                return GetEnumerator();
            }
        }
        
    }

    public class TestGeneratedUsability
    {
        public void Main( )
        {
            var db = new GdDb( null, new List<GDObject>() );
            var orcs = db.GetMobs().GetOrcs();
            foreach ( var orcObj in orcs )
            {
                Debug.Log( $"Obj name {orcObj.Name}, type {orcObj.Type}" );
            }
        }

    }
    
}