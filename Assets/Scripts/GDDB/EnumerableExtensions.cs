using System;
using System.Collections.Generic;

namespace GDDB
{
    public static class EnumerableExtensions
    {
        public static      Boolean TryFirst<T>( this IEnumerable<T> enumerable, Func<T, Boolean> predicate, out T value )
        {
            foreach ( var item in enumerable )
            {
                if ( predicate( item ) )
                {
                    value = item;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}