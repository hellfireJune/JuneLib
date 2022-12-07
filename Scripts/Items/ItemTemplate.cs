using Alexandria.ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static PickupObject;
using System.Reflection;
using Gungeon;

namespace JuneLib.Items
{
    public class ItemTemplate
    {
        public ItemTemplate(Type type)
        {
            Type = type;
            Name = type.Name;
            SpriteResource = $"{JuneLibModule.ASSEMBLY_NAME}/Resources/example_item_sprite";
            Description = "This is a placeholder";
            LongDescription = "JuneLib generated placeholder description";
            Quality = ItemQuality.EXCLUDED;
            Cooldown = 0f;
            CooldownType = ItemBuilder.CooldownType.Damage;
        }

        public string Name;
        public string Description;
        public string LongDescription;
        public string SpriteResource;
        public Type Type;
        public ItemQuality Quality;
        public float Cooldown;
        public ItemBuilder.CooldownType CooldownType;

        public Action<PickupObject> PostInitAction;
    }

    public static class ItemTemplateManager
    {
        public static void Init(Assembly assembly = null)
        {
            if (assembly == null) { assembly = Assembly.GetCallingAssembly(); }
            List<Type> items = assembly.GetTypes().Where(type => !type.IsAbstract && typeof(PickupObject).IsAssignableFrom(type)).ToList();

            foreach (var item in items)
            {
                List<MemberInfo> templates = item.GetMembers(BindingFlags.Static | BindingFlags.Public).Where(member => member.GetValueType() == typeof(ItemTemplate)).ToList();
                foreach(MemberInfo template in templates)
                {
                    ((ItemTemplate)((FieldInfo)template).GetValue(item)).InitTemplate(assembly);
                }
            }
            //ETGModConsole.Log(items.Count);
        }

        public static void InitTemplate(this ItemTemplate temp, Assembly assembly)
        {
            string itemName = temp.Name;
            string resourceName = temp.SpriteResource;
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent(temp.Type);
            Assembly spriteAssembly = temp.SpriteResource != $"{JuneLibModule.ASSEMBLY_NAME}/Resources/example_item_sprite" ? assembly : Assembly.GetExecutingAssembly();
            ItemBuilder.AddSpriteToObject(itemName, resourceName, obj, spriteAssembly);
            string shortDesc = temp.Description;
            string longDesc = temp.LongDescription;
            ItemBuilder.SetupItem((PickupObject)item, shortDesc, longDesc, PrefixHandler.pairs[assembly]);
            ((PickupObject)item).quality = temp.Quality;

            if (item is PlayerItem pitem)
            {
                pitem.SetCooldownType(temp.CooldownType, temp.Cooldown);
            }

            temp.PostInitAction?.Invoke((PickupObject)item);
            //ETGModConsole.Log($"{temp.Name}, {temp.SpriteResource}");
        }
    }
}
