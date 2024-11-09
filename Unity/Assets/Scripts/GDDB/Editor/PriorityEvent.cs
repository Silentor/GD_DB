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
                item.Item2.Invoke();
            }
        }

        public void Subscribe( Action action )
        {
            AddItem( 0, action );   
        }

        public void Subscribe( Int32 priority, Action action )
        {
            AddItem( priority, action );   
        }

        public void Unsubscribe( Action action )
        {
            var index = _subscribers.FindIndex( item => item.Item2 == action );
            if( index >= 0 )
                _subscribers.RemoveAt( index );
        }

        public void Clear( )
        {
            _subscribers.Clear();
        }

        private void AddItem( Int32 priority, Action action )
        {
            for ( int i = 0; i < _subscribers.Count; i++ )
            {
                if(_subscribers[i].Item2 == action)                
                    _subscribers.RemoveAt( i );
            }

            for ( var i = 0; i < _subscribers.Count; i++ )
            {
                if ( _subscribers[i].Item1 > priority )
                {
                    _subscribers.Insert( i, (priority, action) );
                    return;
                }
            }

            _subscribers.Add( (priority, action) );
        }

        private readonly List<(Int32, Action)> _subscribers = new();
    }

    public class PriorityEvent<TParam>
    {
        public void Invoke( TParam param1 )
        {
            foreach ( var item in _subscribers )
            {
                item.Item2.Invoke( param1 );
            }
        }

        public void Subscribe( Action<TParam> action )
        {
            AddItem( 0, action );   
        }

        public void Subscribe( Int32 priority, Action<TParam> action )
        {
            AddItem( priority, action );   
        }

        public void Unsubscribe( Action<TParam> action )
        {
            var index = _subscribers.FindIndex( item => item.Item2 == action );
            if( index >= 0 )
                _subscribers.RemoveAt( index );
        }

        public void Clear( )
        {
            _subscribers.Clear();
        }

        private void AddItem( Int32 priority, Action<TParam> action )
        {
            for ( int i = 0; i < _subscribers.Count; i++ )
            {
                if(_subscribers[i].Item2 == action)                
                    _subscribers.RemoveAt( i );
            }

            for ( var i = 0; i < _subscribers.Count; i++ )
            {
                if ( _subscribers[i].Item1 > priority )
                {
                    _subscribers.Insert( i, (priority, action) );
                    return;
                }
            }

            _subscribers.Add( (priority, action) );
        }

        private readonly List<(Int32, Action<TParam>)> _subscribers = new();
    }

    public class PriorityEvent<TParam1, TParam2>
    {
        public void Invoke( TParam1 param1, TParam2 param2 )
        {
            foreach ( var item in _subscribers )
            {
                item.Item2.Invoke( param1, param2 );
            }
        }

        public void Subscribe( Action<TParam1, TParam2> action )
        {
            AddItem( 0, action );   
        }

        public void Subscribe( Int32 priority,  Action<TParam1, TParam2> action )
        {
            AddItem( priority, action );   
        }

        public void Unsubscribe(  Action<TParam1, TParam2> action )
        {
            var index = _subscribers.FindIndex( item => item.Item2 == action );
            if( index >= 0 )
                _subscribers.RemoveAt( index );
        }

        public void Clear( )
        {
            _subscribers.Clear();
        }

        private void AddItem( Int32 priority,  Action<TParam1, TParam2> action )
        {
            for ( int i = 0; i < _subscribers.Count; i++ )
            {
                if(_subscribers[i].Item2 == action)                
                    _subscribers.RemoveAt( i );
            }

            for ( var i = 0; i < _subscribers.Count; i++ )
            {
                if ( _subscribers[i].Item1 > priority )
                {
                    _subscribers.Insert( i, (priority, action) );
                    return;
                }
            }

            _subscribers.Add( (priority, action) );
        }

        private readonly List<(Int32, Action<TParam1, TParam2> )> _subscribers = new();
    }
}