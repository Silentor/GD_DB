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
        public override IReadOnlyList<GDObject> AllObjects => _allObjects;

        private readonly List<GDObject> _allObjects ;

        public GdScriptableLoader( String name )
        {
            var gddbReference = Resources.Load<GdScriptableReference>( $"{name}" );
            if( !gddbReference )
                throw new ArgumentException( $"GdDB name {name} is incorrect" );

            Root        = gddbReference.Root;
            _allObjects = gddbReference.Content;
        } 
    }
}
