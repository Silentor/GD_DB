﻿using System;
using GDDB;

namespace TestGdDb
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
    
    [Category( typeof(MainCategory), (Int32)MainCategory.Common)]
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
        Bucks,
    }
    
    [Category( MainCategory.Mobs )]
    public enum EMobs
    {
        Humans,
        Orcs,
        Elves
    }

    [Category( EMobs.Humans )]
    public enum EHuman
    {
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