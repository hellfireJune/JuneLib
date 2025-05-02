using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static JuneLib.GunModifyHeart;
using static JuneLib.RegeneratingVolleyModifiers;

namespace JuneLib
{
    [HarmonyPatch]
    public static class RegeneratingVolleyModifiers
    {
        [HarmonyPatch(typeof(Gun), nameof(Gun.Update))]
        [HarmonyPostfix]
        public static void InitGun(Gun __instance)
        {
            if (__instance.gameObject.GetComponent<HasCheckedForGunModifyThing>())
            {
                return;
            }
            __instance.gameObject.AddComponent<HasCheckedForGunModifyThing>();
            GameActor owner = __instance.CurrentOwner;
            if (owner && owner is PlayerController)
            {
                GunModifyHeart modify = __instance.gameObject.AddComponent<GunModifyHeart>();
                modify.m_gun = __instance;
            }
        }

        [HarmonyPatch(typeof(Gun), nameof(Gun.Volley), MethodType.Getter)]
        [HarmonyPostfix]
        public static void ModifyReturnedVolley(Gun __instance, ref ProjectileVolleyData __result)
        {
            if (__instance.CurrentOwner == null || !(__instance.CurrentOwner is PlayerController player))
            {
                return;
            }
            if (__instance.modifiedVolley == null || __result == __instance.rawVolley
                || SkipCheck)
            {
                return;
            }
            GunModifyHeart modifier = __instance.gameObject.GetComponent<GunModifyHeart>();
            if (modifier)
            {
                ProjectileVolleyData cached = modifier.GetCachedModifiedVolley("baseGunVolley");
                if (cached)
                {
                    __result = cached;
                }
                else
                {

                    if (player != null)
                    {

                        SkipCheck = true;
                        ProjectileVolleyData volley = __instance.Volley;
                        //Debug.Log(volley.projectiles.Count);
                        volley = modifier.RunIndividualModifier("baseGunVolley", volley, player, BaseVolleyModifier, __instance);
                        //modifier.projsOnCooldown = projArgs.projs;

                        __result = volley;
                        SkipCheck = false;
                        __instance.ReinitializeModuleData(__result);
                    }
                }
            }
        }

