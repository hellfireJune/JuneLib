using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using JuneLib;

[HarmonyPatch]
public class CustomChargeTypeItem : PlayerItem
{
	internal static bool GetCustomChargeTypeItemIsOnCooldown(PlayerItem item)
	{
		if (item is CustomChargeTypeItem ccitem)
		{
			return ccitem.RemainingSpecialCooldown > 0;
		}
		return false;
	}
	internal float CooldownPercentageHook()
    {
		if (this.IsCurrentlyActive)
		{
			return this.ActivePercentage;
		}
		if (!this.IsOnCooldown)
		{
			return 0f;
		}
		return (float)this.remainingSpecialCooldown / (float)this.specialCooldown;
	}

	[HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.ApplyCooldown))]
	[HarmonyPostfix]
	public static void ApplyCooldownHook(PlayerItem __instance)
	{
		if (__instance is CustomChargeTypeItem specialItem)
		{
			specialItem.remainingSpecialCooldown = specialItem.specialCooldown;
		}
	}


	public float RemainingSpecialCooldown
    {
		get { return this.remainingSpecialCooldown; }
		set { this.remainingSpecialCooldown = Math.Max(value, 0);}
    }
	public float specialCooldown;
	protected float remainingSpecialCooldown;
}