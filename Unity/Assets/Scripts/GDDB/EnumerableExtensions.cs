using System;
using System.Collections.Generic;

namespace Gddb
{
    public static class EnumerableExtensions
    {
        public static      Boolean TryFirst<T>( this IEnumerable<T> enumerable, Predicate<T> predicate, out T value )
        {
            value = default;
            if ( enumerable == null )
            {
                return false;
            }

            foreach ( var item in enumerable )
            {
                if ( predicate( item ) )
                {
                    value = item;
                    return true;
                }
            }

            return false;
        }

        public static      Boolean TryElementAt<T>( this IReadOnlyList<T> enumerable, Int32 elementIndex, out T value )
        {
            value = default;
            if ( enumerable == null )
                return false;

            if ( elementIndex < enumerable.Count )
            {
                value = enumerable[ elementIndex ];
                return true;
            }

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