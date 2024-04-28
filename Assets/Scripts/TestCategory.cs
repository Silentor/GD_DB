using System;
using GDDB;

namespace TestGdDb
{
    [MainCategory]
    public enum TestCategory
    {
        Game,
        [Subcategory(typeof(EMobs))]
        Mobs,
        Items,
        Weapons,
        Spells,
        InApps,
        Locations = 10
    }

    [Category(typeof(TestCategory), (Int32)TestCategory.Game)]
    public enum EGame
    {
        GameModes,
        Currencies
    }

    [Category(typeof(EGame), (Int32)EGame.Currencies)]
    public enum ECurrency
    {
        Silver,
        Gold,
        Gems,
        Bucks
    }


    public enum EMobs
    {
        Humans,
        Orcs,
        Elves
    }

    public enum EItems
    {
        Consumables,
        Reagents,
        QuestItems,

    }

    public enum EWeapons
    {
        Swords,
        Bows,
        Axes
    }

    public enum EInApps
    {
        Skins,
        Weapons,
        Consumables
    }
}