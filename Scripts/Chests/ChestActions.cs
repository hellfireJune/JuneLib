using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JuneLib
{
    public static class ChestActions
    {
        public static void OnPostOpenBullshit(Action<Chest, List<Transform>> orig, Chest self, List<Transform> transforms)
        {
            List<DebrisObject> list = new List<DebrisObject>();
            bool isRainbowRun = GameStatsManager.Instance.IsRainbowRun;
            Type chesttype = typeof(Chest);
            if (isRainbowRun && !self.IsRainbowChest && !self.m_forceDropOkayForRainbowRun)
            {
                Vector2 a;
                if (self.spawnTransform != null)
                {
                    a = self.spawnTransform.position;
                }
                else
                {
                    Bounds bounds = self.sprite.GetBounds();
                    a = self.transform.position + bounds.extents;
                }
                LootEngine.SpawnBowlerNote(GameManager.Instance.RewardManager.BowlerNoteChest, a + new Vector2(-0.5f, -2.25f), self.m_room, true);
            }
            else
            {
                for (int i = 0; i < self.contents.Count; i++)
                {
                    List<DebrisObject> list2 = LootEngine.SpewLoot(new List<GameObject>
                {
                    self.contents[i].gameObject
                }, transforms[i].position);
                    list.AddRange(list2);
                    for (int j = 0; j < list2.Count; j++)
                    {
                        if (list2[j])
                        {
                            list2[j].PreventFallingInPits = true;
                        }
                        if (!(list2[j].GetComponent<Gun>() != null))
                        {
                            if (!(list2[j].GetComponent<CurrencyPickup>() != null))
                            {
                                if (list2[j].specRigidbody != null)
                                {
                                    list2[j].specRigidbody.CollideWithOthers = false;
                                    DebrisObject debrisObject = list2[j];
                                    debrisObject.OnTouchedGround += self.BecomeViableItem;
                                }
                            }
                        }
                    }
                }
            }
            if (ChestsCore.OnPostSpawnChestContents != null)
            {
                ChestsCore.OnPostSpawnChestContents(list, self);
            }
            if (self.IsRainbowChest && isRainbowRun && self.transform.position.GetAbsoluteRoom() == GameManager.Instance.Dungeon.data.Entrance)
            {
                GameManager.Instance.Dungeon.StartCoroutine((IEnumerator)self.HandleRainbowRunLootProcessing(list));
            }
        }
    }
}
