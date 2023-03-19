using BepInEx;
using Dungeonator;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static JuneLib.JuneLibRoomRewardAPI;

namespace JuneLib
{
    public static class JuneLibCore
    {
        //DEPRECATED:
        public static Action<DebrisObject, RoomHandler> OnRoomClearItemDrop;
        public static Action<RoomHandler, ValidRoomRewardContents, float> OnRoomRewardDetermineContents;
        public static void Init()
        {
            ConsoleCommandGroup group = ETGModConsole.Commands.AddGroup("junelib", args =>
            {
                ETGModConsole.Log("Please specify a valid command.");
            });
            JunePlayerEvents.Init();

            ItemsCore.Init();
            GoopCore.Init();
            UICore.Init();


            ChallengeHelper.Init();

            /*WMITF HOOK*/
            ETGMod.StartGlobalCoroutine(DelayRunThing());
        }

        public static IEnumerator DelayRunThing()
        {
            yield return null;
            yield return null;

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.Load("JuneLib", currentDomain.Evidence);
            Assembly[] assems = currentDomain.GetAssemblies();

            List<Assembly> searchResult = assems.ToList().FindAll(a => a.GetName().Name == "WMITF");
            if (searchResult.Count > 0)
            {
                try
                {
                    Assembly ItemTipsMod = searchResult.First();
                    wmitfAssembly = ItemTipsMod;

                    Type ItemTipsModule = ItemTipsMod.GetType("WMITF.WMITFModule");
                    wmitfTYPE = ItemTipsModule;

                    /*ETGModConsole.Log(wmitfTYPE.GetMethod("WMITFAddItemToDict", BindingFlags.Public | BindingFlags.Static) == null);
                    Hook hook = new Hook(wmitfTYPE.GetMethod("WMITFAddItemToDict", BindingFlags.Public | BindingFlags.Static), typeof(JuneLibCore).GetMethod("WMITFHook"));*/
                    FieldInfo _itemDict = wmitfTYPE.GetField("WMITFModItemDict", BindingFlags.Public | BindingFlags.Static);
                    Dictionary<PickupObject, Assembly> dict = _itemDict.GetValue(null) as Dictionary<PickupObject, Assembly>;

                    Dictionary<PickupObject, Assembly> mergeDict = new Dictionary<PickupObject, Assembly>();
                    foreach (var kvp in dict)
                    {
                        if (Items.ItemTemplateManager.keyValuePairs.ContainsKey(kvp.Key))
                        {
                            mergeDict.Add(kvp.Key, Items.ItemTemplateManager.keyValuePairs[kvp.Key]);
                        }
                    }

                    foreach (var kvp in mergeDict)
                    {
                        dict[kvp.Key] = kvp.Value;
                    }

                    _itemDict.SetValue(null, dict);

                    MethodInfo _getactualmoditemdict = wmitfTYPE.GetMethod("WMITFGetActualModItemDict", BindingFlags.Public | BindingFlags.Static);
                    _getactualmoditemdict.Invoke(null, new object[] { });
                }
                catch (Exception ex)
                {
                    ETGModConsole.Log("JuneLib: Failed to override WMITF's item dictionary.\nException:\n" + ex.ToString()+"\n\nBlame June. \n(and to a lesser degree blame SpAPI.)");
                }
            }
            yield break;
        }

        public static Type wmitfTYPE = null;
        public static Assembly wmitfAssembly = null;

        /*public static void WMITFHook(Action<PickupObject> orig, PickupObject value)
        {
            StackFrame[] frames = new StackTrace().GetFrames();
            int current = 1;
            while (frames[current].GetMethod().DeclaringType.Assembly == typeof(ETGMod).Assembly || frames[current].GetMethod().DeclaringType.Assembly == typeof(Harmony).Assembly ||
                frames[current].GetMethod().DeclaringType.Assembly == typeof(Hook).Assembly || frames[current].GetMethod().DeclaringType.Assembly.GetName().Name == "Alexandria"
                || frames[current].GetMethod().DeclaringType.Assembly.GetName().Name == "JuneLib")
            {
                current++;
                if (current >= frames.Length)
                {
                    return;
                }
            }
            FieldInfo _itemDict = wmitfTYPE.GetField("WMITFModItemDict", BindingFlags.Public | BindingFlags.Static);
            Dictionary<PickupObject, Assembly> dict = _itemDict.GetValue(null) as Dictionary<PickupObject, Assembly>;
            if (!dict.ContainsKey(value))
            {
                dict.Add(value, frames[current].GetMethod().DeclaringType.Assembly);
                _itemDict.SetValue(null, dict);
            }
            FieldInfo _fullyInited = wmitfTYPE.GetField("WMITFFullyInited", BindingFlags.Public | BindingFlags.Static);
            bool fullyInited = (bool)_fullyInited.GetValue(null);
            if (fullyInited)
            {
                MethodInfo _getactualmoditemdict = wmitfTYPE.GetMethod("WMITFGetActualModItemDict", BindingFlags.Public | BindingFlags.Static);
                _getactualmoditemdict.Invoke(null, new object[] {});
            }
        }*/
    }

    public static class MultiModDictionary
    {

    }
}
