using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace GDDB
{
    [CreateAssetMenu( menuName = "Create GDObject", fileName = "GDObject", order = 0 )]
    public class GDObject : ScriptableObject
    {
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

        //[SerializeField]public Byte[] SerGuid;

        public String Name => name;


        [SerializeReference]
        public List<GDComponent> Components = new List<GDComponent>();

        public T GetComponent<T>() where T : GDComponent
        {
            return Components.Find( c => c is T ) as T;
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

        protected virtual void OnEnable( )
        {
            Debug.Log( $"OnEnable GDObject {name} guid {Guid}" );
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
