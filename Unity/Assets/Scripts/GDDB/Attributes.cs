using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;

namespace Gddb
{
    /// <summary>
    /// To restrict gd object or gd folder property to specific items 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true )]
    public class GdObjectFilterAttribute : PropertyAttribute
    {
        /// <summary>
        /// Allowed objects or folders query
        /// </summary>
        public String   Query           { get; }

        /// <summary>
        /// Only allow gd object derived from this type
        /// </summary>
        public Type     ObjectType        { get; }

        /// <summary>
        /// Only allow gd object with given components
        /// </summary>
        public IReadOnlyList<Type>   Components      { get; }

        public Boolean AllowNullReference { get; set; }                     = false;

        public GdObjectFilterAttribute( String query )
        {
            Query = query;
        }

        public GdObjectFilterAttribute( params Type[] types ) : this( null, types )
        {
        }

        public GdObjectFilterAttribute( String query, params Type[] types )
        {
            Query = query;

            List<Type> components = null;
            foreach ( var type in types )
            {
                if ( typeof(ScriptableObject).IsAssignableFrom( type )  )
                {
                    if ( ObjectType == null )
                        ObjectType = type;
                    else
                        throw new ArgumentException( "Several gd object types is not allowed", nameof(types) );
                }       
                else if ( typeof(GDComponent).IsAssignableFrom( type ) )
                {
                    if( components == null )
                        components = new List<Type>();
                    components.Add( type );
                }
                else
                {
                    throw new ArgumentException( $"Type must be derived from ScriptableObject or GDComponent, but {type.Name} was not", nameof(types) );
                }
            }

            Components = components;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true )]
    public class RequireComponentAttribute : Attribute                  //TODO add analyzer to check attribute usage
    {
        public IReadOnlyList<Type> Components { get; }

        public RequireComponentAttribute( params Type[] components )
        {
            foreach ( var component in components )
            {
                if ( !typeof(GDComponent).IsAssignableFrom( component ) )
                    throw new ArgumentException( $"Type must be derived from GDComponent, but {component.Name} was not", nameof(components) );
            }

            Components = components;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true )]
    public class ForbidComponentAttribute : Attribute                    //TODO add analyzer to check attribute usage
    {
        public IReadOnlyList<Type> Components { get; }

        public ForbidComponentAttribute( params Type[] components )
        {
            foreach ( var component in components )
            {
                if ( !typeof(GDComponent).IsAssignableFrom( component ) )
                    throw new ArgumentException( $"Type must be derived from GDComponent, but {component.Name} was not", nameof(components) );
            }

            Components = components;
        }
    }

}