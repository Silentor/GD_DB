using System.Collections;
using GDDB;

#nullable enable

namespace GDDB;

public partial class GdDb
{
    public TestRootFolder TestRoot => new( RootFolder );
}

public readonly struct TestRootFolder : IEnumerable<GDObject>, IEquatable<TestRootFolder>
{
    public readonly Folder Folder;

    internal TestRootFolder( Folder folder )
    {
        Folder = folder;
    }

#region IEnumerable

    public IEnumerator<GDObject> GetEnumerator( )
    {
        return Folder.Objects.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator( )
    {
        return GetEnumerator();
    } 

#endregion

#region IEquatable

    public bool Equals(TestRootFolder other)
    {
        return Folder.FolderGuid.Equals( other.Folder.FolderGuid );
    }

    public override bool Equals(object? obj)
    {
        return obj is TestRootFolder other && Equals( other );
    }

    public override int GetHashCode( )
    {
        return Folder.FolderGuid.GetHashCode();
    }

    public static bool operator ==(TestRootFolder left, TestRootFolder right)
    {
        return left.Equals( right );
    }

    public static bool operator !=(TestRootFolder left, TestRootFolder right)
    {
        return !left.Equals( right );
    }

#endregion

    public TestMobsFolder Mobs => new( Folder.SubFolders[0] );
}

public readonly struct TestMobsFolder : IEnumerable<GDObject>
{
    public readonly Folder Folder;

    internal TestMobsFolder( Folder folder )
    {
        Folder = folder;
    }        
    
    public IEnumerator<GDObject> GetEnumerator( )
    {
        return Folder.Objects.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator( )
    {
        return GetEnumerator();
    } 

    public TestHumansFolder Humans => new( Folder.SubFolders[0] ); 
}

public readonly struct TestHumansFolder : IEnumerable<GDObject>
{
    public readonly Folder Folder;

    internal TestHumansFolder( Folder folder )
    {
        Folder = folder;
    }        
    
    public IEnumerator<GDObject> GetEnumerator( )
    {
        return Folder.Objects.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator( )
    {
        return GetEnumerator();
    } 
}
