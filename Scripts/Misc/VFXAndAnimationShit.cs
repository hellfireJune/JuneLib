using Alexandria.ItemAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace JuneLib
{
    public static class VFXAndAnimationShit
    {
        //Taken from OMITB, thankye nn
        public static GameObject CreateOverheadVFX(List<string> filepaths, string name, int fps, Assembly assembly = null)
        {
            if (assembly == null) { assembly = Assembly.GetCallingAssembly(); }
            //Setting up the Overhead Plague VFX
            GameObject overheadderVFX = SpriteBuilder.SpriteFromResource(filepaths[0], new GameObject(name), assembly);
            overheadderVFX.SetActive(false);
            tk2dBaseSprite plaguevfxSprite = overheadderVFX.GetComponent<tk2dBaseSprite>();
            plaguevfxSprite.GetCurrentSpriteDef().ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter, plaguevfxSprite.GetCurrentSpriteDef().position3);
            FakePrefab.MarkAsFakePrefab(overheadderVFX);
            UnityEngine.Object.DontDestroyOnLoad(overheadderVFX);

            //Animating the overhead
            tk2dSpriteAnimator plagueanimator = overheadderVFX.AddComponent<tk2dSpriteAnimator>();
            plagueanimator.Library = overheadderVFX.AddComponent<tk2dSpriteAnimation>();
            plagueanimator.Library.clips = new tk2dSpriteAnimationClip[0];

            tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip { name = "NewOverheadVFX", fps = fps, frames = new tk2dSpriteAnimationFrame[0] };
            foreach (string path in filepaths)
            {
                int spriteId = SpriteBuilder.AddSpriteToCollection(path, overheadderVFX.GetComponent<tk2dBaseSprite>().Collection, assembly);

                overheadderVFX.GetComponent<tk2dBaseSprite>().Collection.spriteDefinitions[spriteId].ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.LowerCenter);

                tk2dSpriteAnimationFrame frame = new tk2dSpriteAnimationFrame { spriteId = spriteId, spriteCollection = overheadderVFX.GetComponent<tk2dBaseSprite>().Collection };
                clip.frames = clip.frames.Concat(new tk2dSpriteAnimationFrame[] { frame }).ToArray();
            }
            plagueanimator.Library.clips = plagueanimator.Library.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            plagueanimator.playAutomatically = true;
            plagueanimator.DefaultClipId = plagueanimator.GetClipIdByName("NewOverheadVFX");
            return overheadderVFX;
        }
    }
}
