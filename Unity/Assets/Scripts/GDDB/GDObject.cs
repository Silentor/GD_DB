using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace GDDB
{
    [CreateAssetMenu( menuName = "Create GDObject", fileName = "GDObject", order = 0 )]
    public class GDObject : ScriptableObject
    {
        [HideInInspector]
        public Boolean EnabledObject            = true;           //To temporary disable object in GD_DB queries

        //public GdType Type;

        public Guid Guid
        {
            get
            {
                if ( _guid == default )
                {
                    _guid = GetAssetGuid();
                    if( _guid == default )
                        Debug.LogWarning( $"Cannot get asset {name} guid, runtime creation?" );
                }

                return _guid;
            }
        }

        public String Name => name;


        [SerializeReference]
        public List<GDComponent> Components = new List<GDComponent>();

        public T GetComponent<T>() where T : GDComponent
        {
            return Components.Find( c => c is T ) as T;
        }

        public Boolean HasComponent<T>() where T : GDComponent
        {
            return Components.Exists( c => c is T );
        }

        public Boolean HasComponent( Type type )
        {
            if( type == null ) return Components.Count == 0;

            return Components.Exists( c => type.IsAssignableFrom( c.GetType() ) );
        }

        public Boolean HasComponents( params Type[] types )
        {
            if( types == null || types.Length == 0 ) return Components.Count == 0;

            foreach ( var t in types )
            {
                if( !Components.Exists( c => t.IsAssignableFrom( c.GetType() ) ) )
                    return false;
            }

            return true;
        }

        internal GDObject WithGuid( Guid guid )
        {
            _guid = guid;
            return this;
        }

        public new static T CreateInstance<T>( ) where T : GDObject
        {
            var result = ScriptableObject.CreateInstance<T>();

            //Set runtime temporary guid, do not equal to asset guid if saved as asset
            result._guid = Guid.NewGuid();
            return result;
        }


        public new static GDObject CreateInstance( Type type )
        {
            Assert.IsTrue( typeof(GDObject).IsAssignableFrom( type ) );

            var result = (GDObject)ScriptableObject.CreateInstance( type );

            //Set runtime temporary guid, do not equal to asset guid if saved as asset
            result._guid = Guid.NewGuid();
            return result;
        }

        private Guid GetAssetGuid( )
        {
            if ( AssetDatabase.TryGetGUIDAndLocalFileIdentifier( this, out var guid, out long localId ) )
            {
                return Guid.Parse( guid );
            }

            return default;
        }

        private Guid _guid;
    }

    [Serializable]
    public abstract class GDComponent
    {

    }
    
}
