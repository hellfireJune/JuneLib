/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.Misc;
using HarmonyLib;
using UnityEngine;

namespace JuneLib.UI.CustomHealth
{
    /*
     * Current Health:
     * Apply DMG Directional
     * Apply Healing
     * Awake
     * ForceSetCurrentHealth
     * Full Heal
     * Set Health Maximum
     * Cursed Maximum Setter
    

    [HarmonyPatch]
    [HarmonyPatch]
    [HarmonyPatch]
    [HarmonyPatch]
    [HarmonyPatch]
    [HarmonyPatch]
    [HarmonyPatch]
    [HarmonyPatch]
    [HarmonyPatch]
    [HarmonyPatch]
    [HarmonyPatch]
    internal class HealthHaverHooks
    {
        public static void Init()
        {
            CustomActions.OnNewPlayercontrollerSpawned += AddComponent;
        }

        public static void AddComponent(PlayerController player)
        {
            player.gameObject.AddComponent<JuneHealthHaver>();
        }

        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.Armor), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool ArmorGetter(ref float __result, HealthHaver __instance)
        {
            JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
            if (juneHealthHaver)
            {
                __result = juneHealthHaver.Armor;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.Armor), MethodType.Setter)]
        [HarmonyPrefix]
        public static bool ArmorSetter(float value, HealthHaver __instance)
        {
            JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
            if (juneHealthHaver)
            {
                juneHealthHaver.Armor = value;
                return false;
            }
            return true;

        }

        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.ApplyDamageDirectional))]
        [HarmonyPrefix]
        public static bool ApplyDMG(HealthHaver __instance, float damage, Vector2 direction, string damageSource, CoreDamageTypes damageTypes, DamageCategory damageCategory, bool ignoreInvulnerabilityFrames, PixelCollider hitPixelCollider, bool ignoreDamageCaps)
        {
            JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
            if (juneHealthHaver)
            {
                juneHealthHaver.ApplyDamageDirectional(damage, direction, damageSource, damageTypes, damageCategory, ignoreInvulnerabilityFrames, hitPixelCollider, ignoreDamageCaps);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.GetCurrentHealth))]
        [HarmonyPrefix]
        public static bool GetHealth(HealthHaver __instance, ref float __result)
        {
            JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
            if (juneHealthHaver)
            {
                __result = juneHealthHaver.GetCurrentHealth();
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.FullHeal))]
        [HarmonyPrefix]
        public static bool FullHeal(HealthHaver __instance)
        {
            JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
            if (juneHealthHaver)
            {
                juneHealthHaver.FullHeal();
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.ApplyHealing))]
        [HarmonyPrefix]
        public static bool ApplyHeal(HealthHaver __instance, float healing)
        {
            JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
            if (juneHealthHaver)
            {
                juneHealthHaver.ApplyHealing(healing);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.AdjustedMaxHealth), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool MaxHPGetter(ref float __result, HealthHaver __instance)
        {
            JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
            if (juneHealthHaver)
            {
                __result = juneHealthHaver.AdjustedMaxHealth;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.AdjustedMaxHealth), MethodType.Setter)]
        [HarmonyPrefix]
        public static bool MaxHPSetter(float value, HealthHaver __instance)
        {
            JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
            if (juneHealthHaver)
            {
                juneHealthHaver.AdjustedMaxHealth = value;
                return false;
            }
            return true;

        }

        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.GetMaxHealth))]
        [HarmonyPrefix]
        public static bool GetMaxHealth(HealthHaver __instance, ref float __result)
        {
            JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
            if (juneHealthHaver)
            {
                __result = juneHealthHaver.GetMaxHealth();
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.SetHealthMaximum))]
        [HarmonyPrefix]
        public static bool SetMaxHealth(HealthHaver __instance, float targetValue, float? amountOfHealthToGain, bool keepHealthPercentage)
        {
            JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
            if (juneHealthHaver)
            {
                juneHealthHaver.SetHealthMaximum(targetValue, amountOfHealthToGain, keepHealthPercentage);
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.GetCurrentHealthPercentage))]
        [HarmonyPrefix]
        public static bool GetHealthPercentage(HealthHaver __instance, ref float __result)
        {
            JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
            if (juneHealthHaver)
            {
                __result = juneHealthHaver.GetCurrentHealthPercentage();
                return false;
            }
            return true;
        }
    }
}*/
