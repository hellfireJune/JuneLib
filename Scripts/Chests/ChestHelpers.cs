using Dungeonator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JuneLib.Chests
{
    public static class ChestHelpers
    {
        public static Chest SpawnGenerationChestAt(this RewardManager manager, GameObject chestPrefab, RoomHandler targetRoom, IntVector2 positionInRoom, float overrideMimicChance = 0f, Chest.GeneralChestType generalChestType = Chest.GeneralChestType.UNSPECIFIED)
		{
			System.Random random = (!GameManager.Instance.IsSeeded) ? null : BraveRandom.GeneratorRandom;
			FloorRewardData rewardDataForFloor = manager.CurrentRewardData;
			if (generalChestType == Chest.GeneralChestType.UNSPECIFIED)
            {
				generalChestType = (BraveRandom.GenerationRandomValue() >= rewardDataForFloor.GunVersusItemPercentChance) ? Chest.GeneralChestType.ITEM : Chest.GeneralChestType.WEAPON;
				if (StaticReferenceManager.ItemChestsSpawnedOnFloor > 0 && StaticReferenceManager.WeaponChestsSpawnedOnFloor == 0)
				{
					generalChestType = Chest.GeneralChestType.WEAPON;
				}
				else if (StaticReferenceManager.WeaponChestsSpawnedOnFloor > 0 && StaticReferenceManager.ItemChestsSpawnedOnFloor == 0)
				{
					generalChestType = Chest.GeneralChestType.ITEM;
				}
			}
			GenericLootTable genericLootTable = (generalChestType != Chest.GeneralChestType.WEAPON) ? manager.ItemsLootTable : manager.GunsLootTable;
			GameObject gameObject = DungeonPlaceableUtility.InstantiateDungeonPlaceable(chestPrefab, targetRoom, positionInRoom, true);
			gameObject.transform.position = gameObject.transform.position;
			Chest component = gameObject.GetComponent<Chest>();
			if (overrideMimicChance >= 0f)
			{
				component.overrideMimicChance = overrideMimicChance;
			}
			component.ChestType = generalChestType;
			component.lootTable.lootTable = genericLootTable;
			Component[] componentsInChildren = gameObject.GetComponentsInChildren(typeof(IPlaceConfigurable));
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				IPlaceConfigurable placeConfigurable = componentsInChildren[i] as IPlaceConfigurable;
				if (placeConfigurable != null)
				{
					placeConfigurable.ConfigureOnPlacement(targetRoom);
				}
			}

			PickupObject.ItemQuality targetQuality = manager.GetQualityFromChest(component);
			if (targetQuality == PickupObject.ItemQuality.A)
			{
				GameManager.Instance.Dungeon.GeneratedMagnificence += 1f;
				component.GeneratedMagnificence += 1f;
			}
			else if (targetQuality == PickupObject.ItemQuality.S)
			{
				GameManager.Instance.Dungeon.GeneratedMagnificence += 1f;
				component.GeneratedMagnificence += 1f;
			}
			if (component.specRigidbody)
			{
				component.specRigidbody.Reinitialize();
			}
			if (component.lootTable.canDropMultipleItems && component.lootTable.overrideItemLootTables != null && component.lootTable.overrideItemLootTables.Count > 0)
			{
				component.lootTable.overrideItemLootTables[0] = genericLootTable;
			}
			if (targetQuality == PickupObject.ItemQuality.D && !component.IsMimic)
			{
				StaticReferenceManager.DChestsSpawnedOnFloor++;
				StaticReferenceManager.DChestsSpawnedInTotal++;
				component.IsLocked = true;
				if (component.LockAnimator)
				{
					component.LockAnimator.renderer.enabled = true;
				}
			}
			targetRoom.RegisterInteractable(component);
			if (manager.SeededRunManifests.ContainsKey(GameManager.Instance.BestGenerationDungeonPrefab.tileIndices.tilesetId))
			{
				component.GenerationDetermineContents(manager.SeededRunManifests[GameManager.Instance.BestGenerationDungeonPrefab.tileIndices.tilesetId], random);
			}
			return component;
		}

		public static void ForEveryChest(Action<Chest> action)
		{
			for (int i = 0; i < StaticReferenceManager.AllChests.Count; i++)
			{
				Chest chest = StaticReferenceManager.AllChests[i];
				if (chest && !chest.IsOpen && !chest.IsBroken)
				{
					action(chest);
				}
			}
		}

		public static Chest ReplaceChestWithOtherChest(Chest toreplace, Chest replacer)
		{
			bool isLocked = toreplace.IsLocked, preventFuse = toreplace.PreventFuse;

			IntVector2 ivector2 = ((Vector2)toreplace.transform.position).ToIntVector2();
			RoomHandler room = GameManager.Instance.Dungeon.data.GetRoomFromPosition(ivector2);
			//toreplace.GetAbsoluteParentRoom().DeregisterInteractable(toreplace);
			toreplace.DeregisterChestOnMinimap();
			UnityEngine.Object.Destroy(toreplace.gameObject);

			Chest newchest = GameManager.Instance.RewardManager.SpawnGenerationChestAt(replacer.gameObject, room, ivector2 - room.area.basePosition, 0, replacer.ChestType); //GameManager.Instance.RewardManager.GenerationSpawnRewardChestAt(new IntVector2(10, 10), room); /*Chest.Spawn(replacer, ivector2, room);*/
			/*newchest.RegisterChestOnMinimap(room);
            room.RegisterInteractable(newchest);*/
			if (preventFuse || (room == GameManager.Instance.PrimaryPlayer.CurrentRoom))
			{
				if (room == GameManager.Instance.PrimaryPlayer.CurrentRoom)
				{
					LootEngine.DoDefaultItemPoof(ivector2.ToCenterVector2());
				}
				newchest.m_hasBeenCheckedForFuses = true;
			}
			if (!isLocked)
			{
				newchest.ForceUnlock();
			}
			newchest.PreventFuse = preventFuse;
			return newchest;
		}
	}
}
