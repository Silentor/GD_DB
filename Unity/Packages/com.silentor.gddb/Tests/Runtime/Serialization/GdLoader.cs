namespace Gddb.Serialization
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