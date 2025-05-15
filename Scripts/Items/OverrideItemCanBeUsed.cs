using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
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
			if (__instance is CustomChargeTypeItem)
            {
				__result = __result || CustomChargeTypeItem.GetCustomChargeTypeItemIsOnCooldown(__instance);
				return;
			}
			if (SkipOverrideCanUse) { return; }
            ValidOverrideArgs contentsSet = new ValidOverrideArgs();
            contentsSet.ShouldBeUseable = false; //
			contentsSet.overrideItemsUse = new List<Tuple<ValidOverrideArgs.Priority, Action<PlayerController, PlayerItem, OnUseOverrideArgs>>>();
			if (ItemsCore.OnPreUseItem != null) { ItemsCore.OnPreUseItem(__instance.LastOwner, __instance, contentsSet); }

			ActionContainer comp = __instance.gameObject.GetOrAddComponent<ActionContainer>();
			comp.overrideItemsUse = contentsSet.overrideItemsUse;
			if (contentsSet.ShouldBeUseable || comp.GuaranteeCheck)
            {
				__result = false;
            }
			if (!SkipLogging) { ETGModConsole.Log(__result);
				ETGModConsole.Log(comp.GuaranteeCheck);
			}
        }

        [HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.Use))]
		[HarmonyPrefix]
		public static bool PreUseItem(PlayerItem __instance, PlayerController user)
        {
			if (!InternalIsOnCooldown(__instance))
            {
				return true;
            }
            ActionContainer sss = __instance.gameObject.GetOrAddComponent<ActionContainer>();
			if (sss.overrideItemsUse != null && sss.overrideItemsUse.Count > 0)
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
						sss.GuaranteeCheck = true;
						return args.shouldSkip;
                    }
				}
			}
			return true;
		}
		[HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.Use))]
		[HarmonyPostfix]
		public static void PostUseItem(PlayerItem __instance)
        {
			ActionContainer sss = __instance.gameObject.GetOrAddComponent<ActionContainer>();
			sss.GuaranteeCheck = false;
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

			public bool ShouldBeUseable;
			internal List<Tuple<Priority, Action<PlayerController, PlayerItem, OnUseOverrideArgs>>> overrideItemsUse;

			public void AddActionWithPriority(Priority priority, Action<PlayerController, PlayerItem, OnUseOverrideArgs> action)
            {
				overrideItemsUse.Add(new Tuple<Priority,
					Action<PlayerController, PlayerItem, OnUseOverrideArgs>>(priority, action));
			}
		}
		
		public class OnUseOverrideArgs : EventArgs
        {
			public bool shouldSkip;
        }

		private class ActionContainer : MonoBehaviour
		{
			public List<Tuple<ValidOverrideArgs.Priority, Action<PlayerController, PlayerItem, OnUseOverrideArgs>>> overrideItemsUse;
			public bool GuaranteeCheck;
		}

		public static bool SkipLogging = true;

		internal static readonly MethodInfo uisp_so = AccessTools.Method(typeof(OverrideItemCanBeUsed), nameof(UpdateItemSpriteProper_SkipOverride));
        internal static readonly MethodInfo uisp_uo = AccessTools.Method(typeof(OverrideItemCanBeUsed), nameof(UpdateItemSpriteProper_UnskipOverride));

        [HarmonyPatch(typeof(GameUIItemController), nameof(GameUIItemController.UpdateItemSprite))]
        [HarmonyILManipulator]
        internal static void UpdateItemSpriteProper_Transpiler(ILContext ctx)
		{
			var crs = new ILCursor(ctx);

			if (!crs.TryGotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<PlayerItem>($"get_{nameof(PlayerItem.IsOnCooldown)}")))
				return;

			crs.Emit(OpCodes.Call, uisp_so);

            if (!crs.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<PlayerItem>($"get_{nameof(PlayerItem.IsOnCooldown)}")))
                return;

            crs.Emit(OpCodes.Call, uisp_uo);
        }

		internal static void UpdateItemSpriteProper_SkipOverride()
		{
			SkipOverrideCanUse = true;
        }

        internal static void UpdateItemSpriteProper_UnskipOverride()
        {
            SkipOverrideCanUse = false;
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