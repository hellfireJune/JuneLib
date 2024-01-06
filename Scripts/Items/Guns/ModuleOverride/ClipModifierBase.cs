using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static JuneLib.GunClipModifiers;

namespace JuneLib
{
    public abstract class ClipModifierBase
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

        public Gun GetParentGun()
        {
            if (parent == null)
            {
                return null;
            }
            return parent.GetComponent<Gun>();
        }

        public void BuildClipModifier(ProjectileModule baseModule)
        {
            if (container != null)
            {
                AddVolleys(container, parent.Gun, parent.Owner, baseModule);
            }
        }
    }
}
