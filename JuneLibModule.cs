using Alexandria.ItemAPI;
using Alexandria.DungeonAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using JuneLib.Items;
using System.Reflection;

namespace JuneLib
{
    [BepInDependency("etgmodding.etg.mtgapi")]
    [BepInDependency("alexandria.etgmod.alexandria")]
    [BepInPlugin(GUID, MOD_NAME, VERSION)]
    public class JuneLibModule : BaseUnityPlugin
    {
        public const string MOD_NAME = "JuneLib";
        public const string VERSION = "1.0.6";
        public static readonly string TEXT_COLOR = "#FFFFFF";
        public static readonly string ASSEMBLY_NAME = "JuneLib";
        public const string GUID = "blazeykat.etg.junelib";

        public void Start()
        {
            Debug.Log("junelib jumpscare");
            ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
        }

        public static bool debugLog = false;

        public void GMStart(GameManager g)
        {
            try
            {
                new Harmony(GUID).PatchAll();
                ETGMod.Assets.SetupSpritesFromAssembly(Assembly.GetExecutingAssembly(), $"{ASSEMBLY_NAME}/Resources/GunSprites");
                JuneLibCore.Init();
                ETGModConsole.Log($"{MOD_NAME} v{VERSION} has successfully started");
            }
            catch (Exception e) { ETGModConsole.Log(e.ToString()); }
        }
    }
}
