using Alexandria.Misc;
using HarmonyLib;
using JuneLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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

	[HarmonyPatch(typeof(Gun), nameof(Gun.ShootSingleProjectile))]
    [HarmonyPrefix]
    internal static bool OverrideShootSingleProjectile(Gun __instance, ProjectileModule mod, ProjectileData overrideProjectileData = null, GameObject overrideBulletObject = null)
	{
		PlayerController playerController = __instance.m_owner as PlayerController;
		AIActor aiactor = __instance.m_owner as AIActor;
		Projectile projectile = null;
		ProjectileModule.ChargeProjectile chargeProjectile = null;
		if (overrideBulletObject)
		{
			projectile = overrideBulletObject.GetComponent<Projectile>();
		}
		else if (mod.shootStyle == ProjectileModule.ShootStyle.Charged)
		{
			chargeProjectile = mod.GetChargeProjectile(__instance.m_moduleData[mod].chargeTime);
			if (chargeProjectile != null)
			{
				projectile = chargeProjectile.Projectile;
				projectile.pierceMinorBreakables = true;
			}
		}
		else
		{
			projectile = mod.GetCurrentProjectile(__instance.m_moduleData[mod], __instance.CurrentOwner);
		}
		if (!projectile)
		{
			__instance.m_moduleData[mod].numberShotsFired++;
			__instance.m_moduleData[mod].numberShotsFiredThisBurst++;
			if (__instance.m_moduleData[mod].numberShotsActiveReload > 0)
			{
				__instance.m_moduleData[mod].numberShotsActiveReload--;
			}
			if (mod.GetModNumberOfShotsInClip(__instance.CurrentOwner) > 0 && __instance.m_moduleData[mod].numberShotsFired >= mod.GetModNumberOfShotsInClip(__instance.CurrentOwner))
			{
				__instance.m_moduleData[mod].needsReload = true;
			}
			if (mod.shootStyle != ProjectileModule.ShootStyle.Charged)
			{
				mod.IncrementShootCount();
			}
			return false; 
		}
		if (playerController)
		{
			if (playerController.OnPreFireProjectileModifier != null)
            {
				projectile = playerController.OnPreFireProjectileModifier(__instance, projectile);
			}
			JEventsComponent jevents = playerController.GetJEvents();
			if (jevents && jevents.OnPreFireContextProjectileModifer != null)
			{
				projectile = jevents.OnPreFireContextProjectileModifer(__instance, projectile, mod);
			}
		}
		if (__instance.m_isCritting && __instance.CriticalReplacementProjectile)
		{
			projectile = __instance.CriticalReplacementProjectile;
		}
		if (__instance.OnPreFireProjectileModifier != null)
		{
			projectile = __instance.OnPreFireProjectileModifier(__instance, projectile, mod);
		}
		if (GameManager.Instance.InTutorial && playerController != null)
		{
			GameManager.BroadcastRoomTalkDoerFsmEvent("playerFiredGun");
		}
		Vector3 a = __instance.barrelOffset.position;
		a = new Vector3(a.x, a.y, -1f);
		float num = (!(playerController != null)) ? 1f : playerController.stats.GetStatValue(PlayerStats.StatType.Accuracy);
		num = ((!(__instance.m_owner is DumbGunShooter) || !(__instance.m_owner as DumbGunShooter).overridesInaccuracy) ? num : (__instance.m_owner as DumbGunShooter).inaccuracyFraction);
		float angleForShot = mod.GetAngleForShot(__instance.m_moduleData[mod].alternateAngleSign, num, null);
		if (__instance.m_moduleData[mod].numberShotsActiveReload > 0 && __instance.activeReloadData.usesOverrideAngleVariance)
		{
			ProjectileModule projectileModule = mod;
			float varianceMultiplier = num;
			angleForShot = projectileModule.GetAngleForShot(1f, varianceMultiplier, new float?(__instance.activeReloadData.overrideAngleVariance));
		}
		if (mod.alternateAngle)
		{
			__instance.m_moduleData[mod].alternateAngleSign *= -1f;
		}
		if (__instance.LockedHorizontalOnCharge && __instance.LockedHorizontalCenterFireOffset >= 0f)
		{
			a = __instance.m_owner.specRigidbody.HitboxPixelCollider.UnitCenter + BraveMathCollege.DegreesToVector(__instance.gunAngle, __instance.LockedHorizontalCenterFireOffset);
		}
		GameObject gameObject = SpawnManager.SpawnProjectile(projectile.gameObject, a + Quaternion.Euler(0f, 0f, __instance.gunAngle) * mod.positionOffset, Quaternion.Euler(0f, 0f, __instance.gunAngle + angleForShot), true);
		Projectile component = gameObject.GetComponent<Projectile>();
		__instance.LastProjectile = component;
		component.Owner = __instance.m_owner;
		component.Shooter = __instance.m_owner.specRigidbody;
		component.baseData.damage += (float)__instance.damageModifier;
		component.Inverted = mod.inverted;
		if (__instance.m_owner is PlayerController && (__instance.LocalActiveReload || (playerController.IsPrimaryPlayer && Gun.ActiveReloadActivated) || (!playerController.IsPrimaryPlayer && Gun.ActiveReloadActivatedPlayerTwo)))
		{
			component.baseData.damage *= __instance.m_moduleData[mod].activeReloadDamageModifier;
		}
		if (__instance.m_owner.aiShooter)
		{
			component.collidesWithEnemies = __instance.m_owner.aiShooter.CanShootOtherEnemies;
		}
		if (__instance.rampBullets)
		{
			component.Ramp(__instance.rampStartHeight, __instance.rampTime);
			TrailController componentInChildren = gameObject.GetComponentInChildren<TrailController>();
			if (componentInChildren)
			{
				componentInChildren.rampHeight = true;
				componentInChildren.rampStartHeight = __instance.rampStartHeight;
				componentInChildren.rampTime = __instance.rampTime;
			}
		}
		if (__instance.m_owner is PlayerController)
		{
			PlayerStats stats = playerController.stats;
			component.baseData.damage *= stats.GetStatValue(PlayerStats.StatType.Damage);
			component.baseData.speed *= stats.GetStatValue(PlayerStats.StatType.ProjectileSpeed);
			component.baseData.force *= stats.GetStatValue(PlayerStats.StatType.KnockbackMultiplier);
			component.baseData.range *= stats.GetStatValue(PlayerStats.StatType.RangeMultiplier);
			if (playerController.inventory.DualWielding)
			{
				component.baseData.damage *= Gun.s_DualWieldFactor;
			}
			if (__instance.CanSneakAttack && playerController.IsStealthed)
			{
				component.baseData.damage *= __instance.SneakAttackDamageMultiplier;
			}
			if (__instance.m_isCritting)
			{
				component.baseData.damage *= __instance.CriticalDamageMultiplier;
				component.IsCritical = true;
			}
			if (__instance.UsesBossDamageModifier)
			{
				if (__instance.CustomBossDamageModifier >= 0f)
				{
					component.BossDamageMultiplier = __instance.CustomBossDamageModifier;
				}
				else
				{
					component.BossDamageMultiplier = 0.8f;
				}
			}
		}
		if (__instance.Volley != null && __instance.Volley.UsesShotgunStyleVelocityRandomizer)
		{
			component.baseData.speed *= __instance.Volley.GetVolleySpeedMod();
		}
		if (aiactor != null && aiactor.IsBlackPhantom)
		{
			component.baseData.speed *= aiactor.BlackPhantomProperties.BulletSpeedMultiplier;
		}
		if (__instance.m_moduleData[mod].numberShotsActiveReload > 0)
		{
			if (!__instance.activeReloadData.ActiveReloadStacks)
			{
				component.baseData.damage *= __instance.activeReloadData.damageMultiply;
			}
			component.baseData.force *= __instance.activeReloadData.knockbackMultiply;
		}
		if (overrideProjectileData != null)
		{
			component.baseData.SetAll(overrideProjectileData);
		}
		__instance.LastShotIndex = __instance.m_moduleData[mod].numberShotsFired;
		component.PlayerProjectileSourceGameTimeslice = Time.time;
		if (!__instance.IsMinusOneGun)
		{
			__instance.ApplyCustomAmmunitionsToProjectile(component);
			if (__instance.m_owner is PlayerController)
			{
				playerController.DoPostProcessProjectile(component);
				JEventsComponent jevents = playerController.GetJEvents();
				if (jevents && jevents.PostProcessProjectileMod != null)
				{
					jevents.PostProcessProjectileMod(component, __instance, mod);

				}
			}
			if (__instance.PostProcessProjectile != null)
			{
				__instance.PostProcessProjectile(component);
			}
		}
		if (mod.mirror)
		{
			gameObject = SpawnManager.SpawnProjectile(projectile.gameObject, a + Quaternion.Euler(0f, 0f, __instance.gunAngle) * mod.InversePositionOffset, Quaternion.Euler(0f, 0f, __instance.gunAngle - angleForShot), true);
			Projectile component2 = gameObject.GetComponent<Projectile>();
			__instance.LastProjectile = component2;
			component2.Inverted = true;
			component2.Owner = __instance.m_owner;
			component2.Shooter = __instance.m_owner.specRigidbody;
			if (__instance.m_owner.aiShooter)
			{
				component2.collidesWithEnemies = __instance.m_owner.aiShooter.CanShootOtherEnemies;
			}
			if (__instance.rampBullets)
			{
				component2.Ramp(__instance.rampStartHeight, __instance.rampTime);
				TrailController componentInChildren2 = gameObject.GetComponentInChildren<TrailController>();
				if (componentInChildren2)
				{
					componentInChildren2.rampHeight = true;
					componentInChildren2.rampStartHeight = __instance.rampStartHeight;
					componentInChildren2.rampTime = __instance.rampTime;
				}
			}
			component2.PlayerProjectileSourceGameTimeslice = Time.time;
			if (!__instance.IsMinusOneGun)
			{
				__instance.ApplyCustomAmmunitionsToProjectile(component2);
				if (__instance.m_owner is PlayerController)
				{
					playerController.DoPostProcessProjectile(component2);
				}
				if (__instance.PostProcessProjectile != null)
				{
					__instance.PostProcessProjectile(component2);
				}
			}
			component2.baseData.SetAll(component.baseData);
			component2.IsCritical = component.IsCritical;
		}
		if (__instance.modifiedFinalVolley != null && mod == __instance.modifiedFinalVolley.projectiles[0])
		{
			mod = __instance.DefaultModule;
		}
		if (chargeProjectile != null && chargeProjectile.ReflectsIncomingBullets && __instance.barrelOffset)
		{
			if (chargeProjectile.MegaReflection)
			{
				int num2 = PassiveReflectItem.ReflectBulletsInRange(__instance.barrelOffset.position.XY(), 2.66f, true, __instance.m_owner, 30f, 1.25f, 1.5f, true);
				if (num2 > 0)
				{
					AkSoundEngine.PostEvent("Play_WPN_duelingpistol_impact_01", __instance.gameObject);
					AkSoundEngine.PostEvent("Play_PET_junk_punch_01", __instance.gameObject);
				}
			}
			else
			{
				int num3 = PassiveReflectItem.ReflectBulletsInRange(__instance.barrelOffset.position.XY(), 2.66f, true, __instance.m_owner, 30f, 1f, 1f, true);
				if (num3 > 0)
				{
					AkSoundEngine.PostEvent("Play_WPN_duelingpistol_impact_01", __instance.gameObject);
					AkSoundEngine.PostEvent("Play_PET_junk_punch_01", __instance.gameObject);
				}
			}
		}
		__instance.IncrementModuleFireCountAndMarkReload(mod, chargeProjectile);
		if (__instance.m_owner is PlayerController)
		{
			GameStatsManager.Instance.RegisterStatChange(TrackedStats.BULLETS_FIRED, 1f);
			if (projectile != null && projectile.AppliesKnockbackToPlayer)
			{
				playerController.knockbackDoer.ApplyKnockback(-1f * BraveMathCollege.DegreesToVector(__instance.gunAngle, 1f), projectile.PlayerKnockbackForce, false);
			}
		}
		return false;
    }
}

public class JEventsComponent : BraveBehaviour
{
    public Action<PlayerController, Gun, ProjectileVolleyData, RegeneratingVolleyModifiers.ModifyProjArgs> ConstantModifyGunVolley;

    public Func<Gun, Projectile, ProjectileModule, Projectile> OnPreFireContextProjectileModifer;

	public Action<Projectile, Gun, ProjectileModule> PostProcessProjectileMod;
}