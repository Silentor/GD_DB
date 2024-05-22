using System;
using System.Collections.Generic;

namespace GDDB
{
    public static class EnumerableExtensions
    {
        public static      Boolean TryFirst<T>( this IEnumerable<T> enumerable, Predicate<T> predicate, out T value )
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

        public static      Int32 FindIndex<T>( this IEnumerable<T> enumerable, Predicate<T> predicate )
        {
            var index = 0;
            foreach ( var item in enumerable )
            {
                if ( predicate( item ) )
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
}