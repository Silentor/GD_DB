using System.Globalization;
using System.Reflection;
using System.Text;
using GDDB.Editor;
using Newtonsoft.Json.Linq;

namespace GDDB.SourceGenerator;

public class CodeEmitter
{
    private static readonly String AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
    private static readonly String AssemblyVersion = Assembly.GetExecutingAssembly(). GetName().Version.ToString();
    private static readonly String FileHeader = "//Generated from {0} at {1}";
    private static readonly String GeneratedTypeAttribute = $"    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(\"{AssemblyName}\", \"{AssemblyVersion}\")]";
    private static readonly String GdDbTypeName = "GdDb";
    

    public String GenerateEnums( String treeFilePath, Category rootCategory, IReadOnlyCollection<Category> allCategories  )
    {
        var sb = new StringBuilder();
        sb.AppendLine( String.Format( FileHeader, treeFilePath, DateTime.Now ));
        sb.AppendLine( $"namespace GDDB" );
        sb.AppendLine( "{" );
        foreach ( var category in allCategories )
        {
            GenerateEnum( sb, category );    
        }
           
        sb.AppendLine( $"{"}"}" );
        return sb.ToString();
    }

    public String GenerateEnumerators( String treeFilePath, Category rootCategory, IReadOnlyCollection<Category> allCategories  )
    {
        var sb = new StringBuilder();
        sb.AppendLine( String.Format( FileHeader, treeFilePath, DateTime.Now ));
        sb.AppendLine( $"namespace GDDB" );
        sb.AppendLine( "{" );
        foreach ( var category in allCategories )
        {
            foreach ( var categoryItem in category.Items )
            {
                GenerateEnumerator( sb, category, categoryItem );
            }
        }
           
        sb.AppendLine( $"{"}"}" );
        return sb.ToString();
    }

    public String GenerateGdDbExtensions( String treeFilePath, Category rootCategory, IReadOnlyCollection<Category> allCategories )
    {
        var sb = new StringBuilder();
        sb.AppendLine( String.Format( FileHeader, treeFilePath, DateTime.Now ));
        sb.AppendLine( $"namespace GDDB" );
        sb.AppendLine( "{" );
        sb.AppendLine( GeneratedTypeAttribute );
        sb.AppendLine( "    public static class GdDbExtensions" );
        sb.AppendLine( "    {" );
        foreach ( var category in allCategories )
        {
            foreach ( var categoryItem in category.Items )
            {
                GenerateCategoryItemAccess( sb, categoryItem );    
            }
            
        }
           
        sb.AppendLine( $"{"    }"}" );
        sb.AppendLine( $"{"}"}" );
        return sb.ToString();
    }

    public String GenerateGdTypeExtensions( String treeFilePath, Category rootCategory, IReadOnlyCollection<Category> allCategories )
    {
        var sb = new StringBuilder();
        sb.AppendLine( String.Format( FileHeader, treeFilePath, DateTime.Now ));
        sb.AppendLine( $"namespace GDDB" );
        sb.AppendLine( "{" );
        sb.AppendLine( "  public partial struct GdType" );
        sb.AppendLine( "  {" );
        foreach ( var category in allCategories )
        {
            foreach ( var categoryItem in category.Items )
            {
                GenerateGdTypeCreate( sb, categoryItem );    
            }
            
        }
           
        sb.AppendLine( $"{"  }"}" );
        sb.AppendLine( $"{"}"}" );
        return sb.ToString();
    }

    private static void GenerateEnumerator( StringBuilder sb, Category owner, CategoryItem item )
    {
        if( item.IsValue )
            return;

        var classFilterName         = $"{item.Name}Enumerator";
        var myCategoryName  = owner. Name;
        var myCategoryItemName = item.Name;

        var filterParams = new List<String>();
        while ( item != null )
        {
            filterParams.Insert( 0, item.Value.ToString(CultureInfo.InvariantCulture) );
            item  = owner.ParentItem;
            owner = owner.Parent;
        }

        sb.AppendLine( GeneratedTypeAttribute );
        sb.AppendLine(
                $$"""
                    public class {{classFilterName}} : global::System.Collections.Generic.IEnumerable<GDObject>
                    {
                        internal readonly {{GdDbTypeName}} _db;
                    
                        public {{classFilterName}}( {{GdDbTypeName}} db )
                        {
                            _db = db;
                        }
                    
                        public global::System.Collections.Generic.IEnumerator<GDObject> GetEnumerator( )
                        {
                            foreach ( var gdo in _db.GetObjects( {{String.Join( ", ", filterParams )}} ) )
                                yield return gdo;
                        }
                    
                        global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator( )
                        {
                            return GetEnumerator();
                        }
                    }
                """);
    }


    private static void GenerateEnum( StringBuilder sb, Category category )
    {
        if ( category.Parent != null )        
            sb.AppendLine($"    //Child of {category.Parent.Name}");
        sb.AppendLine( GeneratedTypeAttribute );
        sb.AppendLine( $"    public enum E{category.Name}" );
        sb.AppendLine( $"    {"{"}" );
        foreach ( var item in category.Items )
        {
            sb.Append( $"        {item.Name} = {item.Value}," );
            if( item.Subcategory != null )
                sb.Append( $" // Has {item.Subcategory.Items.Count} items at subcategory {item.Subcategory.Name}" );
            sb.AppendLine();
        }
        sb.AppendLine( $"    {"}"}" );
    }

    private static void GenerateCategoryItemAccess( StringBuilder sb, CategoryItem item )
    {
        var isRootCategory = item.Owner.Parent == null;
        var itemName = item.Name;
        if ( !item.IsValue )       //Item is a category, return enumerator over children
        {
            var enumeratorTypeName = $"{itemName}Enumerator";

            if ( isRootCategory )
            {
                sb.AppendLine( $"    public static {enumeratorTypeName} Get{itemName}( this {GdDbTypeName} db )" );
                sb.AppendLine(  "    {" );
                sb.AppendLine( $"        return new {enumeratorTypeName}( db );" );
                sb.AppendLine(  "    }" );
                sb.AppendLine( );
            }
            else
            {
                var parentEnumeratorTypeName = $"{item.Owner.Name}Enumerator";                                   
                sb.AppendLine( $"    public static {enumeratorTypeName} Get{itemName}( this {parentEnumeratorTypeName} enumerator )" );
                sb.AppendLine(  "    {" );
                sb.AppendLine( $"        var db = enumerator._db;" );
                sb.AppendLine( $"        return new {enumeratorTypeName}( db );" );
                sb.AppendLine(  "    }" );
                sb.AppendLine( );
            }
        }
        else                //Item is a value, return single object
        {
            var categoryHierarchy = String.Join( ", ", GetCategoriesHierarchy( item ));
            if ( isRootCategory )
            {
                sb.AppendLine( $"    public static GDObject Get{itemName}( this {GdDbTypeName} db )" );
                sb.AppendLine(  "    {" );
                sb.AppendLine( $"        var type = new GdType( {categoryHierarchy} );" );
                sb.AppendLine( $"        return db.GetObject( type );" );
                sb.AppendLine(  "    }" );
                sb.AppendLine( );
            }
            else
            {
                var parentEnumeratorTypeName = $"{item.Owner.Name}Enumerator";                 
                sb.AppendLine( $"    public static GDObject Get{itemName}( this {parentEnumeratorTypeName} enumerator )" );
                sb.AppendLine(  "    {" );
                sb.AppendLine( $"        var db = enumerator._db;" );
                sb.AppendLine( $"        var type = new GdType( {categoryHierarchy} );" );
                sb.AppendLine( $"        return db.GetObject( type );" );
                sb.AppendLine(  "    }" );
                sb.AppendLine( );
            }
        }
        
    }

    private static void GenerateGdTypeCreate( StringBuilder sb, CategoryItem item )
    {
        if( !item.IsValue )
            return;

        var itemName          = item.Name;
        var categoryHierarchy = GetCategoriesHierarchy( item );
        var namesHierarchy    = GetNamesHierarchy( item );

        sb.AppendLine( GeneratedTypeAttribute );
        sb.AppendLine( $"    public static GdType Create{String.Join( null, namesHierarchy )}(  )" );
        sb.AppendLine(  "    {" );
        sb.AppendLine( $"        var result = new GdType({String.Join( ", ", categoryHierarchy )});" );
        sb.AppendLine( $"        return result;" );
        sb.AppendLine(  "    }" );
        sb.AppendLine( );
        
    }


    private static List<String> GetCategoriesHierarchy( CategoryItem item )
    {
        var result = new List<String>();

        var iterateItem = item;
        while ( iterateItem != null )
        {
            result.Insert( 0, iterateItem.Value.ToString( NumberFormatInfo.InvariantInfo ) );
            iterateItem  = iterateItem.Owner.ParentItem;
        }

        return result;
    }

    private static List<String> GetNamesHierarchy( CategoryItem item )
    {
        var result = new List<String>();

        var iterateItem = item;
        while ( iterateItem != null )
        {
            result.Insert( 0, iterateItem.Name );
            iterateItem  = iterateItem.Owner.ParentItem;
        }

        return result;
    }
        
       

}