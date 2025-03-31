using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JuneLib
{
    [HarmonyPatch]
    public static class AmmoTiledSpriteOverrider
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(dfTiledSprite), nameof(dfTiledSprite.OnRebuildRenderData))]
        public static bool OnRebuildRenderData(dfTiledSprite __instance)
        {
            var hijacker = __instance.gameObject.GetComponent<JunesCoolDfTiledSpriteHijacker>();
            if (hijacker == null)
            {
                return true;
            }
            if (__instance.Atlas == null)
                return false;

            var spriteInfo = __instance.SpriteInfo;
            if (spriteInfo == null)
                return false;

            __instance.renderData.Material = __instance.Atlas.Material;

            var verts = __instance.renderData.Vertices;
            var uv = __instance.renderData.UV;
            var colors = __instance.renderData.Colors;
            var triangles = __instance.renderData.Triangles;

            var quadUV = __instance.buildQuadUV();

            var tileSize = Vector2.Scale(spriteInfo.sizeInPixels, __instance.tileScale);
            var tileOffset = new Vector2((__instance.tileScroll.x % 1f), (__instance.tileScroll.y % 1f));

            var y = -Mathf.Abs(tileOffset.y * tileSize.y);
            int yOffset = 0;

            int numsRunThrough = 0;
            var gapmaster = hijacker.currentTileGaps;
            //Debug.Log(gapmaster == null);

            float currentTileScale = Pixelator.Instance.CurrentTileScale;
            Debug.Log("yes, it logs the thing always");
            Debug.Log(-Mathf.Abs(tileOffset.x * tileSize.x));
            Debug.Log(__instance.RelativePosition);
            while (y < __instance.size.y)
            {
                if (gapmaster.Count > numsRunThrough)
                {
                    y += gapmaster[numsRunThrough]*currentTileScale;
                    Debug.Log($"Very cool info: {numsRunThrough}, {gapmaster[numsRunThrough]}, {tileSize.y}");
                    if (y >= __instance.size.y) { break; }
                }
                var x = -Mathf.Abs(tileOffset.x * tileSize.x);
                while (x < __instance.size.x)
                {
                    var yy = y + yOffset;
                    var baseIndex = verts.Count;

                    verts.Add(new Vector3(x, -yy));
                    verts.Add(new Vector3(x + tileSize.x, -yy));
                    verts.Add(new Vector3(x + tileSize.x, -yy + -tileSize.y));
                    verts.Add(new Vector3(x, -yy + -tileSize.y));

                    __instance.addQuadTriangles(triangles, baseIndex);
                    __instance.addQuadUV(uv, quadUV);
                    __instance.addQuadColors(colors);

                    x += tileSize.x;

                }

                y += tileSize.y;
                numsRunThrough++;
            }

            // Clip the quads *before* scaling, as it's easier to deal with untranslated
            // pixel coordinates
            // 
            __instance.clipQuads(verts, uv);

            var p2u = __instance.PixelsToUnits();
            var pivotOffset = __instance.pivot.TransformToUpperLeft(__instance.size);

            for (int i = 0; i < verts.Count; i++)
            {
                verts[i] = (verts[i] + pivotOffset) * p2u;
            }

            return false;
        }

        /*
		 * june's notes:
		 * this method only gets called if:
		 * projectileModule3 == gun.DefaultModule || (projectileModule3.IsDuctTapeModule && projectileModule3.ammoCost > 0);
		 * it'll only run for the primary module.
		 * 
		 * the translation from whole volley to module is going to be icky. a method to translate gun to stored bonus modules would be nice
		 * 
		 * oh, also, in general, going to need to figure out how to make it apply the cooldown when swapping out one volley for another.
		 * gun.attack would probably need to be hooked so that it applies the right module, but then there's also the concern of like gun.volley being in so much spots. it might break burst fire weapons.
		 * handleinitialgunshoot also applies cooldown woe. also it checks for module data too which is like a big wee-woo-wee-woo ruh roh. i might need to put it in an entirely alternative thing.
		 * shit girlies i might just need to find a way to put it in the gun's module data
		 * 
		 * in terms of actual rendering:
		 * going to need to override UpdateGunUI so that
		 * also ducttape is going to be a mess when it comes to overriding the entire thing, since it's on a per module basis
		 * 
		 * hell ductaping modules to behave differently is going to be hell on earth in general. maybe i could attach a component to a projectile inside the gun? there's no way that'd work though. 
		 * i guess take a gander at how hyper light blaster does it.
		 * */

        public static void CleanUp(GameUIAmmoController controller)
        {
            var hijacker = controller.gameObject.GetComponent<GameUIAmmoHijacker>();
            if (hijacker != null)
            {
                if (hijacker.AmmoSpritePairs.Count == 0)
                {

                    return;
                }
                for (int i = 0; i < hijacker.AmmoSpritePairs.Count; i++)
                {
                    var pair = hijacker.AmmoSpritePairs[i];
                    var back = pair.Background; var front = pair.Foreground;
                    /*if (i == 0)
                    {
                        if (back)
                        {
                            var spriteJacker = back.GetComponent<JunesCoolDfTiledSpriteHijacker>();
                            if (spriteJacker != null)
                                spriteJacker.currentTileGaps = new List<float>();
                            back.Invalidate();
                        }

                        if (front)
                        {
                            var spriteJacker = front.GetComponent<JunesCoolDfTiledSpriteHijacker>();
                            if (spriteJacker != null)
                                spriteJacker.currentTileGaps = new List<float>();
                            front.Invalidate();
                        }
                    } else*/
                    {
                        if (back)
                        {
                            UnityEngine.Object.Destroy(back.gameObject);
                        }
                        if (front)
                        {
                            UnityEngine.Object.Destroy(front.gameObject);
                        }
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameUIAmmoController), nameof(GameUIAmmoController.UpdateAmmoUIForModule))]
        public static bool UpdateAmmoUIForModule(ref dfTiledSprite currentAmmoFGSprite, ref dfTiledSprite currentAmmoBGSprite,
            List<dfTiledSprite> AddlModuleFGSprites, List<dfTiledSprite> AddlModuleBGSprites, dfSprite ModuleTopCap, dfSprite ModuleBottomCap,
            ProjectileModule module, Gun currentGun, ref GameUIAmmoType.AmmoType cachedAmmoTypeForModule, ref string cachedCustomAmmoTypeForModule,
            ref int cachedShotsInClip, bool didChangeGun, int numberRemaining, GameUIAmmoController __instance)
        {
            //init)
            var gunThingHolder = currentGun.GetComponent<GunClipModifierHolder>();

            if (gunThingHolder == null || !gunThingHolder.ShouldAddModifiers())
            {
                Debug.Log(ModuleTopCap.size.x / 2);
                if (currentAmmoFGSprite)
                {
                    Debug.Log(currentAmmoFGSprite.SpriteInfo.sizeInPixels.x / 2f - 10.5f);
                    currentAmmoFGSprite.gameObject.AddComponent<JunesCoolDfTiledSpriteHijacker>();
                    currentAmmoFGSprite.Invalidate();
                }
                CleanUp(__instance);
                return true;
            }

            var allModsForThisMod = gunThingHolder.GetModifier(module.runtimeGuid);
            if (allModsForThisMod == null)
            {
                //ETGModConsole.Log("running original, no module");
                CleanUp(__instance);
                return true;
            }/* else { ETGModConsole.Log("running original"); return true; }*/


            var modifiers = allModsForThisMod.PositionContainersSorted;

            if (modifiers.Count == 0)
            {
                CleanUp(__instance);
                return true;
            }

            var hijacker = __instance.gameObject.GetOrAddComponent<GameUIAmmoHijacker>();

            //Debug.Log("checkin ammo nums");
            //checking ammos
            int clipNum = (module.GetModNumberOfShotsInClip(currentGun.CurrentOwner) > 0) ? (module.GetModNumberOfShotsInClip(currentGun.CurrentOwner) - currentGun.RuntimeModuleData[module].numberShotsFired) : currentGun.ammo;
            if (clipNum > currentGun.ammo)
            {
                clipNum = currentGun.ammo;
            }
            int maxClipNum = (module.GetModNumberOfShotsInClip(currentGun.CurrentOwner) > 0) ? module.GetModNumberOfShotsInClip(currentGun.CurrentOwner) : currentGun.AdjustedMaxAmmo;
            if (currentGun.RequiresFundsToShoot)
            {
                clipNum = Mathf.FloorToInt((currentGun.CurrentOwner as PlayerController).carriedConsumables.Currency / (float)currentGun.CurrencyCostPerShot);
                maxClipNum = Mathf.FloorToInt((currentGun.CurrentOwner as PlayerController).carriedConsumables.Currency / (float)currentGun.CurrencyCostPerShot);
            }
            modifiers.Reverse();
            //Debug.Log("initializing the things");
            //initializing the things
            if (hijacker.AmmoSpritePairs == null || didChangeGun || module.ammoType != cachedAmmoTypeForModule || module.customAmmoType != cachedCustomAmmoTypeForModule
                || hijacker.AmmoSpritePairs.Count == 0 || hijacker.AmmoSpritePairs[0].Foreground == null)
            {
                //Debug.Log("is running stuff to make the new ones? lol?");
                //Debug.Log(hijacker.BackgroundForegroundAmmoSpritePairs.Count);
                __instance.m_additionalAmmoTypeDefinitions.Clear();
                for (int i = 0; i < hijacker.AmmoSpritePairs.Count; i++)
                {
                    var tuple = hijacker.AmmoSpritePairs[i];
                    if (tuple.Background)
                    {
                        UnityEngine.Object.Destroy(tuple.Background.gameObject);
                    }
                    if (tuple.Foreground)
                    {
                        UnityEngine.Object.Destroy(tuple.Foreground.gameObject);
                    }
                }
                //legacy
                if (currentAmmoFGSprite != null)
                {
                    UnityEngine.Object.Destroy(currentAmmoFGSprite.gameObject);
                }
                if (currentAmmoBGSprite != null)
                {
                    UnityEngine.Object.Destroy(currentAmmoBGSprite.gameObject);
                }
                hijacker.AmmoSpritePairs.Clear();
                for (int i = 0; i < AddlModuleBGSprites.Count; i++)
                {
                    UnityEngine.Object.Destroy(AddlModuleBGSprites[i].gameObject);
                    UnityEngine.Object.Destroy(AddlModuleFGSprites[i].gameObject);
                }
                AddlModuleBGSprites.Clear();
                AddlModuleFGSprites.Clear();

                //regular projectiles
                GameUIAmmoType uiammoType = __instance.GetUIAmmoType(module.ammoType, module.customAmmoType);
                GameObject gameObject = UnityEngine.Object.Instantiate(uiammoType.ammoBarFG.gameObject);
                GameObject gameObject2 = UnityEngine.Object.Instantiate(uiammoType.ammoBarBG.gameObject);
                gameObject.transform.parent = __instance.GunBoxSprite.transform.parent;
                gameObject2.transform.parent = __instance.GunBoxSprite.transform.parent;
                gameObject.name = uiammoType.ammoBarFG.name;
                gameObject2.name = uiammoType.ammoBarBG.name;
                currentAmmoFGSprite = gameObject.GetComponent<dfTiledSprite>();
                currentAmmoBGSprite = gameObject2.GetComponent<dfTiledSprite>();
                __instance.m_panel.AddControl(currentAmmoFGSprite);
                __instance.m_panel.AddControl(currentAmmoBGSprite);
                currentAmmoFGSprite.EnableBlackLineFix = (module.shootStyle == ProjectileModule.ShootStyle.Beam);
                currentAmmoBGSprite.EnableBlackLineFix = currentAmmoFGSprite.EnableBlackLineFix;
                //Debug.Log("it actually sets the thing here");
                hijacker.AmmoSpritePairs.Add(new TileThingHolder(currentAmmoFGSprite, currentAmmoBGSprite, 0));

                //final projectiles
                // june's notes: it might be worth to just like override this entirely?
                // gritty and dark future june's notes: probably not
                if (module.usesOptionalFinalProjectile)
                {
                    GameUIAmmoType uiammoType2 = __instance.GetUIAmmoType(module.finalAmmoType, module.finalCustomAmmoType);
                    __instance.m_additionalAmmoTypeDefinitions.Add(uiammoType2);
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(uiammoType2.ammoBarFG.gameObject);
                    gameObject2 = UnityEngine.Object.Instantiate<GameObject>(uiammoType2.ammoBarBG.gameObject);
                    gameObject.transform.parent = __instance.GunBoxSprite.transform.parent;
                    gameObject2.transform.parent = __instance.GunBoxSprite.transform.parent;
                    gameObject.name = uiammoType2.ammoBarFG.name;
                    gameObject2.name = uiammoType2.ammoBarBG.name;
                    AddlModuleFGSprites.Add(gameObject.GetComponent<dfTiledSprite>());
                    AddlModuleBGSprites.Add(gameObject2.GetComponent<dfTiledSprite>());
                    __instance.m_panel.AddControl(AddlModuleFGSprites[0]);
                    __instance.m_panel.AddControl(AddlModuleBGSprites[0]);
                    //Debug.Log("like skibidi toilet god damnit!");
                }
                Debug.Log("June: Finished vanilla ui object creation");

                //THE NEW STUFF
                for (int i = 0; i < modifiers.Count; i++)
                {
                    var runtimePos = modifiers[i];
                    var iconProjectile = runtimePos.Modifier.OverrideIcon ?? runtimePos.InsertedDatas.FirstOrDefault().RuntimeVolley.projectiles[0];

                    GameUIAmmoType uiammoType2 = __instance.GetUIAmmoType(iconProjectile.ammoType, iconProjectile.customAmmoType);
                    __instance.m_additionalAmmoTypeDefinitions.Add(uiammoType2);
                    gameObject = UnityEngine.Object.Instantiate<GameObject>(uiammoType2.ammoBarFG.gameObject);
                    gameObject2 = UnityEngine.Object.Instantiate<GameObject>(uiammoType2.ammoBarBG.gameObject);
                    gameObject.transform.parent = __instance.GunBoxSprite.transform.parent;
                    gameObject2.transform.parent = __instance.GunBoxSprite.transform.parent;
                    gameObject.name = uiammoType2.ammoBarFG.name;
                    gameObject2.name = uiammoType2.ammoBarBG.name;
                    var tile1 = gameObject.GetComponent<dfTiledSprite>();
                    var tile2 = gameObject2.GetComponent<dfTiledSprite>();
                    //Debug.Log("DOES THE NEW ONE BREAK IT?");
                    hijacker.AmmoSpritePairs.Add(new TileThingHolder(tile1, tile2, i+1));
                    __instance.m_panel.AddControl(tile1);
                    __instance.m_panel.AddControl(tile2);
                }
                Debug.Log("June: Finished modded ui object creation");
            }
            float currentTileScale = Pixelator.Instance.CurrentTileScale;

            int finalClipNum = (!module.usesOptionalFinalProjectile) ? 0 : module.GetModifiedNumberOfFinalProjectiles(currentGun.CurrentOwner);
            int onlyMainClip = Mathf.Max(0, clipNum - finalClipNum);
            //here is where i think i should calculate the entire size of the thing
            var positions = allModsForThisMod.CurrentFireResults;//hijacker.GetAllTypesToRender(modifiers, maxClipNum, finalClipNum);
            var gaps = new List<float>();
            float sum = 0;
            /*Debug.Log(cachedShotsInClip);
            Debug.Log(clipNum);
            Debug.Log(positions.Count);*/
            for (int i = 0; i < positions.Count; i++)
            {
                var idx = positions[i];
                /*//Debug.Log(idx);
                //Debug.Log(hijacker.BackgroundForegroundAmmoSpritePairs.Count);*/
                var sprite = hijacker.AmmoSpritePairs[idx.TypeIdx];

                var height = sprite.SpriteHeight;
                gaps.Add(height);
                sum += height;
            }
            Debug.Log("June: added heights");

            bool resetSizes = cachedShotsInClip != clipNum;
            for (int i = 0; i < hijacker.AmmoSpritePairs.Count; i++)
            {
                var handler = hijacker.AmmoSpritePairs[i];
                int limit = 125; bool isMainModule = i == 0;
                var specModule = isMainModule ? module : modifiers[i - 1].InsertedDatas.FirstOrDefault().RuntimeVolley.projectiles[0];

                int onlyMainMaxClip = maxClipNum - finalClipNum;

                if (specModule.shootStyle == ProjectileModule.ShootStyle.Beam)
                {
                    limit = 500;
                    onlyMainMaxClip = Mathf.CeilToInt(onlyMainMaxClip / 2f);
                    //onlyMainClip = Mathf.CeilToInt(onlyMainClip / 2f);
                }
                onlyMainMaxClip = Mathf.Min(limit, onlyMainMaxClip);
                //onlyMainClip = Mathf.Min(limit, onlyMainClip);
                var size = new Vector2(handler.BackWidth * currentTileScale, sum * currentTileScale);
                handler.Background.Size = size;
                handler.Foreground.Size = size;

                handler.SetGaps(gaps, positions, maxClipNum- clipNum);
                if (resetSizes)
                {
                    handler.Foreground.Invalidate();
                }
            }
            Debug.Log("June: new stuff finished?");
            int finalProjBackground = Mathf.Min(finalClipNum, clipNum);
            if (module.shootStyle == ProjectileModule.ShootStyle.Beam)
            {
                finalClipNum = Mathf.CeilToInt(finalClipNum / 2f);
                finalProjBackground = Mathf.CeilToInt(finalProjBackground / 2f);
            }
            for (int j = 0; j < AddlModuleBGSprites.Count; j++)
            {
                AddlModuleBGSprites[j].Size = new Vector2(AddlModuleBGSprites[j].SpriteInfo.sizeInPixels.x * currentTileScale, AddlModuleBGSprites[j].SpriteInfo.sizeInPixels.y * currentTileScale * finalClipNum);
                AddlModuleFGSprites[j].Size = new Vector2(AddlModuleFGSprites[j].SpriteInfo.sizeInPixels.x * currentTileScale, AddlModuleFGSprites[j].SpriteInfo.sizeInPixels.y * currentTileScale * finalProjBackground);
            }
            if (!didChangeGun && __instance.AmmoBurstVFX != null && cachedShotsInClip > clipNum && !currentGun.IsReloading)
            {
                int clipDifference = cachedShotsInClip - clipNum;
                for (int k = 0; k < clipDifference; k++)
                {
                    GameObject gameObject3 = UnityEngine.Object.Instantiate<GameObject>(__instance.AmmoBurstVFX.gameObject);
                    dfSprite component = gameObject3.GetComponent<dfSprite>();
                    dfSpriteAnimation component2 = gameObject3.GetComponent<dfSpriteAnimation>();
                    component.ZOrder = currentAmmoFGSprite.ZOrder + 1;
                    float num8 = component.Size.y / 2f;
                    currentAmmoFGSprite.AddControl(component);
                    component.transform.position = currentAmmoFGSprite.GetCenter();
                    component.RelativePosition = component.RelativePosition.WithY(k * currentAmmoFGSprite.SpriteInfo.sizeInPixels.y * currentTileScale - num8);
                    if (onlyMainClip == 0 && finalClipNum > 0)
                    {
                        component.RelativePosition += new Vector3(0f, AddlModuleFGSprites[0].SpriteInfo.sizeInPixels.y * currentTileScale * Mathf.Max(0f, finalClipNum - finalProjBackground - 0.5f), 0f);
                    }
                    component2.Play();
                }
            }
            float num9 = currentTileScale * numberRemaining * -10f;
            float num10 = -Pixelator.Instance.CurrentTileScale + num9;
            float zero = 0f;
            float num12 = (AddlModuleBGSprites.Count <= 0) ? 0f : AddlModuleBGSprites[0].Size.y;
            if (__instance.IsLeftAligned)
            {
                ModuleBottomCap.RelativePosition = __instance.m_panel.Size.WithX(0f).ToVector3ZUp(0f) - ModuleBottomCap.Size.WithX(0f).ToVector3ZUp(0f) + new Vector3(-num10, zero, 0f);
            }
            else
            {
                ModuleBottomCap.RelativePosition = __instance.m_panel.Size.ToVector3ZUp(0f) - ModuleBottomCap.Size.ToVector3ZUp(0f) + new Vector3(num10, -zero, 0f);
            }
            ModuleTopCap.RelativePosition = ModuleBottomCap.RelativePosition + new Vector3(0f, -currentAmmoBGSprite.Size.y + -num12 + -ModuleTopCap.Size.y, 0f);
            float num13 = ModuleTopCap.Size.x / 2f;
            if (hijacker.AmmoSpritePairs.Count > 0)
            {
                for (int i = 0; i < hijacker.AmmoSpritePairs.Count; i++)
                {
                    var pair = hijacker.AmmoSpritePairs[i];
                    float frontWidth = pair.Foreground.size.x / 2f - num13;
                    float backWidth = BraveMathCollege.QuantizeFloat(pair.Background.size.x / 2f - num13, currentTileScale);
                    /*Debug.Log("hey");
                    Debug.Log(frontWidth);*/
                    /*
		float num13 = ModuleTopCap.Size.x / 2f;
		float num14 = BraveMathCollege.QuantizeFloat(currentAmmoBGSprite.Size.x / 2f - num13, currentTileScale);
		float num15 = currentAmmoFGSprite.Size.x / 2f - num13;
		currentAmmoBGSprite.RelativePosition = ModuleTopCap.RelativePosition + new Vector3(-num14, ModuleTopCap.Size.y, 0f);
		currentAmmoFGSprite.RelativePosition = ModuleTopCap.RelativePosition + new Vector3(-num15, ModuleTopCap.Size.y + currentAmmoFGSprite.SpriteInfo.sizeInPixels.y * (float)(num4 - num5) * currentTileScale, 0f);*/
                    pair.Background.RelativePosition = ModuleTopCap.RelativePosition + new Vector3(-backWidth, ModuleTopCap.Size.y, 0f);
                    pair.Foreground.RelativePosition = ModuleTopCap.RelativePosition + new Vector3(-frontWidth, ModuleTopCap.Size.y, 0f);
                }
            }
            currentAmmoFGSprite.ZOrder = currentAmmoBGSprite.ZOrder + 1;
            if (AddlModuleBGSprites.Count > 0)
            {
                float num14 = BraveMathCollege.QuantizeFloat(AddlModuleBGSprites[0].Size.x / 2f - num13, currentTileScale);
                AddlModuleBGSprites[0].RelativePosition = ModuleTopCap.RelativePosition + new Vector3(-num14, ModuleTopCap.Size.y + currentAmmoBGSprite.Size.y, 0f);
                float num15 = AddlModuleFGSprites[0].Size.x / 2f - num13;
                AddlModuleFGSprites[0].RelativePosition = ModuleTopCap.RelativePosition + new Vector3(-num15, ModuleTopCap.Size.y + currentAmmoBGSprite.Size.y + AddlModuleFGSprites[0].SpriteInfo.sizeInPixels.y * (finalClipNum - finalProjBackground) * currentTileScale, 0f);
            }
            cachedAmmoTypeForModule = module.ammoType;
            cachedCustomAmmoTypeForModule = module.customAmmoType;
            cachedShotsInClip = clipNum;

            return false;
        }
        public class TileThingHolder
        {
            public TileThingHolder(dfTiledSprite foreground, dfTiledSprite background, int idx)
            {
                //Debug.Log("WEEWOO, new tile thing holder is being initialized");
                Foreground = foreground;
                Background = background;
                Foreground.gameObject.AddComponent<JunesCoolDfTiledSpriteHijacker>();
                Background.gameObject.AddComponent<JunesCoolDfTiledSpriteHijacker>();

                Index = idx;

                SpriteHeight = background.SpriteInfo.sizeInPixels.y;
                BackWidth = background.SpriteInfo.sizeInPixels.x;
                ForeWidth = foreground.SpriteInfo.sizeInPixels.x;
                if (idx == 1)
                {
                    Debug.Log(foreground.name);
                    Debug.Log(background.name);
                    Debug.Log(SpriteHeight);
                }
                //Debug.Log("finished");
            }

            public dfTiledSprite Foreground;
            public dfTiledSprite Background;

            public float SpriteHeight;
            public float BackWidth;
            public float ForeWidth;


            public void SetGaps(List<float> gaps, List<ModuleInsertData> types, int numFired)
            {
                for (int j = 0; j < 2; j++)
                {
                    bool isForegound = j == 0;
                    List<float> temp = new List<float>();
                    float gapToAdd = 0;

                    for (int i = 0; i < gaps.Count; i++)
                    {
                        var idx = types[i]; var gap = gaps[i];
                        bool LengthenGap = true;
                        if (idx.TypeIdx == Index)
                        {
                            if (i >= numFired || !isForegound)
                            {
                                LengthenGap = false;
                                temp.Add(gapToAdd);
                                gapToAdd = 0;
                            }
                        }
                        if (LengthenGap)
                        {
                            gapToAdd += gap;
                        }
                    }
                    if (gapToAdd > 0)
                    {
                        temp.Add(gapToAdd);
                    }
                    ////Debug.Log("Something errorsome this way comes...");

                    ////Debug.Log(i);
                    var obj = isForegound ? Foreground : Background;
                    var hijacker = obj.gameObject.GetOrAddComponent<JunesCoolDfTiledSpriteHijacker>();

                    hijacker.currentTileGaps = temp;
                }
            }

            public int Index;
        }


        public class GameUIAmmoHijacker : MonoBehaviour
        {
            public List<TileThingHolder> AmmoSpritePairs = new List<TileThingHolder>();

            public float GetPosForClipPos()
            {
                return 0f;
            }

            /*public List<int> GetAllTypesToRender(List<GunClipModifiers.RuntimeModifierContainer> modifiers, int maxClipNum, int maxFinalShotNum)
            {
                //Debug.Log("beginning all types to render");
                //Debug.Log(modifiers.Count);
                List<int> result = new List<int>();
                for (int i = 0; i < maxClipNum; i++)
                {
                    result.Add(0);
                }
                for (int i = 0;i < maxFinalShotNum; i++)
                {
                    result.Add(-1); //THIS IS THE WORST IDEA, DONT DO THIS
                }

                for (int i = 0; i < modifiers.Count; i++)
                {
                    var mod = modifiers[i];
                    foreach (var pos in mod.InsertedDatas)
                    {
                        //Debug.Log(pos.Position);
                        result.Insert(pos.Position, i + 1);
                    }
                }

                return result;
            }*/
        }

        public class JunesCoolDfTiledSpriteHijacker : MonoBehaviour
        {
            public List<float> currentTileGaps = new List<float>();
        }
    }
}
