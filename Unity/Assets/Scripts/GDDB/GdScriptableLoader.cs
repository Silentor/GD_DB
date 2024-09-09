using System;
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
            var timer = System.Diagnostics.Stopwatch.StartNew();

            var gddbReference = Resources.Load<GdScriptableReference>( $"{name}.objects" );
            if( !gddbReference )
                throw new ArgumentException( $"GdDB name {name} is incorrect" );

            var structure  = Resources.Load<TextAsset>( $"{name}.structure" );
            var serializer = new FoldersSerializer();
            var rootFolder = serializer.Deserialize( structure.text );

            //Restore objects in hierarchy
            foreach ( var folder in rootFolder.EnumerateFoldersDFS(  ) )
            {
                foreach ( var gdAsset in folder.Objects )
                {
                    var guid = gdAsset.AssetGuid;
                    foreach ( var obj in gddbReference.Content )
                    {
                        if ( obj.Guid == guid )
                        {
                            gdAsset.Asset = obj;
                            break;
                        }
                    }
                }
            }

            _db = new GdDb( rootFolder, gddbReference.Content );

            timer.Stop();
            Debug.Log( $"[GdScriptableLoader] GD data base {name} loaded in {timer.ElapsedMilliseconds} msec" );
        } 
    }
}
