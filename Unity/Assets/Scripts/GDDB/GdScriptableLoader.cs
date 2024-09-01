using System;
using System.Collections.Generic;
using UnityEngine;

namespace GDDB
{
    /// <summary>
    /// GD DB laoder from Scriptable Objects in Resources folder 
    /// </summary>
    public class GdScriptableLoader : GdLoader
    {
        public GdScriptableLoader( String name )
        {
            var gddbReference = Resources.Load<GdScriptableReference>( $"{name}" );
            if( !gddbReference )
                throw new ArgumentException( $"GdDB name {name} is incorrect" );

            _db = new GdDb( null, gddbReference.Content );
        } 
    }
}
