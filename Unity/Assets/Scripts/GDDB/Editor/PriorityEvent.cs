using System;
using System.Collections.Generic;

namespace GDDB.Editor
{
    public class PriorityEvent
    {
        public void Invoke( )
        {
            foreach ( var item in _subscribers )
            {
                item.Value.Invoke();
            }
        }

        public void Subscribe( Int32 priority, Action action )
        {
            _subscribers.Add( priority, action );   
        }

        public void Unsubscribe( Action action )
        {
            var index = _subscribers.IndexOfValue( action );
            if( index >= 0 )
                _subscribers.RemoveAt( index );
        }

        public void Clear( )
        {
            _subscribers.Clear();
        }


        private readonly SortedList<Int32, Action> _subscribers = new();
    }
}