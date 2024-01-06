using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JuneLib
{

    public class GunClipModifierHolder : BraveBehaviour
    {
        public Gun Gun { get; private set; }
        public PlayerController Owner { get; private set; }
        internal static void Initialize(Gun gun, PlayerController owner)
        {
            GunClipModifierHolder mod = gun.gameObject.AddComponent<GunClipModifierHolder>();
            mod.Gun = gun;
            mod.Owner = owner;
        }

        public bool ShouldAddModifiers()
        {
            return !Gun.IsHeroSword || !Gun.UsesRechargeLikeActiveItem;
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

        public int GetNextTypeToFire(ProjectileModule module)
        {
            int num = GetPos(module);
            GunClipModifiers mod = modifiers[module];

            for (int i = 0; i < mod.CurrentModifiers.Count; i++)
            {
                var modifier = mod.CurrentModifiers[i].ModifierPositions;
                foreach (var j in modifier)
                {
                    if (j.Position == num)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        internal void InitializeForModule(ProjectileModule module)
        {
            modifiers.Add(module, new GunClipModifiers()
            {
                parent = this
            });
        }

        public Dictionary<ProjectileModule, GunClipModifiers> modifiers = new Dictionary<ProjectileModule, GunClipModifiers>();

        public bool ShouldUpdateUI = false;
    }
    public class GunClipModifiers 
    {
        public GunClipModifierHolder parent;
        public void ReEvaluateModifiers(List<ClipModifierBase> externalModifiers)
        {
            List<RuntimeModifierContainer> modifiers = new List<RuntimeModifierContainer>();
            Dictionary<string, List<InsertData>> keyValuePair = new Dictionary<string, List<InsertData>>();

            foreach (var modifier in CurrentModifiers)
            {
                if (modifier.Modifier.InnateToGun)
                {
                    modifiers.Add(modifier);
                }
                else
                {
                    keyValuePair.Add(modifier.Identifier, modifier.ModifierPositions);
                }
            }

            List<string> alreadyAdded = new List<string>();
            foreach (var mod in externalModifiers)
            {
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
                        ModifierPositions = keyValuePair[ident],
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

            RuntimeModifierContainer container = new RuntimeModifierContainer()
            {
                Modifier = mod,
                Identifier = identifier ?? mod.BaseIdentifier
            };
            container.Modifier.container = container;
            CurrentModifiers.Add(container);
        }

        public List<RuntimeModifierContainer> CurrentModifiers = new List<RuntimeModifierContainer>();

        public class RuntimeModifierContainer
        {
            public ClipModifierBase Modifier;
            public List<InsertData> ModifierPositions;
            public string Identifier;

            public void InsertData(int position, ProjectileVolleyData volley, ProjectileModule overrideIcon = null)
            {
                ModifierPositions.Add(new InsertData()
                {
                    Position = position,
                    RuntimeVolley = volley,
                    OverrideIcon = overrideIcon
                });
            }
        }
        public class InsertData
        {
            public ProjectileModule OverrideIcon;
            public ProjectileVolleyData RuntimeVolley;
            public int Position;
            public bool Fired;
        }
    }
}
