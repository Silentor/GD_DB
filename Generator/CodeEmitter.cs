using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace GDDB.SourceGenerator;

public class CodeEmitter
{
    private static readonly String AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
    private static readonly String AssemblyVersion = Assembly.GetExecutingAssembly(). GetName().Version.ToString();
    private static readonly String FileHeader = "//Generated from {0} at {1}";
    private static readonly String GeneratedTypeAttribute = $"[global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"{AssemblyName}\", \"{AssemblyVersion}\")]";
    private static readonly String GdDbTypeName = "GdDb";
    

    // public String GenerateEnums( String treeFilePath, Category rootCategory, IReadOnlyCollection<Category> allCategories  )
    // {
    //     var sb = new StringBuilder();
    //     sb.AppendLine( String.Format( FileHeader, treeFilePath, DateTime.Now ));
    //     sb.AppendLine( $"namespace GDDB" );
    //     sb.AppendLine( "{" );
    //     foreach ( var category in allCategories )
    //     {
    //         GenerateEnum( sb, category );    
    //     }
    //        
    //     sb.AppendLine( $"{"}"}" );
    //     return sb.ToString();
    // }

    public String GenerateFolders( String treeFilePath, IReadOnlyCollection<Folder> allFolders  )
    {
        var sb = new StringBuilder();
        sb.AppendLine( String.Format( FileHeader, treeFilePath, DateTime.Now ));
        sb.AppendLine( "#nullable enable" );
        sb.AppendLine( "using System;" );
        sb.AppendLine( "using System.Collections;" );
        sb.AppendLine( "using System.Collections.Generic;" );
        sb.AppendLine( );
        sb.AppendLine( "namespace GDDB" );
        sb.AppendLine( "{" );
        sb.AppendLine( );
        foreach ( var folder in allFolders )
        {
            GenerateFolder( sb, folder );
        }
        sb.AppendLine( "}" );
           
        return sb.ToString();
    }

    public String GenerateGdDbPartial( String treeFilePath, Folder rootFolder )
    {
        var sb = new StringBuilder();
        sb.AppendLine( String.Format( FileHeader, treeFilePath, DateTime.Now ));
        sb.AppendLine( "namespace GDDB" );
        sb.AppendLine( "{" );
        sb.AppendLine( GeneratedTypeAttribute );
        sb.AppendLine( "  public partial class GdDb" );
        sb.AppendLine( "  {" );
        var rootFolderClassName = $"{rootFolder.Name}Folder";
        sb.AppendLine( $"  public {rootFolderClassName} Root => new( RootFolder );" );
        sb.AppendLine( "  }" );
        sb.AppendLine( "}" );
        return sb.ToString();
    }

