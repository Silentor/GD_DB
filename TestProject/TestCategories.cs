using GDDB;

namespace ClientCode;

public class TestCategories
{
    [Category]
    public enum MainCategory
    {
        Common = 1,
        Mobs,
        Items,
        Weapons,
        Spells,
        InApps,
        Locations = 10
    }
}