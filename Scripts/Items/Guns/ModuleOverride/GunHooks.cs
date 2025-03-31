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
        public static int ModifierlessGetModNumberClipShot(this ProjectileModule module, GameActor owner)
        {
            SkipClipNum = true;
            int modNumber = module.GetModNumberOfShotsInClip(owner);
            SkipClipNum = false;
            return modNumber;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProjectileModule), nameof(ProjectileModule.GetModNumberOfShotsInClip))]
        public static void GetModClipNum(ProjectileModule __instance, ref int __result, GameActor owner)
        {
            if (SkipClipNum || !(owner is PlayerController player))
            {
                return;
            }
            //Debug.Log("owner is player");
            var holder = owner.GetComponent<PlayerClipModifierHolder>();
            if (!holder) { return; } //Debug.Log("owner has the component");
            var projmodGuid = __instance.runtimeGuid;
            var gun = player.inventory.AllGuns.Where(g => g != null && ((g.Volley && g.Volley.projectiles.Contains(__instance)) || g.DefaultModule == __instance)).FirstOrDefault();
            if (!gun) { return; } //Debug.Log("gun exists");
            var gunHolder = gun.GetComponent<GunClipModifierHolder>();
            if (gunHolder && gunHolder.modifiers != null)
            {
                //Debug.Log("doing the final thing");
                var modifier = gunHolder.GetModifier(__instance.runtimeGuid);
                if (modifier != null)
                {
                    __result += modifier.CurrentBonusClipSize;
                }
                //Debug.Log(modifier == null);
            }
        }

        public static bool SkipClipNum = false;


        [HarmonyPostfix]
        [HarmonyPatch(typeof(Gun), nameof(Gun.ReinitializeModuleData))]
        public static void AddMoreModuleData(Gun __instance)
        {
            var holder = __instance.GetComponent<GunClipModifierHolder>();
            if (!holder || holder.modifiers == null) { return; }
            foreach (var modifiers in holder.modifiers.Values)
            {
                foreach (var mod in modifiers.RuntimePositionContainers)
                {
                    if (mod.InsertedDatas != null)
                    {
                        foreach (var position in mod.InsertedDatas)
                        {
                            foreach (var projectiles in position.RuntimeVolley.projectiles)
                            {
                                if (__instance.m_moduleData.ContainsKey(projectiles)) { continue; }
                                ModuleShootData moduleShootData = new ModuleShootData();
                                moduleShootData.numberShotsFired = 0;
                                __instance.m_moduleData.Add(projectiles, moduleShootData);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Gun), nameof(Gun.FinishReload))]
        public static void OnFinishReload(Gun __instance)
        {
            var holder = __instance.GetComponent<GunClipModifierHolder>();
            if (!holder) { return; }

            if (holder.ShouldAddModifiers())
            {
                holder.ReloadRebuild(false);

                //reset num fired coz i have to do that manually lol
                foreach (var mod in holder.modifiers.Values)
                {
                    var hostProjMod = mod.hostProjectile;
                    int numberShotsFired = Math.Max(hostProjMod.GetModNumberOfShotsInClip(__instance.CurrentOwner) - __instance.ammo, 0);
                    foreach (var containers in mod.RuntimePositionContainers)
                    {
                        foreach (var insertData in containers.InsertedDatas)
                        {
                            foreach (var projMod in insertData.RuntimeVolley.projectiles)
                            {
                                if (__instance.m_moduleData.ContainsKey(projMod))
                                {
                                    __instance.m_moduleData[projMod].numberShotsFired = numberShotsFired;
                                    __instance.m_moduleData[projMod].needsReload = false;
                                }
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
            Debug.Log("calling main gun attack method");
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

            ModuleInsertData data = holder.GetNextTypeToFire(out ProjectileModule mod);
            Debug.Log(data == null ? "null" : data.DataType.ToString());
            if (data == null || data.DataType != ModuleInsertData.InsertDataType.NEW_STUFF)
            {
                return true;
            }
            Debug.Log("one");

            ProjectileVolleyData cooldownVolley = __instance.Volley;
            if (__instance.Volley == null)
            {
                return true;
            }
            if (__instance.modifiedFinalVolley != null && __instance.DefaultModule.HasFinalVolleyOverride() && __instance.DefaultModule.IsFinalShot(__instance.m_moduleData[__instance.DefaultModule], __instance.CurrentOwner))
            {
                cooldownVolley = __instance.modifiedFinalVolley;
            }
            Debug.Log("two");

            bool flag = __instance.InitialFireFakeVolley(data.RuntimeVolley, cooldownVolley, mod.IsDuctTapeModule);
            __instance.m_midBurstFire = false;
            Debug.Log("three");

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
            Debug.Log("cooldown stuff");
            Debug.Log(cooldownReference.projectiles.Count);
            foreach (var proj in cooldownReference.projectiles)
            {
                if (!gun.m_moduleData[proj].onCooldown && proj.IsDuctTapeModule == ducttape)
                {
                    shouldEvenFire = true;
                    Debug.Log("is this even working? but genuinely?");
                    gun.StartCoroutine(CustomModuleCooldown(gun, proj, cooldownReference.projectiles[0]));
                }
            }

            bool playEffects = true;
            bool flag2 = false;
            bool flag3 = true;
            bool flag4 = false;
            for (int i = 0; i < volley.projectiles.Count; i++)
            {
                ProjectileModule projectileModule = volley.projectiles[i];
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
            return flag2;
        }

        private static bool ContinueFireFakeVolley(this Gun gun, ProjectileVolleyData Volley, ProjectileVolleyData cooldownReference, bool ducttape, bool canAttack = true)
        {
            bool shouldEvenFire = false;
            Debug.Log("starting the fire thing. aef");
            foreach (var proj in cooldownReference.projectiles)
            {
                Debug.Log(gun.m_moduleData[proj].onCooldown);
                Debug.Log(proj.IsDuctTapeModule);
                Debug.Log(proj.shootStyle != ProjectileModule.ShootStyle.SemiAutomatic);
                if (!gun.m_moduleData[proj].onCooldown && proj.IsDuctTapeModule == ducttape && proj.shootStyle != ProjectileModule.ShootStyle.SemiAutomatic)
                {
                    shouldEvenFire = true;
                    Debug.Log("continue gun fire: wokring?");
                    if (proj.shootStyle == ProjectileModule.ShootStyle.Burst || proj.shootStyle == ProjectileModule.ShootStyle.Automatic)
                    {
                        gun.StartCoroutine(CustomModuleCooldown(gun, proj, cooldownReference.projectiles[0]));
                    }
                }
            }

            bool playEffects = true;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = true;
            for (int i = 0; i < Volley.projectiles.Count; i++)
            {
                ProjectileModule projectileModule = Volley.projectiles[i];
                if (shouldEvenFire)
                {

                    flag = true;
                    if (!gun.UsesRechargeLikeActiveItem || gun.m_remainingActiveCooldownAmount <= 0f)
                    {
                        if (Volley.ModulesAreTiers)
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
                                    playEffects = true;
                                    flag3 = true;
                                }
                                else
                                {
                                    playEffects = false;
                                    flag3 = false;
                                }
                            }
                        }
                        if (projectileModule.isExternalAddedModule)
                        {
                            playEffects = false;
                        }
                        if (flag3)
                        {
                            Debug.Log($"is this ever getting aclled? {canAttack}");
                            flag2 |= gun.HandleSpecificContinueGunShoot(projectileModule, canAttack, null, playEffects);
                        }
                        if (flag2)
                        {
                            playEffects = false;
                        }

                    }
                }
            }
            if (!flag)
            {
                Debug.LogError("(June): Attempting to continue fire without being loaded. This should never happen.");
            }
            return flag2;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Gun), nameof(Gun.ContinueAttack))]
        public static bool OverrideContinueGunShoot(Gun __instance, ref bool __result, bool canAttack, ProjectileData overrideProjectileData)
        {
            Debug.Log("calling continue gun attack method");
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
            ModuleInsertData overrideVolley = holder.GetNextTypeToFire(out ProjectileModule mod);
            if (overrideVolley == null || overrideVolley.DataType != ModuleInsertData.InsertDataType.NEW_STUFF)
            {
                return true;
            }
            ProjectileVolleyData newVolley = overrideVolley.RuntimeVolley;
            if (!newVolley)
            {
                return true;
            }

            bool onlySemi = true;
            foreach (ProjectileModule module in newVolley.projectiles)
            {
                if (!__instance.m_isCurrentlyFiring)
                {
                    if (module.shootStyle == ProjectileModule.ShootStyle.Automatic || module.shootStyle == ProjectileModule.ShootStyle.Charged ||
                        module.shootStyle == ProjectileModule.ShootStyle.Charged)
                    {
                        __result = __instance.Attack() == Gun.AttackResult.Success;
                        return false;
                    }
                }
                else if (module.shootStyle != ProjectileModule.ShootStyle.SemiAutomatic) { onlySemi = false; }
            }
            if (onlySemi) { __instance.ClearBurstState(); return false; }

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
                float num = PlayerStats.GetTotalCoolness() / 100f;
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
                    flag = __instance.ContinueFireFakeVolley(volley, __instance.Volley, mod.IsDuctTapeModule);
                    __instance.m_midBurstFire = false;
                    for (int i = 0; i < newVolley.projectiles.Count; i++)
                    {
                        ProjectileModule projectileModule = newVolley.projectiles[i];
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
                //__instance.m_midBurstFire = false;
                //if (__instance.singleModule.shootStyle == ProjectileModule.ShootStyle.Burst && __instance.m_moduleData[__instance.singleModule].numberShotsFiredThisBurst < __instance.singleModule.burstShotCount)
                //{
                //    __instance.m_midBurstFire = true;
                //}
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
            Debug.Log("setting cooldown");
            Debug.Log(gun.m_moduleData[mod].numberShotsFired);
            gun.m_moduleData[mod].numberShotsFired++;

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
    }
}
