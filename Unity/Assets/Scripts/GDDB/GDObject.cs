using System;
using System.Collections.Generic;
using UnityEngine;

namespace GDDB
{
    [CreateAssetMenu( menuName = "Create GDObject", fileName = "GDObject", order = 0 )]
    public class GDObject : ScriptableObject
    {
        public Boolean EnabledObject            = true;           //To temporary disable object in GD_DB queries

        public Guid Guid
        {
            get
            {
#if UNITY_EDITOR
                if ( _guid == Guid.Empty )
                {
                    //GDObject do not store guid in asset, we just use AssetDatabase guid if needed
                    _guid = EditorGetAssetGuid();
                    if ( _guid == Guid.Empty )
                    {
                        Debug.LogWarning( $"[GDObject] Cannot get GDObject asset guid, probably GDObject was created via ScriptableObject.CreateInstance(), but should via GDObject.CreateInstance(), name {Name}. " +
                                          $"Assigned temporary guid, will be changed if saved to AssetDatabase." );
                        _guid = Guid.NewGuid();
                    }
                }
#else 

                if ( _guid == default )  //Must be loaded form GD database OR set on runtime creation TODO consider support it
                {
                    throw new InvalidOperationException($"[GDObject] Guid is not set, objects {name}");
                }
#endif

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

        public Boolean HasComponents( IReadOnlyList<Type> types )
        {
            if( types == null || types.Count == 0 ) return Components.Count == 0;

            foreach ( var t in types )
            {
                if( !Components.Exists( c => t.IsAssignableFrom( c.GetType() ) ) )
                    return false;
            }

            return true;
        }

        internal GDObject SetGuid( Guid guid )
        {
            _guid = guid;
            return this;
        }


        // public new static T CreateInstance<T>( ) where T : GDObject
        // {
        //     var result = ScriptableObject.CreateInstance<T>();
        //     //result.name = typeof(T).Name + result.GetInstanceID().ToString();
        //     result._guid = Guid.NewGuid();
        //     return result;
        // }

        /// <summary>
        /// Creeate instance of given type (scriptable object or GDObject) and cast result to T type
        /// </summary>
        /// <param name="type"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateInstance<T>( Type type ) where T : ScriptableObject
        {
            return (T)CreateInstance( type );
        }

        /// <summary>
        /// Create gd object of given type (scriptable object or GDObject)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public new static ScriptableObject CreateInstance( Type type )
        {
            if( !typeof(ScriptableObject).IsAssignableFrom( type ) )
                throw new ArgumentException( $"Type {type.Name} must be derived from ScriptableObject" );

            var result = ScriptableObject.CreateInstance( type );
            if( result is GDObject gdo )
            {
                gdo.SetGuid( Guid.NewGuid() );
                return gdo;
            }
            return result;
        }

        private Guid _guid;

#if UNITY_EDITOR
        private Guid EditorGetAssetGuid( )
        {
            if ( UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier( this, out var guid, out _ ) )
            {
                return Guid.ParseExact( guid, "N" );
            }

            return default;
        }
#endif
    }

    [Serializable]
    public abstract class GDComponent
    {

    }


    
}