    public String GenerateGdDbExtensions( String treeFilePath, IReadOnlyCollection<Folder> allFolders )
    {
        var sb = new StringBuilder();
        sb.AppendLine( String.Format( FileHeader, treeFilePath, DateTime.Now ));
        sb.AppendLine( $"namespace GDDB" );
        sb.AppendLine( "{" );
        sb.AppendLine( GeneratedTypeAttribute );
        sb.AppendLine( "  public static class GdDbExtensions" );
        sb.AppendLine( "  {" );
        foreach ( var folder in allFolders )
        {
            //GenerateFolderAccess( sb, folder );
        }
           
        sb.AppendLine( $"{"  }"}" );
        sb.AppendLine( $"{"}"}" );
        return sb.ToString();
    }

    // public String GenerateGdTypeExtensions( String treeFilePath, Category rootCategory, IReadOnlyCollection<Category> allCategories )
    // {
    //     var sb = new StringBuilder();
    //     sb.AppendLine( String.Format( FileHeader, treeFilePath, DateTime.Now ));
    //     sb.AppendLine( $"namespace GDDB" );
    //     sb.AppendLine( "{" );
    //     sb.AppendLine( "  public partial struct GdType" );
    //     sb.AppendLine( "  {" );
    //     foreach ( var category in allCategories )
    //     {
    //         foreach ( var categoryItem in category.Items )
    //         {
    //             GenerateGdTypeCreate( sb, categoryItem );    
    //         }
    //         
    //     }
    //        
    //     sb.AppendLine( $"{"  }"}" );
    //     sb.AppendLine( $"{"}"}" );
    //     return sb.ToString();
    // }

    private void GenerateFolder( StringBuilder sb, Folder folder )
    {
        var classFolderName    = GetFolderClassName( folder );

        sb.AppendLine( $"//Folder {folder.Path}, subfolders {folder.SubFolders.Count}, objects {folder.ObjectIds.Count}" );
        sb.AppendLine( GeneratedTypeAttribute );
        sb.AppendLine(
                $$"""
                public readonly struct {{classFolderName}} : IEnumerable<GDObject>, IEquatable<{{classFolderName}}>
                {
                    public readonly Folder Folder;
                    
                    internal {{classFolderName}}( Folder folder )
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
                
                    public bool Equals({{classFolderName}} other)
                    {
                        return Folder.FolderGuid.Equals( other.Folder.FolderGuid );
                    }
                
                    public override bool Equals(object? obj)
                    {
                        return obj is {{classFolderName}} other && Equals( other );
                    }
                
                    public override int GetHashCode( )
                    {
                        return Folder.FolderGuid.GetHashCode();
                    }
                
                    public static bool operator ==({{classFolderName}} left, {{classFolderName}} right)
                    {
                        return left.Equals( right );
                    }
                
                    public static bool operator !=({{classFolderName}} left, {{classFolderName}} right)
                    {
                        return !left.Equals( right );
                    }
                
                #endregion
                """);

        if( folder.Parent != null )
        {
            sb.AppendLine( );
            var parentFolderTypeName = GetFolderClassName( folder.Parent );
            sb.AppendLine( $"        public {parentFolderTypeName} ParentFolder => new( Folder.Parent! );" );
        }

        if ( folder.SubFolders.Count > 0 )
        {
            sb.AppendLine( );
            sb.AppendLine( "         //Subfolder members" );
            for ( var i = 0; i < folder.SubFolders.Count; i++ )
            {
                var subFolder          = folder.SubFolders[ i ];
                var subfolderClassName = GetFolderClassName( subFolder );
                sb.AppendLine( $"        public {subfolderClassName} {GetFolderMemberName( subFolder )} => new( Folder.SubFolders[{i}] );" );
            }
        }

        sb.AppendLine( "}" );
        sb.AppendLine( );
    }

    private String GetFolderClassName( Folder folder )
    {
        //Check cached folder name
        foreach ( var folderAndName in _folderClassNames )
        {
            if( folderAndName.Folder == folder )
                return folderAndName.Name;
        }

        //Check duplicated folder name
        var folderName = GetFolderMemberName( folder ) + "Folder";
        for ( var i = 0; i < _folderClassNames.Count; i++ )
        {
            var fn = _folderClassNames[ i ];
            if( fn.Name == folderName )
            {
                fn.DuplicateCounter++;
                folderName += fn.DuplicateCounter;
                _folderClassNames[ i ] =  fn;
                _folderClassNames.Add( new FolderAndName(){Folder = folder, Name = folderName, DuplicateCounter = 1 } );
                return folderName;
            }
        }

        _folderClassNames.Add( new FolderAndName(){Folder = folder, Name = folderName, DuplicateCounter = 1  } );
        return folderName;
    }

    private static String GetFolderMemberName( Folder folder )
    {
        var folderName = folder.Name;

        if( Char.IsDigit( folderName[0] ) )
            folderName = "_" + folderName ;

        var sanitizedName = IncorrectIdentifierChars.Replace( folderName, "_" );
        return sanitizedName;
    }

    private static readonly Regex IncorrectIdentifierChars = new ( @"[^a-zA-Z0-9_]" );

    private List<FolderAndName> _folderClassNames = new ();

    private struct FolderAndName
    {
        public Folder Folder;
        public String Name;
        public Int32  DuplicateCounter;
    }

    // private static void GenerateEnum( StringBuilder sb, Category category )
    // {
    //     if( category.Type != CategoryType.Enum )
    //         return;
    //
    //     if ( category.Parent != null )        
    //         sb.AppendLine($"    //Child of {category.Parent.Name}");
    //     sb.AppendLine( GeneratedTypeAttribute );
    //     sb.AppendLine( $"    public enum E{category.Name}" );
    //     sb.AppendLine( $"    {"{"}" );
    //     foreach ( var item in category.Items )
    //     {
    //         sb.Append( $"        {item.Name} = {item.Value}," );
    //         if( item.Subcategory != null )
    //             sb.Append( $" // Has {item.Subcategory.Items.Count} items at subcategory {item.Subcategory.Name}" );
    //         sb.AppendLine();
    //     }
    //     sb.AppendLine( $"    {"}"}" );
    // }

    // private static void GenerateFolderAccess( StringBuilder sb, Folder folder )
    // {
    //     var isRootFolder = folder.Parent == null;
    //     var itemName     = folder.PartName;
    //     var folderId     = new GdId() { GUID = folder.FolderGuid};
    //
    //     var enumeratorTypeName = $"{itemName}Folder";
    //
    //     if ( isRootFolder )
    //     {
    //         sb.AppendLine( $"    //Folder id: {folderId}" );
    //         sb.AppendLine( $"    public static {enumeratorTypeName} Get{itemName}( this {GdDbTypeName} db )" );
    //         sb.AppendLine(  "    {" );
    //         sb.AppendLine( $"        return new {enumeratorTypeName}( db );" );
    //         sb.AppendLine(  "    }" );
    //         sb.AppendLine( );
    //     }
    //     else
    //     {
    //         var parentFolderypeName = $"{folder.Parent!.PartName}Folder";                                   
    //         sb.AppendLine( $"    //Folder id: {folderId}" );
    //         sb.AppendLine( $"    public static {enumeratorTypeName} Get{itemName}( this {parentFolderypeName} parentFolder )" );
    //         sb.AppendLine(  "    {" );
    //         sb.AppendLine( $"        return new {enumeratorTypeName}( parentFolder._db );" );
    //         sb.AppendLine(  "    }" );
    //         sb.AppendLine( );
    //     }
    // else                //Item is a value, return single object
    // {
    //     var categoryHierarchy = String.Join( ", ", GetCategoriesHierarchy( folder ));
    //     if ( isRootFolder )
    //     {
    //         sb.AppendLine( $"    public static GDObject Get{itemName}( this {GdDbTypeName} db )" );
    //         sb.AppendLine(  "    {" );
    //         sb.AppendLine( $"        var type = new GdType( {categoryHierarchy} );" );
    //         sb.AppendLine( $"        return db.GetObject( type );" );
    //         sb.AppendLine(  "    }" );
    //         sb.AppendLine( );
    //     }
    //     else
    //     {
    //         var parentEnumeratorTypeName = $"{folder.Owner.Name}Enumerator";                 
    //         sb.AppendLine( $"    public static GDObject Get{itemName}( this {parentEnumeratorTypeName} enumerator )" );
    //         sb.AppendLine(  "    {" );
    //         sb.AppendLine( $"        var db = enumerator._db;" );
    //         sb.AppendLine( $"        var type = new GdType( {categoryHierarchy} );" );
    //         sb.AppendLine( $"        return db.GetObject( type );" );
    //         sb.AppendLine(  "    }" );
    //         sb.AppendLine( );
    //     }
    // }

    //}

    // private static void GenerateGdTypeCreate( StringBuilder sb, CategoryItem item )
    // {
    //     if( !item.IsValue )
    //         return;
    //
    //     var itemName          = item.Name;
    //     var categoryHierarchy = GetCategoriesHierarchy( item );
    //     var namesHierarchy    = GetNamesHierarchy( item );
    //
    //     sb.AppendLine( GeneratedTypeAttribute );
    //     sb.AppendLine( $"    public static GdType Create{String.Join( null, namesHierarchy )}(  )" );
    //     sb.AppendLine(  "    {" );
    //     sb.AppendLine( $"        var result = new GdType({String.Join( ", ", categoryHierarchy )});" );
    //     sb.AppendLine( $"        return result;" );
    //     sb.AppendLine(  "    }" );
    //     sb.AppendLine( );
    //     
    // }


    // private static List<String> GetCategoriesHierarchy( CategoryItem item )
    // {
    //     var result = new List<String>();
    //
    //     var iterateItem = item;
    //     while ( iterateItem != null )
    //     {
    //         result.Insert( 0, iterateItem.Value.ToString( NumberFormatInfo.InvariantInfo ) );
    //         iterateItem  = iterateItem.Owner.ParentItem;
    //     }
    //
    //     return result;
    // }

    // private static List<String> GetNamesHierarchy( CategoryItem item )
    // {
    //     var result = new List<String>();
    //
    //     var iterateItem = item;
    //     while ( iterateItem != null )
    //     {
    //         result.Insert( 0, iterateItem.Name );
    //         iterateItem  = iterateItem.Owner.ParentItem;
    //     }
    //
    //     return result;
    // }



}