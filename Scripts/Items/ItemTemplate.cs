using Alexandria.ItemAPI;
using Alexandria.Misc;
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
    public abstract class ItemTemplateBase
    {
        public abstract void SpecialClassBasedThing(PickupObject pickup);
    }

    public class ItemTemplate : ItemTemplateBase
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
            ManualSpriteID = -1;
        }
        public override void SpecialClassBasedThing(PickupObject pickup) { }

        public string Name;
        public string Description;
        public string LongDescription;
        public string SpriteResource;
        public Type Type;
        public ItemQuality Quality;
        public float Cooldown;
        public ItemBuilder.CooldownType CooldownType;

        public tk2dSpriteCollectionData ManualSpriteCollection;
        public int ManualSpriteID;
        public string ManualSpriteKey;

        public Action<PickupObject> PostInitAction;
    }

    public static class ItemTemplateManager
    {
        internal static Dictionary<Type, Action<ItemTemplate, PickupObject>> AdditionalEffects = new Dictionary<Type, Action<ItemTemplate, PickupObject>>();

        public static int count = 0;
        public static void Init(Assembly assembly = null)
        {
            if (assembly == null) { assembly = Assembly.GetCallingAssembly(); }
            List<Type> items = assembly.GetTypes().Where(type => !type.IsAbstract && typeof(PickupObject).IsAssignableFrom(type)).ToList();

            count = 0;
            foreach (var item in items)
            {
                List<MemberInfo> templates = item.GetMembers(BindingFlags.Static | BindingFlags.Public).Where(member => member.GetValueType() == typeof(ItemTemplate) || typeof(ItemTemplate).IsAssignableFrom(member.GetValueType())).ToList();
                foreach(MemberInfo template in templates)
                {
                    ((ItemTemplate)((FieldInfo)template).GetValue(item)).InitTemplate(assembly);
                }
            }
            if (JuneLibModule.debugLog)
            {
                ETGModConsole.Log($"{PrefixHandler.pairs[assembly].ToTitleCaseInvariant()} Items: {count} / {items.Count}");
            }
            //ETGModConsole.Log(items.Count);
        }

        public static void InitTemplate(this ItemTemplate temp, Assembly assembly)
        {
            string itemName = temp.Name;
            string resourceName = temp.SpriteResource;
            GameObject obj = new GameObject(itemName);
            var item = obj.AddComponent(temp.Type);
            if (temp.ManualSpriteCollection != null)
            {
                tk2dSpriteCollectionData daa = temp.ManualSpriteCollection;
                int id = -1;
                if (temp.ManualSpriteID != -1)
                {
                    id = temp.ManualSpriteID;
                } else if (string.IsNullOrEmpty(temp.ManualSpriteKey))
                {
                    id = daa.GetSpriteIdByName(temp.ManualSpriteKey);
                } else
                {
                    ETGModConsole.Log("JuneLib: Something horrible happened while loading the sprite from the Asset Bundle, and both sprite key and sprite ID is null");
                }
                ItemBuilderAdditions.AddSpriteToObjectAssetbundle(temp.Name, id, daa, obj);
            } else
            {
                Assembly spriteAssembly = temp.SpriteResource != $"{JuneLibModule.ASSEMBLY_NAME}/Resources/example_item_sprite" ? assembly : Assembly.GetExecutingAssembly();
                ItemBuilder.AddSpriteToObject(itemName, resourceName, obj, spriteAssembly);
            }
            PickupObject pobject = (PickupObject)item;

            string shortDesc = temp.Description;
            string longDesc = temp.LongDescription;
            ItemBuilder.SetupItem(pobject, shortDesc, longDesc, PrefixHandler.pairs[assembly]);
            pobject.quality = temp.Quality;

            if (item is PlayerItem pitem)
            {
                pitem.SetCooldownType(temp.CooldownType, temp.Cooldown);
            }
            if (temp.Quality == ItemQuality.EXCLUDED)
            {
                pobject.RemovePickupFromLootTables();
            } else { count++; }

            temp.SpecialClassBasedThing(pobject);
            temp.PostInitAction?.Invoke(pobject);
            //ETGModConsole.Log($"{temp.Name}, {temp.SpriteResource}");
            keyValuePairs.Add(pobject, assembly);
        }

        internal static Dictionary<PickupObject, Assembly> keyValuePairs = new Dictionary<PickupObject, Assembly>();
    }
}
