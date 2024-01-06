using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
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
			Debug.Log("Successfully running");
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
			while (y < __instance.size.y)
			{

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
			}

			// Clip the quads *before* scaling, as it's easier to deal with untranslated
			// pixel coordinates
			//__instance.clipQuads(verts, uv);

			var p2u = __instance.PixelsToUnits();
			var pivotOffset = __instance.pivot.TransformToUpperLeft(__instance.size);

			for (int i = 0; i < verts.Count; i++)
			{
				verts[i] = (verts[i] + (Vector3)pivotOffset) * p2u;
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
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameUIAmmoController), nameof(GameUIAmmoController.UpdateAmmoUIForModule))]
		public static bool UpdateAmmoUIForModule(ref dfTiledSprite currentAmmoFGSprite, ref dfTiledSprite currentAmmoBGSprite, 
			List<dfTiledSprite> AddlModuleFGSprites, List<dfTiledSprite> AddlModuleBGSprites, dfSprite ModuleTopCap, dfSprite ModuleBottomCap, 
			ProjectileModule module, Gun currentGun, ref GameUIAmmoType.AmmoType cachedAmmoTypeForModule, ref string cachedCustomAmmoTypeForModule, 
			ref int cachedShotsInClip, bool didChangeGun, int numberRemaining, GameUIAmmoController __instance)
		{
			//init
			var hijacker = !__instance.gameObject.GetOrAddComponent<GameUIAmmoHijacker>();

			//checking ammos
			int num = (module.GetModNumberOfShotsInClip(currentGun.CurrentOwner) > 0) ? (module.GetModNumberOfShotsInClip(currentGun.CurrentOwner) - currentGun.RuntimeModuleData[module].numberShotsFired) : currentGun.ammo;
			if (num > currentGun.ammo)
			{
				num = currentGun.ammo;
			}
			int num2 = (module.GetModNumberOfShotsInClip(currentGun.CurrentOwner) > 0) ? module.GetModNumberOfShotsInClip(currentGun.CurrentOwner) : currentGun.AdjustedMaxAmmo;
			if (currentGun.RequiresFundsToShoot)
			{
				num = Mathf.FloorToInt((float)(currentGun.CurrentOwner as PlayerController).carriedConsumables.Currency / (float)currentGun.CurrencyCostPerShot);
				num2 = Mathf.FloorToInt((float)(currentGun.CurrentOwner as PlayerController).carriedConsumables.Currency / (float)currentGun.CurrencyCostPerShot);
			}

			//initializing the things
			if (currentAmmoFGSprite == null || didChangeGun || module.ammoType != cachedAmmoTypeForModule || module.customAmmoType != cachedCustomAmmoTypeForModule)
			{
				__instance.m_additionalAmmoTypeDefinitions.Clear();
				if (currentAmmoFGSprite != null)
				{
					UnityEngine.Object.Destroy(currentAmmoFGSprite.gameObject);
				}
				if (currentAmmoBGSprite != null)
				{
					UnityEngine.Object.Destroy(currentAmmoBGSprite.gameObject);
				}
				for (int i = 0; i < AddlModuleBGSprites.Count; i++)
				{
					UnityEngine.Object.Destroy(AddlModuleBGSprites[i].gameObject);
					UnityEngine.Object.Destroy(AddlModuleFGSprites[i].gameObject);
				}
				AddlModuleBGSprites.Clear();
				AddlModuleFGSprites.Clear();

				//regular projectiles
				GameUIAmmoType uiammoType = __instance.GetUIAmmoType(module.ammoType, module.customAmmoType);
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(uiammoType.ammoBarFG.gameObject);
				GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(uiammoType.ammoBarBG.gameObject);
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

				//final projectiles
				// june's notes: it might be worth to just like override this entirely?
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
				}
			}
			float currentTileScale = Pixelator.Instance.CurrentTileScale;
			int num3 = (!module.usesOptionalFinalProjectile) ? 0 : module.GetModifiedNumberOfFinalProjectiles(currentGun.CurrentOwner);
			int num4 = num2 - num3;
			int num5 = Mathf.Max(0, num - num3);
			int num6 = Mathf.Min(num3, num);
			int a = 125;
			if (module.shootStyle == ProjectileModule.ShootStyle.Beam)
			{
				a = 500;
				num3 = Mathf.CeilToInt((float)num3 / 2f);
				num4 = Mathf.CeilToInt((float)num4 / 2f);
				num5 = Mathf.CeilToInt((float)num5 / 2f);
				num6 = Mathf.CeilToInt((float)num6 / 2f);
			}
			num4 = Mathf.Min(a, num4);
			num5 = Mathf.Min(a, num5);
			currentAmmoBGSprite.Size = new Vector2(currentAmmoBGSprite.SpriteInfo.sizeInPixels.x * currentTileScale, currentAmmoBGSprite.SpriteInfo.sizeInPixels.y * currentTileScale * (float)num4);
			currentAmmoFGSprite.Size = new Vector2(currentAmmoFGSprite.SpriteInfo.sizeInPixels.x * currentTileScale, currentAmmoFGSprite.SpriteInfo.sizeInPixels.y * currentTileScale * (float)num5);
			for (int j = 0; j < AddlModuleBGSprites.Count; j++)
			{
				AddlModuleBGSprites[j].Size = new Vector2(AddlModuleBGSprites[j].SpriteInfo.sizeInPixels.x * currentTileScale, AddlModuleBGSprites[j].SpriteInfo.sizeInPixels.y * currentTileScale * (float)num3);
				AddlModuleFGSprites[j].Size = new Vector2(AddlModuleFGSprites[j].SpriteInfo.sizeInPixels.x * currentTileScale, AddlModuleFGSprites[j].SpriteInfo.sizeInPixels.y * currentTileScale * (float)num6);
			}
			if (!didChangeGun && __instance.AmmoBurstVFX != null && cachedShotsInClip > num && !currentGun.IsReloading)
			{
				int num7 = cachedShotsInClip - num;
				for (int k = 0; k < num7; k++)
				{
					GameObject gameObject3 = UnityEngine.Object.Instantiate<GameObject>(__instance.AmmoBurstVFX.gameObject);
					dfSprite component = gameObject3.GetComponent<dfSprite>();
					dfSpriteAnimation component2 = gameObject3.GetComponent<dfSpriteAnimation>();
					component.ZOrder = currentAmmoFGSprite.ZOrder + 1;
					float num8 = component.Size.y / 2f;
					currentAmmoFGSprite.AddControl(component);
					component.transform.position = currentAmmoFGSprite.GetCenter();
					component.RelativePosition = component.RelativePosition.WithY((float)k * currentAmmoFGSprite.SpriteInfo.sizeInPixels.y * currentTileScale - num8);
					if (num5 == 0 && num3 > 0)
					{
						component.RelativePosition += new Vector3(0f, AddlModuleFGSprites[0].SpriteInfo.sizeInPixels.y * currentTileScale * Mathf.Max(0f, (float)(num3 - num6) - 0.5f), 0f);
					}
					component2.Play();
				}
			}
			float num9 = currentTileScale * (float)numberRemaining * -10f;
			float num10 = -Pixelator.Instance.CurrentTileScale + num9;
			float num11 = 0f;
			float num12 = (AddlModuleBGSprites.Count <= 0) ? 0f : AddlModuleBGSprites[0].Size.y;
			if (__instance.IsLeftAligned)
			{
				ModuleBottomCap.RelativePosition = __instance.m_panel.Size.WithX(0f).ToVector3ZUp(0f) - ModuleBottomCap.Size.WithX(0f).ToVector3ZUp(0f) + new Vector3(-num10, num11, 0f);
			}
			else
			{
				ModuleBottomCap.RelativePosition = __instance.m_panel.Size.ToVector3ZUp(0f) - ModuleBottomCap.Size.ToVector3ZUp(0f) + new Vector3(num10, -num11, 0f);
			}
			ModuleTopCap.RelativePosition = ModuleBottomCap.RelativePosition + new Vector3(0f, -currentAmmoBGSprite.Size.y + -num12 + -ModuleTopCap.Size.y, 0f);
			float num13 = ModuleTopCap.Size.x / 2f;
			float num14 = BraveMathCollege.QuantizeFloat(currentAmmoBGSprite.Size.x / 2f - num13, currentTileScale);
			float num15 = currentAmmoFGSprite.Size.x / 2f - num13;
			currentAmmoBGSprite.RelativePosition = ModuleTopCap.RelativePosition + new Vector3(-num14, ModuleTopCap.Size.y, 0f);
			currentAmmoFGSprite.RelativePosition = ModuleTopCap.RelativePosition + new Vector3(-num15, ModuleTopCap.Size.y + currentAmmoFGSprite.SpriteInfo.sizeInPixels.y * (float)(num4 - num5) * currentTileScale, 0f);
			currentAmmoFGSprite.ZOrder = currentAmmoBGSprite.ZOrder + 1;
			if (AddlModuleBGSprites.Count > 0)
			{
				num14 = BraveMathCollege.QuantizeFloat(AddlModuleBGSprites[0].Size.x / 2f - num13, currentTileScale);
				AddlModuleBGSprites[0].RelativePosition = ModuleTopCap.RelativePosition + new Vector3(-num14, ModuleTopCap.Size.y + currentAmmoBGSprite.Size.y, 0f);
				num15 = AddlModuleFGSprites[0].Size.x / 2f - num13;
				AddlModuleFGSprites[0].RelativePosition = ModuleTopCap.RelativePosition + new Vector3(-num15, ModuleTopCap.Size.y + currentAmmoBGSprite.Size.y + AddlModuleFGSprites[0].SpriteInfo.sizeInPixels.y * (float)(num3 - num6) * currentTileScale, 0f);
			}
			cachedAmmoTypeForModule = module.ammoType;
			cachedCustomAmmoTypeForModule = module.customAmmoType;
			cachedShotsInClip = num;

			return false;
		}

		public class GameUIAmmoHijacker : MonoBehaviour
        {

        }

		public class JunesCoolDfTiledSpriteHijacker : MonoBehaviour
        {
			public KeyValuePair<float, float> currentTileGaps = new KeyValuePair<float, float>();
        }
	}
}
