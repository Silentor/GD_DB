using System;
using System.Collections.Generic;
using UnityEngine;

namespace GDDB
{
    [CreateAssetMenu( menuName = "Gddb/GDObject", fileName = "GDObject", order = 0 )]
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

        public IList<GDComponent> Components => components;

        public T GetComponent<T>() where T : GDComponent
        {
            return components.Find( c => c is T ) as T;
        }

        public Boolean HasComponent<T>() where T : GDComponent
        {
            for ( var i = 0; i < Components.Count; i++ )
            {
                var component = Components[ i ];
                if ( component == null ) continue;
                if ( typeof(T).IsAssignableFrom( component.GetType() ) )
                    return true;
            }

            return false;
        }

        public Boolean HasComponent( Type type )
        {
            if( !typeof(GDComponent).IsAssignableFrom( type ) ) throw new ArgumentException( $"Type {type.Name} must be derived from GDComponent" );
            if( type == null ) return Components.Count == 0;

            for ( var i = 0; i < Components.Count; i++ )
            {
                var component = Components[ i ];
                if ( component == null ) continue;
                if ( type.IsAssignableFrom( component.GetType() ) )
                    return true;
            }

            return false;
        }

        public Boolean HasComponents( IReadOnlyList<Type> types )
        {
            if( types == null ) return Components.Count == 0;
            _tempList.Clear();
            _tempList.AddRange( types );
            return HasComponentsInternal();
        }

        public Boolean HasComponents(params Type[] types )
        {
            if( types == null ) return Components.Count == 0;
            _tempList.Clear();
            _tempList.AddRange( types );
            return HasComponentsInternal();
        }

        public Boolean HasComponents<T1, T2> () where T1 : GDComponent where T2 : GDComponent
        {
            _tempList.Clear();
            _tempList.Add( typeof(T1) );
            _tempList.Add( typeof(T2) );
            return HasComponentsInternal();
        }

        public Boolean HasComponents<T1, T2, T3> () where T1 : GDComponent where T2 : GDComponent where T3 : GDComponent
        {
            _tempList.Clear();
            _tempList.Add( typeof(T1) );
            _tempList.Add( typeof(T2) );
            _tempList.Add( typeof(T3) );
            return HasComponentsInternal();
        }

        public new static T CreateInstance<T>( ) where T : GDObject
        {
            var result = ScriptableObject.CreateInstance<T>();
            result._guid = Guid.NewGuid();
            return result;
        }

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

        public static GDObject CreateInstance( )
        {
            var result = ScriptableObject.CreateInstance<GDObject>();
            result.SetGuid( Guid.NewGuid() );
            return result;
        }

        internal const String ComponentPropName = nameof(components); 

        [SerializeReference]
        [SerializeField]
        private List<GDComponent> components = new ();

        private Guid _guid;
        private readonly List<Type> _tempList = new ();

        private Boolean HasComponentsInternal(  )
        {
            if( _tempList == null || _tempList.Count == 0 ) return Components.Count == 0;
            var expectedCount = _tempList.Count;
            if ( Components.Count < expectedCount ) return false;

            var actualCount = 0;
            for ( var i = 0; i < Components.Count; i++ )
            {
                var component = Components[ i ];
                if ( component == null ) continue;                  //Error in GDObject

                for ( var j = 0; j < _tempList.Count; j++ )
                {
                    var t = _tempList[ j ];
                    if ( t.IsAssignableFrom( component.GetType() ) )
                    {
                        actualCount++;
                        if ( actualCount == expectedCount )
                            return true;
                        _tempList.RemoveAt( j );
                        break;
                    }
                }
            }

            return false;
        }

        internal GDObject SetGuid( Guid guid )
        {
            _guid = guid;
            return this;
        }

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
