using System;
using System.Collections.Generic;
using System.Linq;

namespace GDDB
{
    public abstract class GdLoader
    {
        public         GDRoot                  Root       { get; protected set; }
        public virtual IReadOnlyList<GDObject> AllObjects { get; }

        public IEnumerable<T> GetComponents<T>() where T : GDComponent
        {
            foreach ( var gdObject in AllObjects )
            {
                foreach ( var component in gdObject.Components )
                {
                    if ( component is T t )
                        yield return t;
                }
            }
        }

        public GDObject GetObject( GdType type )
        {
            return AllObjects.First( o => o.Type == type );
        }

        public IEnumerable<GDObject> GetObjects( Int32 category1 )
        {
            return AllObjects.Where( o => o.Type.Cat1 == category1 );
        }

        public IEnumerable<GDObject> GetObjects( Int32 category1, Int32 category2 )
        {
            return AllObjects.Where( o => o.Type.Cat1 == category1 && o.Type.Cat2 == category2 );
        }

    }
}