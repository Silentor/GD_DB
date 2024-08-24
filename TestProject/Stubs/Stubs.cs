namespace GDDB;

public  class GDObject
{

}

public class GdDb
{
    public IEnumerable<GDObject> GetObjects( Int32 mainCategory, Int32 subCategory )
    {
        return Array.Empty<GDObject>();
    }

    public IEnumerable<GDObject> GetObjects( Int32 mainCategory )
    {
        return Array.Empty<GDObject>();
    }
}