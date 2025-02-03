using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Animations;

namespace GDDB
{
    /// <summary>
    /// To restrict gd type value to specific categories 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true )]
    public class GdTypeFilterAttribute : Attribute
    {
        public String   Query           { get; }
        public Type[]   Components      { get; }

        public GdTypeFilterAttribute( )
        {
            Query = null;
        }
        
        public GdTypeFilterAttribute( String query )
        {
            Query = query;
        }

        public GdTypeFilterAttribute( String query, params Type[] componentTypes )
        {
            Query = query;

            if( componentTypes != null && componentTypes.Any( c => !typeof(GDComponent).IsAssignableFrom( c ) ) )
                throw new ArgumentException( $"Component type must be derived from GDComponent", nameof(componentTypes) );
            Components = componentTypes;
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