using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JuneLib
{
    public class PlayerClipModifierHolder : BraveBehaviour
    {
        internal List<ClipModifierBase> modifiers = new List<ClipModifierBase>();
        private PlayerController m_owner;

        public void Start()
        {
            m_owner = GetComponent<PlayerController>();
            if (GunClipModifierHolder.AddDebug)
            {
                AddClipModifier(new TestClipModifier());
            }
        }

        public void AddClipModifier(ClipModifierBase modifier)
        {
            modifiers.Add(modifier);
            RebuildClipModifiers(m_owner);
        }

        public void RemoveClipModifier(ClipModifierBase modifier)
        {
            if (modifiers.Contains(modifier))
            {
                modifiers.Remove(modifier);
                RebuildClipModifiers(m_owner);
            }
        }

        public GunClipModifierHolder GetCurrentGunModifier()
        {
            if (m_owner.CurrentGun)
            {
                return m_owner.CurrentGun.GetComponent<GunClipModifierHolder>();
            }
            return null;
        }

        public void RebuildClipModifiers(PlayerController owner)
        {
            if (owner.inventory == null || owner.inventory.AllGuns == null || owner.inventory.AllGuns.Count == 0)
            {
                return;
            }
            for (int i = 0; i < owner.inventory.AllGuns.Count; i++)
            {
                Gun gun = owner.inventory.AllGuns[i];
                var gunMod = gun.GetComponent<GunClipModifierHolder>();
                if (gunMod == null) { continue; }
                if (!gunMod.ShouldAddModifiers()) { continue; }

                foreach (var module in gun.Volley.projectiles)
                {
                    if (module == gun.DefaultModule || ( module.IsDuctTapeModule && module.ammoCost > 0))
                    {
                        if (!gunMod.modifiers.ContainsKey(module.runtimeGuid))
                        {
                            gunMod.InitializeForModule(module, module == gun.DefaultModule, gun.Volley);
                        }
                        var clipMod = gunMod.modifiers[module.runtimeGuid];

                        clipMod.ReEvaluateModifiers(modifiers);
                    }
                }
            }
        }
    }
}
