using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace JuneLib
{
    [HarmonyPatch]
    public static class JuneLibLootEngineModificationAPI
    {
        [HarmonyPatch(typeof(RewardManager), nameof(RewardManager.GetMultiplierForItem))]
        [HarmonyPostfix]
        public static void ModifyItemLuck(PickupObject prefab, PlayerController player, bool completesSynergy, ref float __result)
        {
            if (ItemsCore. GetItemChanceMult != null)
            {
                ModifyLuckArgs modifyLuckArgs = new ModifyLuckArgs() { WeightMult = 1f };
                ItemsCore.GetItemChanceMult(prefab, player, completesSynergy, modifyLuckArgs);
                __result *= modifyLuckArgs.WeightMult;
            }
        }

        public class ModifyLuckArgs : EventArgs
        {
            public float WeightMult;
        }
    }
}
