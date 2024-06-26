using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace GDDB
{
    /// <summary>
    /// GD DB loader from JSON file in Resources folder or from any TextReader
    /// </summary>
    public class GdJsonLoader : GdLoader
    {
        public GdJsonLoader( String name )
        {
            var jsonAsset = Resources.Load<TextAsset>( $"{name}" );
            if( !jsonAsset )
                throw new ArgumentException( $"GdDB name {name} is incorrect" );

            var gdJson = new GDJson();
            var content = gdJson.JsonToGD( jsonAsset.text );

            _db = new GdDb( content );
        }
        
        public GdJsonLoader( [NotNull] TextReader jsonContent )
        {
            if ( jsonContent == null ) throw new ArgumentNullException( nameof(jsonContent) );

            var gdJson  = new GDJson();
            var content = gdJson.JsonToGD( jsonContent.ReadToEnd() );

            _db  = new GdDb( content );
        } 
    }
}
