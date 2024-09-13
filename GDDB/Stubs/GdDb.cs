namespace GDDB;

public partial class GdDb
{
    public Folder RootFolder;

    public virtual IReadOnlyList<GDObject> AllObjects { get; }

    public IEnumerable<GDObject> GetObjects( String path )
    {
        return Array.Empty<GDObject>();
    }

    public GDObject? GetObject( GdId guid )
    {
        return null;
    }
    
    public Folder? GetFolder( GdId guid )
    {
        return new Folder();
    }
    

}