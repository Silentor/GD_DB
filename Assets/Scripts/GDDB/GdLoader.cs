using System.Collections.Generic;

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
    }
}