using UnityEditor;
using UnityEngine.UIElements;

namespace GDDB.Editor
{
    [CustomPropertyDrawer( typeof(TYPE) )]
    public class GdFilterDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            
            return base.CreatePropertyGUI( property );
        }
    }
}