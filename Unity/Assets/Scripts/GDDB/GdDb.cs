using System;
using System.Collections.Generic;
using System.Linq;

namespace GDDB
{
    public partial class GdDb
    {
        public         GDRoot                  Root       { get; }

        public virtual IReadOnlyList<GDObject> AllObjects { get; }

        public GdDb( IReadOnlyList<GDObject> allObjects )
        {
            Root       = allObjects.OfType<GDRoot>().Single( );
            AllObjects = allObjects;
        }

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
            return AllObjects.Where( o => o.Type[0] == category1 );
        }

        public IEnumerable<GDObject> GetObjects( Int32 category1, Int32 category2 )
        {
            return AllObjects.Where( o => o.Type[0] == category1 && o.Type[1] == category2 );
        }
    }
}