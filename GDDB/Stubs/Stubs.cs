namespace GDDB;

public  class GDObject
{

}

public class GdDb
{
    public IEnumerable<GDObject> GetObjects( Int32 mainCategory, Int32 subCategory )
    {
        return Array.Empty<GDObject>();
    }

    public IEnumerable<GDObject> GetObjects( Int32 mainCategory )
    {
        return Array.Empty<GDObject>();
    }

    public GDObject? GetObject( GdType type )
    {
        return null;
    }
}

public partial struct GdType
{
    public Int32 this[Int32 index]
    {
        get { return 0; }
        set {  }
    }

    public GdType( Int32 cat1, Int32 cat2 = 0, Int32 cat3 = 0, Int32 cat4 = 0 )
    {

    }

    // public static GdType Create( ERoot rootCategory, ECurrencies currenciesCategory )       //Not scalable
    // {
    //     var result = new GdType();
    //     result[0] = (Int32)rootCategory;
    //     result[1] = (Int32)currenciesCategory;
    //     return result;
    // }
    //
    // public static GdType Create( ERoot rootCategory, ECurrencies currenciesCategory, ETokens tokensCategory )       //Not scalable
    // {
    //     var result = new GdType();
    //     result[0] = (Int32)rootCategory;
    //     result[1] = (Int32)currenciesCategory;
    //     result[2] = (Int32)tokensCategory;
    //     return result;
    // }
    //
    // public static GdType Create( ERoot rootCategory, EMobs mobsCategory )       //Not scalable
    // {
    //     var result = new GdType();
    //     result[0] = (Int32)rootCategory;
    //     result[1] = (Int32)mobsCategory;
    //     return result;
    // }


    // public static GdType CreateCurrenciesTokensArmorType(  )
    // {
    //     var result = new GdType( (Int32)ERoot.Currencies, (Int32)ECurrencies.Copper, (Int32)ETokens.Armor );
    //     //result[0] = (Int32)ERoot.Currencies;
    //     //result[1] = (Int32)ECurrencies.Copper;
    //     //result[2] = (Int32)ETokens.Armor;
    //     return result;
    // }

}