using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GDDB
{
    public class GdJsonLoader : GdLoader
    {
        public override IReadOnlyList<GDObject> AllObjects => _allObjects;

        private readonly List<GDObject> _allObjects ;

        public GdJsonLoader( String name )
        {
            var jsonAsset = Resources.Load<TextAsset>( $"{name}" );
            if( !jsonAsset )
                throw new ArgumentException( $"GdDB name {name} is incorrect" );

            var gdJson = new GDJson();
            var content = gdJson.JsonToGD( jsonAsset.text );

            Root = content.First( gd => gd is GDRoot ) as GDRoot;
            _allObjects = content;
        } 
    }
}