        public static List<string> bonusVolleys = new List<string>
        {
            "finalGunVolley",
            "reloadGunVolley"
        };
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Gun), nameof(Gun.Update))]
        public static void Updately(Gun __instance)
        {

            if (__instance.CurrentOwner == null || !(__instance.CurrentOwner is PlayerController player) || player == null)
            {
                return;
            }
            if ((__instance.modifiedFinalVolley == null && __instance.modifiedOptionalReloadVolley) || SkipCheck)
            {
                return;
            }
            GunModifyHeart modifier = __instance.gameObject.GetComponent<GunModifyHeart>();
            if (modifier)
            {
                foreach (var idx in bonusVolleys)
                {
                    if (!modifier.GetCachedModifiedVolley(idx))
                    {
                        bool isFinal = idx == "finalGunVolley";
                        ProjectileVolleyData volley = isFinal ? __instance.modifiedFinalVolley : __instance.modifiedOptionalReloadVolley;
                        if (volley != null)
                        {
                            Action<ProjectileModule, IndividualModifier, Gun> weapon = FinalVolleyModifier;
                            if (!isFinal)
                            {
                                weapon = ReloadVolleyModifier;
                            }
                            volley = modifier.RunIndividualModifier(idx, volley, player, (weapon),  __instance);
                            //modifier.projsOnCooldown = projArgs.projs;

                            __instance.ReinitializeModuleData(volley);
                        }
                    }
                }
            }
        }
        public static bool SkipCheck = false;
        public static bool Log = false;

        [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.RebuildGunVolleys))]
        [HarmonyPrefix]
        public static void ILove() { SkipCheck = true; Log = true; }

        [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.RebuildGunVolleys))]
        [HarmonyPostfix]
        public static void DoingThis() { SkipCheck = false; Log = false; }

        public static List<ProjectileModule> AllAvailableModules(ProjectileVolleyData data, Gun gun)
        {

            List<ProjectileModule> availableModules = new List<ProjectileModule>();
            foreach (var module in data.projectiles)
            {
                if (!gun.m_moduleData[module].onCooldown)
                {
                    availableModules.Add(module);
                }
            }
            return availableModules;
        }

        public class ModifyProjArgs : EventArgs
        {
            public Dictionary<string, List<ProjectileModule>> projs;
        }

        public static ProjectileVolleyData OnTheGoModifyVolley(ProjectileVolleyData data, PlayerController owner, Gun gun = null)
        {
            ProjectileVolleyData newVolley = ScriptableObject.CreateInstance<ProjectileVolleyData>();
            newVolley.InitializeFrom(data);

            owner.stats.ModVolley(owner, newVolley);
            ModifyProjArgs projArgs = new ModifyProjArgs() { projs = new Dictionary<string, List<ProjectileModule>>() };
            if (owner.GetJEvents().ConstantModifyGunVolley != null)
            {
                owner.GetJEvents().ConstantModifyGunVolley?.Invoke(owner, gun, newVolley, projArgs);

                foreach (var stuff in projArgs.projs)
                {
                    //ETGModConsole.Log("is worth removing");
                    foreach (var mod in stuff.Value)
                    {
                        newVolley.projectiles.Add(mod);
                    }
                }
            }

            return newVolley;
        }

        internal static void FinalVolleyModifier(ProjectileModule mod, IndividualModifier modifier, Gun gun)
        {
            gun.modifiedFinalVolley.projectiles.Remove(mod);
        }
        internal static void ReloadVolleyModifier(ProjectileModule mod, IndividualModifier modifier, Gun gun)
        {
            gun.modifiedOptionalReloadVolley.projectiles.Remove(mod);
        }
        internal static void BaseVolleyModifier(ProjectileModule mod, IndividualModifier modifier, Gun gun)
        {
            gun.Volley.projectiles.Remove(mod);
        }
    }


    internal class GunModifyHeart : BraveBehaviour
    {
        public Dictionary<string, IndividualModifier> modifiers = new Dictionary<string, IndividualModifier>();
        internal Gun m_gun;

        public void InitializeModifier(string idx, ProjectileVolleyData volley, Action<ProjectileModule, IndividualModifier, Gun> modifier)
        {
            IndividualModifier mod = new IndividualModifier
            {
                papa = this,
                remove = modifier
            };
            modifiers.Add(idx, mod);
        }

        public ProjectileVolleyData GetCachedModifiedVolley(string idx)
        {
            if (modifiers.ContainsKey(idx))
            {
                return modifiers[idx].cachedModifiedVolley;
            } else { return null; }
        }

        public ProjectileVolleyData RunIndividualModifier(string idx, ProjectileVolleyData volley, PlayerController player, Action<ProjectileModule, IndividualModifier, Gun> runner, Gun gun = null)
        {
            if (!modifiers.ContainsKey(idx))
            {
                InitializeModifier(idx, volley, runner);
            }
            IndividualModifier modifier = modifiers[idx];

            ModifyProjArgs projArgs = new ModifyProjArgs() { projs = new Dictionary<string, List<ProjectileModule>>() };
            if (player.GetJEvents().ConstantModifyGunVolley != null)
            {
                player.GetJEvents().ConstantModifyGunVolley?.Invoke(player, gun, volley, projArgs);
                Debug.Log($"is running the stuff for {idx}");
                if (modifier.projsOnCooldown == null)
                {
                    modifier.projsOnCooldown = new Dictionary<string, List<ProjectileModule>>();
                }
                foreach (var projMod in modifier.projsOnCooldown)
                {
                    foreach (var thing in projMod.Value)
                    {
                        if (!volley.projectiles.Contains(thing))
                        {
                            volley.projectiles.Add(thing);
                        }
                    }
                }

                foreach (var stuff in projArgs.projs)
                {
                    if (modifier.projsOnCooldown.ContainsKey(stuff.Key))
                    {
                        //Debug.Log("contains key " + stuff.Key);
                    }
                    else
                    {
                        //ETGModConsole.Log("is worth removing");
                        foreach (var mod in stuff.Value)
                        {
                            volley.projectiles.Add(mod);
                        }
                        modifier.projsOnCooldown[stuff.Key] = stuff.Value;
                    }
                }
            }

            modifier.cachedModifiedVolley = volley;
            return volley;
        }

        private void FixedUpdate()
        {
            foreach (var mod in modifiers.Values)
            {
                mod.UpdateStuff();
            }
        }

        public class IndividualModifier
        {
            internal ProjectileVolleyData cachedModifiedVolley;
            public Dictionary<string, List<ProjectileModule>> projsOnCooldown;
            public GunModifyHeart papa;
            public Action<ProjectileModule, IndividualModifier, Gun> remove;
            internal void UpdateStuff()
            {
                if (papa.m_gun != null && projsOnCooldown != null)
                {
                    List<string> toRemove = new List<string>();
                    foreach (var entry in projsOnCooldown)
                    {
                        bool worthRemoving = true;
                        foreach (var module in entry.Value)
                        {
                            //ETGModConsole.Log(m_gun.m_moduleData[module].onCooldown);
                            if (papa.m_gun.m_moduleData[module].onCooldown)
                            {
                                worthRemoving = false;
                            } 
                            else
                            {
                                remove?.Invoke(module, this, papa.m_gun);
                            }
                        }
                        //ETGModConsole.Log($"{entry.Key}, {worthRemoving}");
                        if (worthRemoving)
                        {
                            toRemove.Add(entry.Key);
                        }
                    }
                    foreach (var module in toRemove)
                    {
                        projsOnCooldown.Remove(module);
                    }
                }
                cachedModifiedVolley = null;
            }
        }
    }


    internal class HasCheckedForGunModifyThing : BraveBehaviour { }
}
