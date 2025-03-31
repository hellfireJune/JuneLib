using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static JuneLib.GunClipModifiers;

namespace JuneLib
{

    public class GunClipModifierHolder : MonoBehaviour
    {
        public Gun Gun { get; private set; }
        public PlayerController Owner { get; private set; }

        public static GunClipModifierHolder Initialize(Gun gun, PlayerController owner)
        {
            
            GunClipModifierHolder mod = gun.gameObject.GetOrAddComponent<GunClipModifierHolder>();
            mod.Gun = gun;

            Debug.Log(gun.DefaultModule.runtimeGuid);
            Debug.Log(gun.DefaultModule.CloneSourceIndex);
            Debug.Log(gun.Volley.projectiles.Count);
            Debug.Log(mod.modifiers?.Count);

            if (!mod.modifiers.ContainsKey(gun.DefaultModule.runtimeGuid))
            {
                Debug.Log("it makes it again");
                mod.InitializeForModule(gun.DefaultModule, true, gun.Volley);
                if (StaticInnateModifiers.ContainsKey(gun.PickupObjectId))
                {
                    foreach (var modifier in StaticInnateModifiers[gun.PickupObjectId])
                    {
                        mod.modifiers[gun.DefaultModule.runtimeGuid].AddClipModifierToGun(modifier, true);
                    }
                }
            } else
            {
                Debug.Log("no it doesnt");
            }
            if (owner)
            {
                mod.Owner = owner;
                var ownerHolder = owner.GetComponent<PlayerClipModifierHolder>();
                ownerHolder.RebuildClipModifiers(owner);
                mod.ReloadRebuild(false);
            }
            Debug.Log(mod.modifiers.Count);
            return mod;
        }

        public static Dictionary<int, List<ClipModifierBase>> StaticInnateModifiers = new Dictionary<int, List<ClipModifierBase>>();
        public static void AddInnateStaticModifier(ClipModifierBase modifier, Gun gun = null)
        {
            int id = PickupObjectDatabase.Instance.Objects.Count;
            if (gun != null)
            {
                id = gun.PickupObjectId;
            }
            ETGModConsole.Log(id);

            List<ClipModifierBase> mods;
            if (!StaticInnateModifiers.ContainsKey(id))
            {
                mods = new List<ClipModifierBase>();
            } else
            {
                mods = StaticInnateModifiers[id];
            }
            mods.Add(modifier);
            StaticInnateModifiers.Add(id, mods);
        }

        public static bool hasAddedDebug = false;

        public static readonly int TEST_GUN_ID = 129;
        public bool ShouldAddModifiers()
        {
            return !Gun.IsHeroSword && !Gun.UsesRechargeLikeActiveItem && (!AddDebug || Gun.PickupObjectId != TEST_GUN_ID);
            //ill make these compatible maybe one day
        }

        public int GetPos(ProjectileModule module)
        {

            int num;
            if (Gun.m_moduleData == null || !Gun.m_moduleData.ContainsKey(module))
            {
                num = 0;
            }
            else
            {
                num = Gun.m_moduleData[module].numberShotsFired;
            }
            return num;
        }

        internal void InitializeForModule(ProjectileModule module, bool isDefaultModule = false, ProjectileVolleyData volley = null)
        {
            Debug.Log($"is running init for module");
            modifiers.Add(module.runtimeGuid, new GunClipModifiers()
            {
                parent = this,
                hostProjectile = module,
                isDefaultModule = isDefaultModule
            });
            if (volley.ModulesAreTiers)
            {
                foreach (var thing in volley.projectiles)
                {
                    altModifiers.Add(thing.runtimeGuid, module.runtimeGuid);
                }
            }
        }

