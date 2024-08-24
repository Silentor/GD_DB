using System.Text;
using Newtonsoft.Json.Linq;

namespace GDDB.SourceGenerator;

public static class Parser
{
    public static Category ParseJson(string json)
    {
        var root            = JObject.Parse( json );
        if( root.First is not JProperty rootCategory )
            throw new Exception( "Root category not found" );
        return ParseCategory( rootCategory.Name, rootCategory.Value as JObject );
    }

    private static Category ParseCategory( String name, JObject jCategory, Category parentCategory = null, Int32 parentItemValue = 0 )
    {
        var jItems            = jCategory["Items"] as JObject;
        if ( jItems == null )
            throw new ArgumentException( "Is not valid category" );

        var result = new Category() { Name = name, Parent = parentCategory, ParentItemValue = parentItemValue};
        var items  = new List<Item>();
        foreach ( var jItem in jItems.Children<JProperty>() )
        {
            var      itemName                                = jItem.Name;
            var      itemValue                               = jItem.Value["Value"].Value<Int32>();
            var      childItems                              = jItem.Value["Items"];
            Category subcategory                             = null;
            if ( childItems != null )            
                subcategory = ParseCategory( itemName, jItem.Value as JObject, result, itemValue );
            items.Add( new Item{ Name = itemName, Value = itemValue, Subcategory = subcategory} );
        }

        result.Items = items;
        return result;
    }

    public static String GenerateEnums( String treeFilePath, Category rootCategory )
    {
        var categories = new List<Category>(); 
        ListCategories( rootCategory, categories );
    
        var sb = new StringBuilder();
        sb.AppendLine( $"//{DateTime.Now}" );
        sb.AppendLine( $"//{treeFilePath}" );
        sb.AppendLine( $"namespace GDDB" );
        sb.AppendLine( "{" );
        foreach ( var category in categories )
        {
            GenerateEnum( sb, category );    
        }
           
        sb.AppendLine( $"{"}"}" );
        return sb.ToString();
    }

    public static String GenerateFilters( String treeFilePath, Category rootCategory )
    {
        var categories = new List<Category>(); 
        ListCategories( rootCategory, categories );
    
        var sb = new StringBuilder();
        sb.AppendLine( $"//{DateTime.Now}" );
        sb.AppendLine( $"//{treeFilePath}" );
        sb.AppendLine( $"namespace GDDB" );
        sb.AppendLine( "{" );
        foreach ( var category in categories )
        {
            GenerateFilter( sb, category );    
        }
           
        sb.AppendLine( $"{"}"}" );
        return sb.ToString();
    }



    private static void ListCategories( Category rootCategory, List<Category> result )
    {
        result.Add( rootCategory );
        foreach ( var item in rootCategory.Items )
        {
            if ( item.Subcategory != null )
            {
                ListCategories( item.Subcategory, result );
            }
        }
    }

    private static void GenerateFilter( StringBuilder builder, Category category )
    {
        //Root category has no filter
        if( category.Parent == null )
            return;

        var classFilterName        = $"{category.Name}Filter";
        var parentCategoryItemName = category.Parent.Name;
        var parentCategoryItemValue = category.Name;
        builder.AppendLine(
                $$"""
                    public class {{classFilterName}} : IEnumerable<GDObject>
                    {
                        internal readonly GdDb _db;
                    
                        public {{classFilterName}}( GdDb db )
                        {
                            _db = db;
                        }
                    
                        public IEnumerator<GDObject> GetEnumerator( )
                        {
                            foreach ( var gdo in _db.GetObjects( (Int32){{parentCategoryItemName}}.{{parentCategoryItemValue}} ) )
                                yield return gdo;
                        }
                    
                        IEnumerator IEnumerable.GetEnumerator( )
                        {
                            return GetEnumerator();
                        }
                    }
                """);
    }


    private static void GenerateEnum( StringBuilder builder, Category category )
    {
        if ( category.Parent != null )        
            builder.AppendLine($"    //Child of {category.Parent.Name}");
        builder.AppendLine( $"    public enum E{category.Name}" );
        builder.AppendLine( $"    {"{"}" );
        foreach ( var item in category.Items )
        {
            builder.Append( $"        {item.Name} = {item.Value}," );
            if( item.Subcategory != null )
                builder.Append( $" // Has {item.Subcategory.Items.Count} items at subcategory {item.Subcategory.Name}" );
            builder.AppendLine();
        }
        builder.AppendLine( $"    {"}"}" );
        
    }

    public class Category
    {
        public String     Name;
        public Category?  Parent;
        public Int32      ParentItemValue;
        public List<Item> Items;
    }

    public class Item
    {
        public String    Name;
        public Int32     Value;
        public Category? Subcategory;
    }


}