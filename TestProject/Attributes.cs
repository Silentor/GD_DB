namespace GDDB;

/// <summary>
/// To make gd types hierarchy
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public class CategoryAttribute : System.Attribute
{
    public Type? ParentCategory { get; }
    public Int32 ParentValue    { get; }

    /// <summary>
    /// Subcategory
    /// </summary>
    /// <param name="parentCategory"></param>
    /// <param name="parentValue"></param>
    public CategoryAttribute( Type parentCategory, Int32 parentValue )
    {
        ParentCategory = parentCategory;
        ParentValue    = parentValue;
    }

    public CategoryAttribute( Object parentEnumValue )
    {
        ParentCategory = parentEnumValue.GetType();
        ParentValue    = (Int32)parentEnumValue;
    }

    /// <summary>
    /// Main category
    /// </summary>
    public CategoryAttribute(  )
    {

    }
}