        public ModuleInsertData GetNextTypeToFire(out ProjectileModule returnMod)
        {
            returnMod = null;

            Debug.Log("trying to get a thing to fire");
            foreach (var activeMod in modifiers)
            {
                returnMod = activeMod.Value.hostProjectile;
                int num = GetPos(returnMod);
                GunClipModifiers mod = modifiers[returnMod.runtimeGuid];

                /*Debug.Log(num);
                Debug.Log(mod.CurrentFireResults == null);
                Debug.Log(mod.RuntimePositionContainers == null);*/
                if (mod.CurrentFireResults.Count > num)
                {
                    var insert = mod.CurrentFireResults[num];
                    Debug.Log("got a thing to fire");
                    return insert;
                }

                /*var sortedMods = mod.PositionContainersSorted;
                for (int i = 0; i < sortedMods.Count; i++)
                {
                    var modifier = sortedMods[i].InsertedDatas;
                    int idx = 0;
                    foreach (var j in modifier)
                    {
                        if (j.Position == num)
                        {
                            var projMod = j.RuntimeVolley.projectiles[0];
                            if (Gun.m_moduleData.ContainsKey(projMod))
                            {
                                int numFired = Gun.m_moduleData[projMod].numberShotsFired;
                                if (numFired <= idx)
                                {
                                    returnMod = module;
                                    return j;
                                }
                            }
                        }
                        idx++;
                    }
                }*/
            }
            return null;
        }

        public GunClipModifiers GetModifier(string modifierKey)
        {
            if (!modifiers.ContainsKey(modifierKey))
            {
                if (altModifiers.ContainsKey(modifierKey))
                {
                    modifierKey = altModifiers[modifierKey];
                } else { return null; }
            }
            return modifiers[modifierKey];
        }

        [SerializeField]
        public Dictionary<string, GunClipModifiers> modifiers = new Dictionary<string, GunClipModifiers>();
        internal Dictionary<string, string> altModifiers = new Dictionary<string, string>();

        public bool ShouldUpdateUI = false;
        public static bool AddDebug = false;

        public void ReloadRebuild(bool isNotFromReload = false)
        {
            Debug.Log($"running reload rebuild for {Gun.DisplayName}");
            if (modifiers == null)
            {
                return;
            }
            foreach (var key in modifiers.Keys)
            {
                var mods = modifiers[key];
                var projMod = mods.hostProjectile;
                //if (mods.RuntimePositionContainers != null) { continue; }
                for (int i = 0; i < mods.RuntimePositionContainers.Count; i++)
                {
                    var mod = mods.RuntimePositionContainers[i];
                    if (mod.Modifier.BuildOnReload)
                    {
                        mod.Modifier.BuildClipModifier(projMod);
                    }
                }
                //Now process the actual datas
                int maxClipNum = projMod.ModifierlessGetModNumberClipShot(Owner);
                int maxFinalShotNum = !projMod.usesOptionalFinalProjectile ? 0 : projMod.GetModifiedNumberOfFinalProjectiles(Owner);

                List<ModuleInsertData> result = new List<ModuleInsertData>();
                for (int i = 0; i < maxClipNum; i++)
                {
                    result.Add(new ModuleInsertData(ModuleInsertData.InsertDataType.MAIN_CLIP));
                }
                for (int i = 0; i < maxFinalShotNum; i++)
                {
                    result.Add(new ModuleInsertData(ModuleInsertData.InsertDataType.FINAL_PROJECTILE));
                }

                int add = 0;
                var sortedMods = mods.PositionContainersSorted;
                for (int i = 0; i < sortedMods.Count; i++)
                {
                    int customIdx = i + 1;
                    var mod = sortedMods[i];
                    foreach (var pos in mod.InsertedDatas)
                    {
                        pos.TypeIdx = customIdx;
                        result.Insert(pos.Position+add, pos);
                        add++;
                    }
                }

                mods.CurrentBonusClipSize = add;
                mods.CurrentFireResults = result;
            }

        }
    }
    [SerializeField]
    public class GunClipModifiers 
    {
        public List<ModuleInsertData> CurrentFireResults = new List<ModuleInsertData>();
        public int CurrentBonusClipSize = 0;
        public bool isDefaultModule = false;
        public GunClipModifierHolder parent;
        public void ReEvaluateModifiers(List<ClipModifierBase> externalModifiers)
        {
            Debug.Log("reevaluating");
            List<RuntimeModifierContainer> modifiers = new List<RuntimeModifierContainer>();
            Dictionary<string, List<ModuleInsertData>> keyValuePair = new Dictionary<string, List<ModuleInsertData>>();

            foreach (var modifier in RuntimePositionContainers)
            {
                Debug.Log(modifier.Modifier.BaseIdentifier);
                if (modifier.Modifier.InnateToGun)
                {
                    modifiers.Add(modifier);
                }
                else
                {
                    keyValuePair.Add(modifier.Identifier, modifier.InsertedDatas);
                }
            }

            List<string> alreadyAdded = new List<string>();
            Debug.Log(externalModifiers.Count);
            for (int i = 0; i < externalModifiers.Count; i++)
            {
                var mod = externalModifiers[i].Clone() as ClipModifierBase;
                string ident = mod.BaseIdentifier;
                int suffix = 0;
                while (alreadyAdded.Contains($"{ident}-{suffix}"))
                {
                    suffix++;
                }
                ident = $"{ident}-{suffix}";

                if (keyValuePair.ContainsKey(ident))
                {
                    modifiers.Add(new RuntimeModifierContainer()
                    {
                        Modifier = mod,
                        InsertedDatas = keyValuePair[ident],
                        Identifier = ident
                    });
                }
                else
                {
                    parent.ShouldUpdateUI = true;
                    AddClipModifierToGun(mod, false, ident);
                }
                alreadyAdded.Add(ident);
            }
        }

