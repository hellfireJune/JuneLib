using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static JuneLib.GunClipModifiers;

namespace JuneLib
{
    public abstract class ClipModifierBase : IComparable<ClipModifierBase>, ICloneable
    {
        public static T Initialize<T>(string identifier) where T : ClipModifierBase, new()
        {
            T modifier = new T
            {
                BaseIdentifier = identifier,
            };
            modifier.PostInit();

            return modifier;
        }

        public abstract void AddVolleys(RuntimeModifierContainer modifier, Gun gun, PlayerController player, ProjectileModule baseModule);
        public virtual void PostInit() { }

        public string BaseIdentifier { get; internal set; }

        //runtime stuff
        public GunClipModifierHolder parent;
        public RuntimeModifierContainer container;

        public bool InnateToGun;
        public bool BuildOnReload = true;
        public ProjectileModule OverrideIcon;
        public float Priority = 0;

        public List<ModuleInsertData> BuildClipModifier(ProjectileModule baseModule)
        {
            container.InsertedDatas.Clear();
            if (container != null && parent != null)
            {
                AddVolleys(container, parent.Gun, parent.Owner, baseModule);
            }
            return container.InsertedDatas;
        }

        public int CompareTo(ClipModifierBase other)
        {
            return other.Priority.CompareTo(Priority);
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
