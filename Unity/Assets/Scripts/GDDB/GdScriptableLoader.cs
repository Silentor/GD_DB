using System;
using System.Linq;
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
                folder.Objects.Capacity = folder.ObjectIds.Count;
                foreach ( var id in folder.ObjectIds )
                {
                    foreach ( var obj in gddbReference.Content )           //todo Optimize search by parallel iteration
                    {
                        if ( obj.Guid.Guid == id )
                        {
                            obj.Object.SetGuid( id );
                            folder.Objects.Add( obj.Object );
                            break;
                        }
                    }
                }
            }

            _db = new GdDb( rootFolder, gddbReference.Content.Select( gdo => gdo.Object ).ToArray() ); 

            timer.Stop();
            Debug.Log( $"[GdScriptableLoader] GD data base '{name}' loaded in {timer.ElapsedMilliseconds} msec, gdobjects stored {gddbReference.Content.Length}, loaded {_db.AllObjects.Count}" );
        } 
    }
}