        public void AddClipModifierToGun(ClipModifierBase modifier, bool innate = false, string identifier = null)
        {
            ClipModifierBase mod = modifier;
            mod.InnateToGun = innate;
            mod.parent = parent;
            Debug.Log($"adding modifiers to the {parent.Gun.DisplayName}");

            RuntimeModifierContainer container = new RuntimeModifierContainer()
            {
                Modifier = mod,
                Identifier = identifier ?? mod.BaseIdentifier,
                InsertedDatas = new List<ModuleInsertData>()
            };
            container.Modifier.container = container;
            RuntimePositionContainers.Add(container);
            container.Modifier.BuildClipModifier(hostProjectile);
        }

        public List<RuntimeModifierContainer> RuntimePositionContainers = new List<RuntimeModifierContainer>();

        public List<RuntimeModifierContainer> PositionContainersSorted 
        { 
            get 
            {
                var list = RuntimePositionContainers;
                list.Sort();
                return list; 
            } 
        }

        public ProjectileModule hostProjectile = null;

        public class RuntimeModifierContainer : IComparable<RuntimeModifierContainer>
        {
            public ClipModifierBase Modifier;
            public List<ModuleInsertData> InsertedDatas;

            public string Identifier;

            public int CompareTo(RuntimeModifierContainer other)
            {
                return Modifier.CompareTo(other.Modifier);
            }

            public void InsertData(int position, ProjectileVolleyData volley)
            {
                Debug.Log("it's running the inserted data thing?");
                InsertedDatas.Add(new ModuleInsertData(ModuleInsertData.InsertDataType.NEW_STUFF)
                {
                    Position = position,
                    RuntimeVolley = volley,
                });
            }
        }
    }

    public class ModuleInsertData
    {
        public ModuleInsertData(InsertDataType type, int typeIdx = 0)
        {
            DataType = type;
        }

        public enum InsertDataType
        {
            MAIN_CLIP,
            FINAL_PROJECTILE,
            NEW_STUFF
        }
        public InsertDataType DataType { get; internal set; }
        public ProjectileVolleyData RuntimeVolley;
        public int Position;
        public int TypeIdx = 0;
    }
}
