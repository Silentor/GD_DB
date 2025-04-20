using System;
using UnityEngine;

namespace Gddb.Validations
{
    /// <summary>
    /// Inherits this attribute or apply separately to field to apply validation to entire collection instead of every item 
    /// </summary>
    [Obsolete("Not used now, application scope moved to validator")]
    public class CollectionLevelValidationAttribute : PropertyAttribute
    {
        
    }
}