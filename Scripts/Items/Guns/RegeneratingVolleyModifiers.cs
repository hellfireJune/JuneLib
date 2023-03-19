using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
                GunModifyThing modify = __instance.gameObject.AddComponent<GunModifyThing>();
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
            GunModifyThing modifier = __instance.gameObject.GetComponent<GunModifyThing>();
            if (modifier)
            {

                if (modifier.cachedModifiedVolley)
                {
                    __result = modifier.cachedModifiedVolley;
                }
                else
                {

                    if (player != null)
                    {

                        SkipCheck = true;
                        ProjectileVolleyData volley = __instance.Volley;
                        //Debug.Log(volley.projectiles.Count);
                        ModifyProjArgs projArgs = new ModifyProjArgs() { projs = new Dictionary<string, List<ProjectileModule>>() };
                        List<ProjectileModule> newMods = new List<ProjectileModule>();
                        if (player.GetJEvents().ConstantModifyGunVolley != null)
                        {
                            player.GetJEvents().ConstantModifyGunVolley?.Invoke(player, __instance, volley, projArgs);
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
                        //modifier.projsOnCooldown = projArgs.projs;

                        __result = volley;
                        SkipCheck = false;
                        __instance.ReinitializeModuleData(__result);
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
    }


    internal class GunModifyThing : BraveBehaviour
    {
        internal ProjectileVolleyData cachedModifiedVolley;
        public Dictionary<string, List<ProjectileModule>> projsOnCooldown;
        internal Gun m_gun;
        private void FixedUpdate()
        {
            if (m_gun != null && projsOnCooldown != null)
            {
                List<string> toRemove = new List<string>();
                foreach (var entry in projsOnCooldown)
                {
                    bool worthRemoving = true;
                    foreach (var module in entry.Value)
                    {
                        //ETGModConsole.Log(m_gun.m_moduleData[module].onCooldown);
                        if (m_gun.m_moduleData[module].onCooldown)
                        {
                            worthRemoving = false;
                        } else
                        {
                            m_gun.Volley.projectiles.Remove(module);
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

    internal class HasCheckedForGunModifyThing : BraveBehaviour { }
}
