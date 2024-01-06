using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JuneLib
{
    [HarmonyPatch]
    public static class GunHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Gun), nameof(Gun.ReinitializeModuleData))]
        public static void AddMoreModuleData(Gun __instance)
        {
            var holder = __instance.GetComponent<GunClipModifierHolder>();
            if (!holder) { return; }
            foreach (var modifiers in holder.modifiers.Values)
            {
                foreach (var mod in modifiers.CurrentModifiers)
                {
                    if (mod.ModifierPositions != null)
                    {
                        foreach (var position in mod.ModifierPositions)
                        {
                            foreach (var projectiles in position.RuntimeVolley.projectiles)
                            {
                                ModuleShootData moduleShootData = new ModuleShootData();
                                moduleShootData.numberShotsFired = 0;
                                __instance.m_moduleData.Add(projectiles, moduleShootData);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Gun), nameof(Gun.Attack))]
        public static bool OverrideGunAttack(Gun __instance, ref Gun.AttackResult __result)
        {
            var holder = __instance.GetComponent<GunClipModifierHolder>();
            if (holder == null)
            {
                return true;
            }

            if (!holder.ShouldAddModifiers())
            {
                return true;
            }

            if (__instance.IsReloading || (__instance.m_isCurrentlyFiring && __instance.m_midBurstFire) || (!__instance.InfiniteAmmo && __instance.ammo <= 0))
            {
                return true;
            }

            GunClipModifiers.InsertData overrideVolley = holder.GetThing(out ProjectileModule mod);
            if (overrideVolley == null)
            {
                return true;
            }

            ProjectileVolleyData cooldownVolley = __instance.Volley;
            if (__instance.modifiedFinalVolley != null && __instance.DefaultModule.HasFinalVolleyOverride() && __instance.DefaultModule.IsFinalShot(__instance.m_moduleData[__instance.DefaultModule], __instance.CurrentOwner))
            {
                cooldownVolley = __instance.modifiedFinalVolley;
            }

            bool flag = __instance.InitialFireFakeVolley(overrideVolley.RuntimeVolley, cooldownVolley, mod.IsDuctTapeModule);
            __instance.m_midBurstFire = false;

            for (int i = 0; i < __instance.Volley.projectiles.Count; i++)
            {
                ProjectileModule projectileModule = __instance.Volley.projectiles[i];
                if (projectileModule.shootStyle == ProjectileModule.ShootStyle.Burst && __instance.m_moduleData[projectileModule].numberShotsFiredThisBurst < projectileModule.burstShotCount)
                {
                    __instance.m_midBurstFire = true;
                    break;
                }
            }

            __instance.m_isCurrentlyFiring = flag;
            if (__instance.m_isCurrentlyFiring && __instance.lowersAudioWhileFiring)
            {
                AkSoundEngine.PostEvent("play_state_volume_lower_01", GameManager.Instance.gameObject);
            }
            if (flag && __instance.OnPostFired != null && __instance.m_owner is PlayerController)
            {
                __instance.OnPostFired(__instance.m_owner as PlayerController, __instance);
            }
            __result = (!flag) ? Gun.AttackResult.OnCooldown : Gun.AttackResult.Success;
            return false;
        }

        internal static bool InitialFireFakeVolley(this Gun gun, ProjectileVolleyData volley, ProjectileVolleyData cooldownReference, bool ducttape)
        {
            bool shouldEvenFire = false;
            foreach (var proj in cooldownReference.projectiles)
            {
                if (!gun.m_moduleData[proj].onCooldown && !proj.IsDuctTapeModule == ducttape)
                {
                    shouldEvenFire = true;
                    CustomModuleCooldown(gun, proj, cooldownReference.projectiles[0]);
                }
            }

            bool playEffects = true;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = true;
            bool flag4 = false;
            for (int i = 0; i < volley.projectiles.Count; i++)
            {
                ProjectileModule projectileModule = volley.projectiles[i];
                if (!gun.m_moduleData[projectileModule].needsReload)
                {
                    flag = true;
                    if (shouldEvenFire)
                    {
                        if (!gun.UsesRechargeLikeActiveItem || gun.m_remainingActiveCooldownAmount <= 0f)
                        {
                            if (volley.ModulesAreTiers)
                            {
                                if (projectileModule.IsDuctTapeModule)
                                {
                                    flag3 = true;
                                }
                                else
                                {
                                    int num = (projectileModule.CloneSourceIndex < 0) ? i : projectileModule.CloneSourceIndex;
                                    if (num == gun.m_currentStrengthTier)
                                    {
                                        playEffects = !flag4;
                                        flag3 = true;
                                        flag4 = true;
                                    }
                                    else
                                    {
                                        playEffects = false;
                                        flag3 = false;
                                    }
                                }
                            }
                            if (flag3)
                            {
                                flag2 |= gun.HandleSpecificInitialGunShoot(projectileModule, null, null, playEffects);
                            }
                            playEffects = false;
                        }
                    }
                }
            }
            if (!flag)
            {
                Debug.LogError("June flavoured error message: Help! Gun shoot unloaded thing? So icky...");
            }
            return flag2;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Gun), nameof(Gun.ContinueAttack))]
        public static bool OverrideContinueGunShoot(Gun __instance, ref bool __result, bool canAttack, ProjectileData overrideProjectileData)
        {
            var holder = __instance.GetComponent<GunClipModifierHolder>();
            if (holder == null)
            {
                return true;
            }

            if (!holder.ShouldAddModifiers())
            {
                return true;
            }

            if ((!__instance.InfiniteAmmo && __instance.ammo <= 0))
            {
                return true;
            }
            if (!__instance.m_isCurrentlyFiring || __instance.IsEmpty)
            {
                return true;
            }
            GunClipModifiers.InsertData overrideVolley = holder.GetThing(out ProjectileModule mod);
            if (overrideVolley == null)
            {
                return false;
            }
            ProjectileVolleyData newVolley = overrideVolley.RuntimeVolley;

            bool giveUp = true;
            foreach (ProjectileModule module in newVolley.projectiles)
            {
                if (module.shootStyle == ProjectileModule.ShootStyle.Automatic || module.shootStyle == ProjectileModule.ShootStyle.Charged ||
                    module.shootStyle == ProjectileModule.ShootStyle.Charged)
                {
                    giveUp = false;
                }
            }
            if (giveUp) { return true; }


            if (!__instance.m_playedEmptyClipSound && __instance.ClipShotsRemaining == 0)
            {
                if (GameManager.AUDIO_ENABLED)
                {
                    AkSoundEngine.PostEvent("Play_WPN_gun_empty_01", __instance.gameObject);
                }
                __instance.m_playedEmptyClipSound = true;
            }
            __instance.m_cachedIsGunBlocked = __instance.IsGunBlocked();
            __instance.m_isCurrentlyFiring = true;
            __instance.m_continuousAttackTime += BraveTime.DeltaTime;
            bool flag = false;
            if (!canAttack || __instance.m_cachedIsGunBlocked)
            {
                if (__instance.m_activeBeams.Count > 0)
                {
                    __instance.ClearBeams();
                }
                else if (__instance.isAudioLoop && __instance.m_isAudioLooping)
                {
                    if (GameManager.AUDIO_ENABLED)
                    {
                        AkSoundEngine.PostEvent("Stop_WPN_gun_loop_01", __instance.gameObject);
                    }
                    __instance.m_isAudioLooping = false;
                }
                __instance.ClearBurstState();
                if (__instance.usesContinuousMuzzleFlash)
                {
                    __instance.muzzleFlashEffects.DestroyAll();
                    __instance.m_isContinuousMuzzleFlashOut = false;
                }
                __instance.m_continuousAttackTime = 0f;
            }
            if (__instance.m_activeBeams.Count > 0 && __instance.m_owner is PlayerController)
            {
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.BEAM_WEAPON_FIRE_TIME, BraveTime.DeltaTime);
            }
            if (__instance.CanCriticalFire)
            {
                float num = (float)PlayerStats.GetTotalCoolness() / 100f;
                if (__instance.m_owner.IsStealthed)
                {
                    num = 10f;
                }
                if (UnityEngine.Random.value < __instance.CriticalChance + num)
                {
                    __instance.m_isCritting = true;
                }
                if (__instance.ForceNextShotCritical)
                {
                    __instance.ForceNextShotCritical = false;
                    __instance.m_isCritting = true;
                }
            }
            if (__instance.Volley != null)
            {
                if (__instance.CheckHasLoadedModule(__instance.Volley))
                {
                    ProjectileVolleyData volley = newVolley;
                    flag = __instance.HandleContinueGunShoot(volley, canAttack, overrideProjectileData);
                    __instance.m_midBurstFire = false;
                    for (int i = 0; i < __instance.Volley.projectiles.Count; i++)
                    {
                        ProjectileModule projectileModule = __instance.Volley.projectiles[i];
                        if (projectileModule.shootStyle == ProjectileModule.ShootStyle.Burst && __instance.m_moduleData[projectileModule].numberShotsFiredThisBurst < projectileModule.burstShotCount)
                        {
                            __instance.m_midBurstFire = true;
                            break;
                        }
                    }
                }
                else
                {
                    __instance.CeaseAttack(false, null);
                }
            }
            else
            {
                __instance.CeaseAttack(false, null);
                __instance.m_midBurstFire = false;
                if (__instance.singleModule.shootStyle == ProjectileModule.ShootStyle.Burst && __instance.m_moduleData[__instance.singleModule].numberShotsFiredThisBurst < __instance.singleModule.burstShotCount)
                {
                    __instance.m_midBurstFire = true;
                }
            }
            if (flag && __instance.OnPostFired != null && __instance.m_owner is PlayerController)
            {
                __instance.OnPostFired(__instance.m_owner as PlayerController, __instance);
            }
            __result = flag;

            return false;


        }

        public static IEnumerator CustomModuleCooldown(Gun gun, ProjectileModule mod, ProjectileModule fakedule)
        {
            gun.m_moduleData[mod].onCooldown = true;
            float elapsed = 0f;
            float fireMultiplier = (!(gun.m_owner is PlayerController)) ? 1f : (gun.m_owner as PlayerController).stats.GetStatValue(PlayerStats.StatType.RateOfFire);
            if (gun.GainsRateOfFireAsContinueAttack)
            {
                float num = gun.RateOfFireMultiplierAdditionPerSecond * gun.m_continuousAttackTime;
                fireMultiplier += num;
            }
            float cooldownTime;
            if (mod.shootStyle == ProjectileModule.ShootStyle.Burst && gun.m_moduleData[mod].numberShotsFiredThisBurst < mod.burstShotCount)
            {
                cooldownTime = fakedule.burstCooldownTime > 0 ? fakedule.burstCooldownTime : mod.burstCooldownTime;
            }
            else
            {
                cooldownTime = fakedule.cooldownTime + gun.gunCooldownModifier;
            }
            cooldownTime *= 1f / fireMultiplier;
            while (elapsed < cooldownTime)
            {
                elapsed += BraveTime.DeltaTime;
                yield return null;
            }
            if (gun.m_moduleData != null && gun.m_moduleData.ContainsKey(mod))
            {
                gun.m_moduleData[mod].onCooldown = false;
                gun.m_moduleData[mod].chargeTime = 0f;
                gun.m_moduleData[mod].chargeFired = false;
            }
            yield break;

        }

        public static GunClipModifiers.InsertData GetThing(this GunClipModifierHolder holder, out ProjectileModule mod)
        {
            GunClipModifiers.InsertData overrideVolley = null;
            mod = null;
            foreach (var activeMod in holder.modifiers)
            {
                int type = holder.GetNextTypeToFire(activeMod.Key);

                if (type >= 0)
                {
                    var currentMod = activeMod.Value.CurrentModifiers[type];
                    var pos = holder.GetPos(activeMod.Key);

                    overrideVolley = currentMod.ModifierPositions.FirstOrDefault(help => help.Position == pos && !help.Fired);
                    mod = activeMod.Key;
                    break;
                }

            }
            return overrideVolley;
        }
    }
}
