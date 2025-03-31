using Gungeon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static PickupObject;

namespace JuneLib.Items
{
    public static class GunMaker
    {

        public static void InitGuns(Assembly assembly = null)
        {
            if (assembly == null) { assembly = Assembly.GetCallingAssembly(); }
            List<Type> items = assembly.GetTypes().Where(type => !type.IsAbstract && typeof(GunBehaviour).IsAssignableFrom(type)).ToList();

            foreach (var item in items)
            {
                List<MemberInfo> templates = item.GetMembers(BindingFlags.Static | BindingFlags.Public).Where(member => member.GetValueType() == typeof(GunTemplate)).ToList();
                foreach (MemberInfo template in templates)
                {
                    ((GunTemplate)((FieldInfo)template).GetValue(item)).InitGunTemplate(assembly);
                }
            }
        }

        public static void InitGunTemplate(this GunTemplate template, Assembly assembly)
        {
            string prefix = PrefixHandler.pairs[assembly];
            string cName = template.Name.ToLower().Replace(" ", "_").Replace("-", string.Empty);
            Gun gun = ETGMod.Databases.Items.NewGun(template.Name, template.CoreSpriteName);
            Game.Items.Rename($"outdated_gun_mods:{cName}", $"{prefix}:{cName}");
            gun.gameObject.AddComponent(template.Type);
            gun.SetShortDescription(template.Description);
            gun.SetLongDescription(template.LongDescription);
            gun.SetupSprite(null, template.CoreSpriteName + "_idle_001");
            gun.gunClass = template.Class;
            gun.quality = template.Quality;

            gun.SetAnimationFPS(gun.idleAnimation, template.IdleFPS);
            gun.SetAnimationFPS(gun.shootAnimation, template.ShootFPS);
            gun.SetAnimationFPS(gun.reloadAnimation, template.ReloadFPS);

            template.PostInitAction?.Invoke(gun);

            gun.maxAmmo = template.MaxAmmo;
            gun.InfiniteAmmo = template.InfAmmo;
            ETGMod.Databases.Items.Add(gun, false);
        }
    }

    public class GunTemplate
    {
        public GunTemplate(Type type)
        {
            Type = type;
            Name = type.Name;
            CoreSpriteName = "junelib_placeholdergun";
            Description = "This is a placeholder";
            LongDescription = "JuneLib generated placeholder description";
            Quality = ItemQuality.EXCLUDED;
            Class = GunClass.NONE; 

            IdleFPS = 10;
            ShootFPS = 10;
            ReloadFPS = 10;

            MaxAmmo = 1;
            InfAmmo = true;
        }

        public string Name;
        public string Description;
        public string LongDescription;
        public string CoreSpriteName;
        public Type Type;
        public ItemQuality Quality;
        public GunClass Class;
        public int MaxAmmo;
        public bool InfAmmo;
        public Action<Gun> PostInitAction;

        public int IdleFPS;
        public int ShootFPS;
        public int ReloadFPS;
    }
}
