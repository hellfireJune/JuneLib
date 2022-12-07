using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using JuneLib.Chests;
using MonoMod.RuntimeDetour;

namespace JuneLib
{
    public static class ChestsCore
    {
        public static Action<List<DebrisObject>, Chest> OnPostSpawnChestContents;

        public static void Init()
        {
            Hook chestSpawnItemsHook = new Hook(
                typeof(Chest).GetMethod("SpewContentsOntoGround", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(ChestActions).GetMethod("OnPostOpenBullshit"));
        }
    }
}
