using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JuneLib.Chests
{
    public class ChestModifyItem : PassiveItem
    {
        public static Action<Chest> modifyChestAction;

        public override void Pickup(PlayerController player)
        {
            base.Pickup(player);
            if (!this.m_pickedUpThisRun && modifyChestAction != null)
            {
                ChestHelpers.ForEveryChest(modifyChestAction);
            }
        }


        public static void AddModifyChestAction(Action<Chest> self)
        {
            modifyChestAction += self;
            Alexandria.Misc.CustomActions.OnChestPostSpawn += self;
        }
    }
}
