using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JuneLib
{
    public static class ItemBuilderAdditions
    {
        //stolen from bunny so i can make thing work
        public static GameObject AddSpriteToObjectAssetbundle(string name, int CollectionID, tk2dSpriteCollectionData data, GameObject obj = null)
        {
            GameObject spriteObject = SpriteFromBundle(name, CollectionID, data, obj);
            spriteObject.name = name;
            return spriteObject;
        }
        public static GameObject SpriteFromBundle(string spriteName, int CollectionID, tk2dSpriteCollectionData data, GameObject obj = null)
        {
            if (obj == null)
            {
                obj = new GameObject();
            }
            tk2dSprite sprite;
            sprite = obj.AddComponent<tk2dSprite>();
            sprite.SetSprite(data, CollectionID);
            sprite.SortingOrder = 0;
            sprite.IsPerpendicular = true;

            obj.GetComponent<BraveBehaviour>().sprite = sprite;

            return obj;
        }
    }
}
