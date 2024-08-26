#nullable enable

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GDDB.Editor
{
    public class TreeStructureParser
    {
        public Category ParseJson(string json)
        {
            var root            = JObject.Parse( json );
            if( root.First is not JProperty rootCategory )
                throw new Exception( "Root category not found" );
            return ParseCategory( rootCategory.Name, rootCategory.Value as JObject );
        }

        private static Category ParseCategory( String name, JObject jCategory, Category parentCategory = null, CategoryItem parentItem = null )
        {
            var jItems            = jCategory["Items"] as JObject;
            if ( jItems == null )
                throw new ArgumentException( "Is not valid category" );

            var result = new Category() { Name = name, ParentItem = parentItem};
            var items  = new List<CategoryItem>();
            foreach ( var jItem in jItems.Children<JProperty>() )
            {
                var      itemName    = jItem.Name;
                var      itemValue   = jItem.Value["Value"].Value<Int32>();
                var      childItems  = jItem.Value["Items"];
                var      item        = new CategoryItem(){Name = itemName, Value = itemValue, Owner = result };
                Category subcategory = null;
                if ( childItems != null )            
                    subcategory = ParseCategory( itemName, jItem.Value as JObject, result, item );
                item.Subcategory = subcategory;
                items.Add( item );
            }

            result.Items = items;
            return result;
        }
    }

    public class Category
    {
        public String             Name;
        public Category?          Parent            => ParentItem?.Owner;
        public CategoryItem?      ParentItem;
        public List<CategoryItem> Items;


    }

    public class CategoryItem
    {
        public String               Name;
        public Int32                Value;
        public Category             Owner;
        public Category?            Subcategory;
        public Boolean IsValue      => Subcategory == null;
    }

    public enum CategoryType
    {
        Int8,           //Value 8 bits
        Int16,          //Value 16 bits
        Int24,          //Value 24 bits
        Int32,          //Value 32 bits
        Enum            //Namespace or enum value, always 8 bits
    }

}