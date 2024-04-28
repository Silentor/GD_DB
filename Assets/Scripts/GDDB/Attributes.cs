using System;

namespace GDDB
{
    [AttributeUsage(AttributeTargets.Enum)]
    public class MainCategoryAttribute : System.Attribute
    {
        
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SubcategoryAttribute : System.Attribute
    {
        public Type SubcategoryEnum { get; }

        public SubcategoryAttribute( Type subcategoryEnum )
        {
            SubcategoryEnum = subcategoryEnum;
        }
    }

    [AttributeUsage(AttributeTargets.Enum)]
    public class CategoryAttribute : System.Attribute
    {
        public Type  ParentCategory { get; }
        public Int32 ParentValue    { get; }

        /// <summary>
        /// Subcategory
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <param name="parentValue"></param>
        public CategoryAttribute( Type parentCategory, Int32 parentValue )
        {
            ParentCategory   = parentCategory;
            ParentValue = parentValue;
        }

        /// <summary>
        /// Main category
        /// </summary>
        public CategoryAttribute(  )
        {

        }
    }


}