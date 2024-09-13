using System.Globalization;
using System.Reflection;
using System.Text;
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
        var rootFolderClassName = $"{rootFolder.PartName}Folder";
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

    private static void GenerateFolder( StringBuilder sb, Folder folder )
    {
        var classFolderName    = $"{folder.PartName}Folder";

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
            var parentFolderTypeName = $"{folder.Parent.PartName}Folder";
            sb.AppendLine( $"        public {parentFolderTypeName} ParentFolder => new( Folder.Parent! );" );
        }

        if ( folder.SubFolders.Count > 0 )
        {
            sb.AppendLine( );
            sb.AppendLine( "         //Subfolder members" );
            for ( var i = 0; i < folder.SubFolders.Count; i++ )
            {
                var subFolder          = folder.SubFolders[ i ];
                var subfolderClassName = $"{subFolder.PartName}Folder";
                sb.AppendLine( $"        public {subfolderClassName} {subFolder.PartName} => new( Folder.SubFolders[{i}] );" );
            }
        }

        sb.AppendLine( "}" );
        sb.AppendLine( );
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