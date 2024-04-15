using System;
using System.Collections.Generic;
using System.Linq;

namespace GDDB
{
    public abstract class GdLoader
    {
        public GdDb GetGameDataBase( )
        {
            return _db;
        }

        

        protected GdDb _db;

    }
}