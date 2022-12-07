using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace JuneLib
{
    [HarmonyPatch]
    public static class OverrideItemCanBeUsed
    {

        [HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.IsOnCooldown), MethodType.Getter)]
        [HarmonyPostfix]
        public static void OverrideCanUse(PlayerItem __instance, ref bool __result)
        {
            if (SkipOverrideCanUse) { __result = __result || CustomChargeTypeItem.GetCustomChargeTypeItemIsOnCooldown(__instance); return; }
            ValidOverrideArgs contentsSet = new ValidOverrideArgs();
            contentsSet.ShouldBeOnCooldown = false; //
			contentsSet.overrideItemsUse = new List<Tuple<ValidOverrideArgs.Priority, Action<PlayerController, PlayerItem, OnUseOverrideArgs>>>();
			if (ItemsCore. OnPreUseItem != null) { ItemsCore.OnPreUseItem(__instance.LastOwner, __instance, contentsSet); }

			__instance.gameObject.GetOrAddComponent<ActionContainer>().overrideItemsUse = contentsSet.overrideItemsUse;
            __result = __result || CustomChargeTypeItem.GetCustomChargeTypeItemIsOnCooldown(__instance) || contentsSet.ShouldBeOnCooldown;
        }

        [HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.Use))]
		[HarmonyPrefix]
		public static bool PreUseItem(PlayerItem __instance, PlayerController user)
        {
			ActionContainer sss = __instance.gameObject.GetOrAddComponent<ActionContainer>();
			if (sss.overrideItemsUse.Count > 0)
			{
				for (ValidOverrideArgs.Priority priority = ValidOverrideArgs.Priority.INHERENT_ACTIVE_ITEM_EFFECT; priority < ValidOverrideArgs.Priority.NONE; priority = priority.Next())
				{
					var tuples = sss.overrideItemsUse.Where(hell => hell.First == priority).ToList();
					if (tuples.Count > 0)
                    {
                        OnUseOverrideArgs args = new OnUseOverrideArgs
                        {
                            shouldSkip = true
                        };
                        foreach (var actions in tuples)
                        {
							actions.Second?.Invoke(user, __instance, args);
                        }
						return args.shouldSkip;
                    }
				}
			}
			return true;
		}


        public static bool SkipOverrideCanUse = false;
        private static bool InternalIsOnCooldown(this PlayerItem item)
        {
            SkipOverrideCanUse = true;
            bool isOnCooldown = item.IsOnCooldown;
            SkipOverrideCanUse = false;
            return isOnCooldown;
        }



		public class ValidOverrideArgs : EventArgs
		{
			public enum Priority
			{
				INHERENT_ACTIVE_ITEM_EFFECT,
				PASSIVE_EFFECT_HIGH_PRIORITY,
				PASSIVE_EFFECT_REGULAR_PRIORITY,
				PASSIVE_EFFECT_LOW_PRIORITY,
				NONE
			}

			public bool ShouldBeOnCooldown;
			public List<Tuple<Priority, Action<PlayerController, PlayerItem, OnUseOverrideArgs>>> overrideItemsUse;
		}
		
		public class OnUseOverrideArgs : EventArgs
        {
			public bool shouldSkip;
        }

		private class ActionContainer : MonoBehaviour
		{
			public List<Tuple<ValidOverrideArgs.Priority, Action<PlayerController, PlayerItem, OnUseOverrideArgs>>> overrideItemsUse;
		}

		/*Compat Hell*/
		[HarmonyPatch(typeof(GameUIItemController), nameof(GameUIItemController.UpdateItemSprite))]
        [HarmonyPrefix] 
        public static bool UpdateItemSpriteProper(PlayerItem newItem, int itemShift, GameUIItemController __instance)
		{
			tk2dSprite component = newItem.GetComponent<tk2dSprite>();
			if (newItem != __instance.m_cachedItem)
			{
				__instance.DoItemCardFlip(newItem, itemShift);
			}
			__instance.UpdateItemSpriteScale();
			if (!__instance.m_deferCurrentItemSwap)
			{
				if (!__instance.itemSprite.renderer.enabled)
				{
					__instance.ToggleRenderers(true);
				}
				if (__instance.itemSprite.spriteId != component.spriteId || __instance.itemSprite.Collection != component.Collection)
				{
					__instance.itemSprite.SetSprite(component.Collection, component.spriteId);
					for (int i = 0; i < __instance.outlineSprites.Length; i++)
					{
						__instance.outlineSprites[i].SetSprite(component.Collection, component.spriteId);
						SpriteOutlineManager.ForceUpdateOutlineMaterial(__instance.outlineSprites[i], component);
					}
				}
			}
			Vector3 center = __instance.ItemBoxSprite.GetCenter();
			__instance.itemSprite.transform.position = center + __instance.GetOffsetVectorForItem(newItem, __instance.m_isCurrentlyFlipping);
			__instance.itemSprite.transform.position = __instance.itemSprite.transform.position.Quantize(__instance.ItemBoxSprite.PixelsToUnits() * 3f);
			if (newItem.PreventCooldownBar || (!newItem.IsActive && !newItem.InternalIsOnCooldown()) || __instance.m_isCurrentlyFlipping)
			{
				__instance.ItemBoxFillSprite.IsVisible = false;
				__instance.ItemBoxFGSprite.IsVisible = false;
				__instance.ItemBoxSprite.SpriteName = "weapon_box_02";
			}
			else
			{
				__instance.ItemBoxFillSprite.IsVisible = true;
				__instance.ItemBoxFGSprite.IsVisible = true;
				__instance.ItemBoxSprite.SpriteName = "weapon_box_02_cd";
			}
			if (newItem.IsActive)
			{
				__instance.ItemBoxFillSprite.FillAmount = 1f - newItem.ActivePercentage;
			}
			else
			{
				__instance.ItemBoxFillSprite.FillAmount = 1f - newItem.CooldownPercentage;
			}
			PlayerController user = GameManager.Instance.PrimaryPlayer;
			if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER && __instance.IsRightAligned)
			{
				user = GameManager.Instance.SecondaryPlayer;
			}
			if (newItem.IsOnCooldown || !newItem.CanBeUsed(user))
			{
				Color color = __instance.itemSpriteMaterial.GetColor("_OverrideColor");
				Color color2 = new Color(0f, 0f, 0f, 0.8f);
				if (color != color2)
				{
					__instance.itemSpriteMaterial.SetColor("_OverrideColor", color2);
					tk2dSprite[] array = SpriteOutlineManager.GetOutlineSprites(__instance.itemSprite);
					Color value = new Color(0.4f, 0.4f, 0.4f, 1f);
					for (int j = 0; j < array.Length; j++)
					{
						array[j].renderer.material.SetColor("_OverrideColor", value);
					}
				}
			}
			else
			{
				Color color3 = __instance.itemSpriteMaterial.GetColor("_OverrideColor");
				Color color4 = new Color(0f, 0f, 0f, 0f);
				if (color3 != color4)
				{
					__instance.itemSpriteMaterial.SetColor("_OverrideColor", color4);
					tk2dSprite[] array2 = SpriteOutlineManager.GetOutlineSprites(__instance.itemSprite);
					Color white = Color.white;
					for (int k = 0; k < array2.Length; k++)
					{
						array2[k].renderer.material.SetColor("_OverrideColor", white);
					}
				}
			}
			return true;
        }

		[HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.CooldownPercentage), MethodType.Getter)]
		[HarmonyPrefix]
		public static void Shhh()
        {
			SkipOverrideCanUse = true;
		}
		[HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.CooldownPercentage), MethodType.Getter)]
		[HarmonyPostfix]
		public static void No(ref float __result, PlayerItem __instance)
		{
			if (__instance is CustomChargeTypeItem specialItem)
			{
				__result = specialItem.CooldownPercentageHook();
			}
			SkipOverrideCanUse = false;
		}
	}
}
/**/