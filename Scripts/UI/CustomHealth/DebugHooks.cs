using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HarmonyLib;
using Alexandria.Misc;

/*namespace JuneLib.UI.CustomHealth
{
    [HarmonyPatch]
    [HarmonyPatch]
    internal class DebugHooks
    {
        [HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.ManualDeathHandling), MethodType.Getter)]
        [HarmonyPrefix]
        public static void LogTestHuh()
        {
            //ETGModConsole.Log("Now aint that wacky");
        }

		//putting this here fixes the player death
		//because, sure.
		[HarmonyPatch(typeof(HealthHaver), nameof(HealthHaver.Die))]
		[HarmonyPrefix]
		public static bool D(Vector2 finalDamageDirection, HealthHaver __instance)
		{
			JuneHealthHaver juneHealthHaver = __instance.GetComponent<JuneHealthHaver>();
			if (!juneHealthHaver)
            {
				return true;
            }
				__instance.EndFlashEffects();
			bool flag = false;
			if (__instance.spawnBulletScript && (!__instance.gameActor || !__instance.gameActor.IsFalling) && (__instance.chanceToSpawnBulletScript >= 1f || UnityEngine.Random.value < __instance.chanceToSpawnBulletScript))
			{
				flag = true;
				if (__instance.noCorpseWhenBulletScriptDeath)
				{
					__instance.aiActor.CorpseObject = null;
				}
				if (__instance.bulletScriptType == HealthHaver.BulletScriptType.OnAnimEvent)
				{
					tk2dSpriteAnimator spriteAnimator = __instance.spriteAnimator;
					spriteAnimator.AnimationEventTriggered = (Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int>)Delegate.Combine(spriteAnimator.AnimationEventTriggered, new Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int>(__instance.BulletScriptEventTriggered));
				}
			}
			Action<Vector2> preDeath = ReflectionUtility.ReflectGetField<Action<Vector2>>(typeof(HealthHaver), "OnPreDeath", __instance);
			if (preDeath != null)
			{
				preDeath(finalDamageDirection);
			}
			//ETGModConsole.Log("a");
			if (flag && __instance.bulletScriptType == HealthHaver.BulletScriptType.OnPreDeath)
			{
				SpawnManager.SpawnBulletScript(__instance.aiActor, __instance.bulletScript, null, null, false, null);
			}
			//ETGModConsole.Log("a");
			if (__instance.GetCurrentHealth() > 0f || __instance.Armor > 0f)
			{
				goto end;
			}
			//ETGModConsole.Log("a");
			__instance.IsVulnerable = false;
			if (__instance.deathEffect != null)
			{
				SpawnManager.SpawnVFX(__instance.deathEffect, __instance.transform.position, Quaternion.identity);
			}
			//ETGModConsole.Log("a");
			if (__instance.IsBoss)
			{
				__instance.EndBossState(true);
			}
			//ETGModConsole.Log("a");
			if (__instance.ManualDeathHandling)
			{
				goto end;
			}
			//ETGModConsole.Log("a");
			if (__instance.spriteAnimator != null)
			{
				string text = (!flag || string.IsNullOrEmpty(__instance.overrideDeathAnimBulletScript)) ? __instance.overrideDeathAnimation : __instance.overrideDeathAnimBulletScript;
				if (!string.IsNullOrEmpty(text))
				{
					tk2dSpriteAnimationClip tk2dSpriteAnimationClip;
					if (__instance.aiAnimator != null)
					{
						__instance.aiAnimator.PlayUntilCancelled(text, false, null, -1f, false);
						tk2dSpriteAnimationClip = __instance.spriteAnimator.CurrentClip;
					}
					else
					{
						tk2dSpriteAnimationClip = __instance.spriteAnimator.GetClipByName(__instance.overrideDeathAnimation);
						if (tk2dSpriteAnimationClip != null)
						{
							__instance.spriteAnimator.Play(tk2dSpriteAnimationClip);
						}
					}
					if (tk2dSpriteAnimationClip != null && !__instance.isPlayerCharacter && (!__instance.gameActor || !__instance.gameActor.IsFalling))
					{
						tk2dSpriteAnimator spriteAnimator2 = __instance.spriteAnimator;
						spriteAnimator2.AnimationEventTriggered = (Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int>)Delegate.Combine(spriteAnimator2.AnimationEventTriggered, new Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int>(__instance.DeathEventTriggered));
						tk2dSpriteAnimator spriteAnimator3 = __instance.spriteAnimator;
						spriteAnimator3.AnimationCompleted = (Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip>)Delegate.Combine(spriteAnimator3.AnimationCompleted, new Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip>(__instance.DeathAnimationComplete));
						goto end;
					}
				}
				else
				{
					if (__instance.aiAnimator != null)
					{
						__instance.aiAnimator.enabled = false;
					}
					float num = finalDamageDirection.ToAngle();
					tk2dSpriteAnimationClip tk2dSpriteAnimationClip2;
					if (__instance.aiAnimator != null && __instance.aiAnimator.HasDirectionalAnimation("death"))
					{
						if (!__instance.aiAnimator.LockFacingDirection)
						{
							__instance.aiAnimator.LockFacingDirection = true;
							__instance.aiAnimator.FacingDirection = (num + 180f) % 360f;
						}
						__instance.aiAnimator.PlayUntilCancelled("death", false, null, -1f, false);
						tk2dSpriteAnimationClip2 = __instance.spriteAnimator.CurrentClip;
					}
					else if (__instance.gameActor && __instance.gameActor is PlayerSpaceshipController)
					{
						Exploder.DoDefaultExplosion(__instance.gameActor.CenterPosition, Vector2.zero, null, false, CoreDamageTypes.None, false);
						tk2dSpriteAnimationClip2 = null;
					}
					else
					{
						tk2dSpriteAnimationClip2 = __instance.GetDeathClip(BraveMathCollege.ClampAngle360(num + 22.5f));
						if (tk2dSpriteAnimationClip2 != null)
						{
							__instance.spriteAnimator.Play(tk2dSpriteAnimationClip2);
						}
					}
					if (tk2dSpriteAnimationClip2 != null && !__instance.isPlayerCharacter && (!__instance.gameActor || !__instance.gameActor.IsFalling))
					{
						tk2dSpriteAnimator spriteAnimator4 = __instance.spriteAnimator;
						spriteAnimator4.AnimationEventTriggered = (Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int>)Delegate.Combine(spriteAnimator4.AnimationEventTriggered, new Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int>(__instance.DeathEventTriggered));
						tk2dSpriteAnimator spriteAnimator5 = __instance.spriteAnimator;
						spriteAnimator5.AnimationCompleted = (Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip>)Delegate.Combine(spriteAnimator5.AnimationCompleted, new Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip>(__instance.DeathAnimationComplete));
						goto end;
					}
				}
			}
			//ETGModConsole.Log("a");
			if (__instance.spawnBulletScript && __instance.bulletScriptType == HealthHaver.BulletScriptType.OnDeath && (!__instance.gameActor || !__instance.gameActor.IsFalling))
			{
				SpawnManager.SpawnBulletScript(__instance.aiActor, __instance.bulletScript, null, null, false, null);
			}
			__instance.FinalizeDeath();
			//ETGModConsole.Log("a");
		end:
			return false;
		}
	}
}
*/