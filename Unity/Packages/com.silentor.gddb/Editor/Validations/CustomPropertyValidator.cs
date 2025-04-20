using System;

namespace Gddb.Editor.Validations
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class CustomPropertyValidator : Attribute
    {
        public readonly Type ValidationAttributeType;

        public CustomPropertyValidator(Type validationAttributeType )
        {
            ValidationAttributeType = validationAttributeType;
        }
    }
}