using Alexandria.Misc;
using HarmonyLib;
using JuneLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[HarmonyPatch]
public static class JunePlayerEvents
{
    internal static void Init()
    {
        CustomActions.OnNewPlayercontrollerSpawned += player =>
        {
            player.gameObject.AddComponent<JEventsComponent>();
        };
    }

    internal static JEventsComponent GetJEvents(this PlayerController player)
    {
        return player.GetComponent<JEventsComponent>();
	}

	internal static readonly MethodInfo sspe_pfpm = AccessTools.Method(typeof(JunePlayerEvents), nameof(ShootSingleProjectileEvents_PreFireProjectileModifier));
    internal static readonly MethodInfo sspe_ppp = AccessTools.Method(typeof(JunePlayerEvents), nameof(ShootSingleProjectileEvents_PostProcessProjectile));

    [HarmonyPatch(typeof(Gun), nameof(Gun.ShootSingleProjectile))]
    [HarmonyILManipulator]
	internal static void ShootSingleProjectileEvents_Transpiler(ILContext ctx)
	{
		var crs = new ILCursor(ctx);

        // OnPreFireContextProjectileModifer:
        // Go before the game gets Gun.m_isCritting
        if (!crs.TryGotoNext(MoveType.Before, x => x.MatchLdfld<Gun>(nameof(Gun.m_isCritting))))
			return;

		// Emit an instruction to load the projectile module (argument 1)
        crs.Emit(OpCodes.Ldarg_1);
		// Emit an instruction to load a *reference* to the projectile prefab (local variable 2)
		crs.Emit(OpCodes.Ldloca, 2);
        // Emit an instruction to call the ShootSingleProjectileEvents_PreFireProjectileModifier helper method, which in turn calls OnPreFireContextProjectileModifer
        crs.Emit(OpCodes.Call, sspe_pfpm);

        // PostProcessProjectileMod:
        // Go after PlayerController.DoPostProcessProjectile is called
        if (!crs.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<PlayerController>(nameof(PlayerController.DoPostProcessProjectile))))
			return;

		// Emit an instruction to load the player (local variable 0)
		crs.Emit(OpCodes.Ldloc_0);
		// Emit an instruction to load the projectile (local variable 10)
		crs.Emit(OpCodes.Ldloc, 10);
		// Emit an instruction to load the gun (argument 0/instance)
        crs.Emit(OpCodes.Ldarg_0);
		// Emit an instruction to load the projectile module (argument 1)
        crs.Emit(OpCodes.Ldarg_1);
        // Emit an instruction to call the ShootSingleProjectileEvents_PostProcessProjectile helper method, which in turn calls PostProcessProjectileMod
        crs.Emit(OpCodes.Call, sspe_ppp);
    }

	internal static Gun ShootSingleProjectileEvents_PreFireProjectileModifier(Gun curr, ProjectileModule mod, ref Projectile proj)
    {
		if(curr == null || curr.CurrentOwner == null || !(curr.CurrentOwner is PlayerController player))
			return curr;

        var jevents = player.GetJEvents();

        if (!jevents || jevents.OnPreFireContextProjectileModifer == null)
            return curr;

        proj = jevents.OnPreFireContextProjectileModifer(curr, proj, mod);
        return curr;
    }

	internal static void ShootSingleProjectileEvents_PostProcessProjectile(PlayerController player, Projectile proj, Gun gun, ProjectileModule module)
    {
        var jevents = player.GetJEvents();

        if (!jevents || jevents.PostProcessProjectileMod == null)
            return;

        jevents.PostProcessProjectileMod(proj, gun, module);
    }
}

public class JEventsComponent : BraveBehaviour
{
    public Action<PlayerController, Gun, ProjectileVolleyData, RegeneratingVolleyModifiers.ModifyProjArgs> ConstantModifyGunVolley;

    public Func<Gun, Projectile, ProjectileModule, Projectile> OnPreFireContextProjectileModifer;

	public Action<Projectile, Gun, ProjectileModule> PostProcessProjectileMod;
}