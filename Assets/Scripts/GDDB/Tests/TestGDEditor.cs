using System.Collections.Generic;
using System.Linq;

namespace GDDB.Tests
{
    public class TestGDEditor : GdLoader
    {
        public override IReadOnlyList<GDObject> AllObjects => ExposedObjectsList;

        public List<GDObject> ExposedObjectsList { get; } = new ();
    
        public TestGDEditor( GDRoot root )
        {
            Root = root;
            ExposedObjectsList.Add( Root );
        }

        public TestGDEditor( IEnumerable<GDObject> objects )
        {
            ExposedObjectsList.AddRange( objects );
            Root = ExposedObjectsList.Single( gdo => gdo is GDRoot ) as GDRoot;
        }
    }
}