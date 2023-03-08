using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace JuneLib
{
    [HarmonyPatch]
    public static class JuneLibLootEngineModificationAPI
    {
        [HarmonyPatch(typeof(RewardManager), nameof(RewardManager.GetMultiplierForItem))]
        [HarmonyPostfix]
        internal static void ModifyItemLuck(PickupObject prefab, PlayerController player, bool completesSynergy, ref float __result)
        {
            if (ItemsCore.GetItemChanceMult != null)
            {
                ModifyLuckArgs modifyLuckArgs = new ModifyLuckArgs() { WeightMult = 1f };
                ItemsCore.GetItemChanceMult(prefab, player, completesSynergy, modifyLuckArgs);
                __result *= modifyLuckArgs.WeightMult;
            }
        }

        [HarmonyPatch(typeof(LootEngine), nameof(LootEngine.SpawnItem))]
        [HarmonyPrefix]
        internal static void PreSpawnItemChange(ref GameObject item)
        {
            GameObject replacer = DoReplacement(item);
            if (replacer)
            {
                item = replacer;
            }
        }
        [HarmonyPatch(typeof(LootEngine), nameof(LootEngine.SpewLoot), new Type[] { typeof(GameObject), typeof(Vector3)})]
        [HarmonyPrefix]
        internal static void PreSpewItemChange(ref GameObject itemToSpawn)
        {
            GameObject replacer = DoReplacement(itemToSpawn);
            if (replacer)
            {
                itemToSpawn = replacer;
            }
        }

        [HarmonyPatch(typeof(ShopItemController), nameof(ShopItemController.InitializeInternal))]
        [HarmonyPrefix]
        internal static void ShopItemReplacer(ref PickupObject i)
        {
            GameObject replacer = DoReplacement(i.gameObject);
            if (replacer) { i = replacer.GetComponent<PickupObject>(); }
        }
        [HarmonyPatch(typeof(Alexandria.NPCAPI.CustomShopItemController), nameof(Alexandria.NPCAPI.CustomShopItemController.InitializeInternal))]
        [HarmonyPrefix]
        internal static void CustomShopItemReplacer(ref PickupObject i)
        {
            GameObject replacer = DoReplacement(i.gameObject);
            if (replacer) { i = replacer.GetComponent<PickupObject>(); }
        }

        public static GameObject DoReplacement(GameObject item)
        {
            bool actuallySkip = false;
            if (item.GetComponent<PickupObject>() != null && ItemsCore.ChangeSpawnedItem.Count != 0)
            {
                PickupObject oldObject = item.GetComponent<PickupObject>();
                foreach (var func in ItemsCore.ChangeSpawnedItem)
                {
                    GameObject newObject = func(oldObject);

                    if (newObject != null)
                    {
                        actuallySkip = true;
                        item = newObject;
                        PickupObject newPickup = item.GetComponent<PickupObject>();
                        if (newPickup)
                        {
                            oldObject = newPickup;
                        }
                        else { break; }
                    }
                }
            } 

            if (actuallySkip) { return item; } return null;
        }

        public class ModifyLuckArgs : EventArgs
        {
            public float WeightMult;
        }
    }
}
