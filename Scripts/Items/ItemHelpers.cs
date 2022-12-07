using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PassiveItem;
public static class ItemHelpers
{

    public static void AddFlagsToPlayer(this PlayerController player, Type type)
    {
        if (!ActiveFlagItems.ContainsKey(player))
        {
            ActiveFlagItems.Add(player, new Dictionary<Type, int>());
        }
        if (!ActiveFlagItems[player].ContainsKey(type))
        {
            ActiveFlagItems[player].Add(type, 1);
        }
        else
        {
            ActiveFlagItems[player][type] = ActiveFlagItems[player][type] + 1;
        }
    }
    public static void RemoveFlagsFromPlayer(this PlayerController player, Type type)
    {

        if (player && ActiveFlagItems.ContainsKey(player) && ActiveFlagItems[player].ContainsKey(type))
        {
            ActiveFlagItems[player][type] = Mathf.Max(0, ActiveFlagItems[player][type] - 1);
            if (ActiveFlagItems[player][type] == 0)
            {
                ActiveFlagItems[player].Remove(type);
            }
        }
    }

    public static bool PlayerHasActiveSynergy(this PlayerController player, string synergyNameToCheck)
    {
        foreach (int index in player.ActiveExtraSynergies)
        {
            AdvancedSynergyEntry synergy = GameManager.Instance.SynergyManager.synergies[index];
            if (synergy.NameKey == synergyNameToCheck)
            {
                return true;
            }
        }
        return false;
    }

    public static bool SynergyActiveAtAll(string synergyNameToCheck)
    {
        for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
        {
            PlayerController player = GameManager.Instance.AllPlayers[i];
            if (player.PlayerHasActiveSynergy(synergyNameToCheck))
            {
                return true;
            }
        }
        return false;
    }


    public static void PlaceItemInAmmonomiconAfterItemById(this PickupObject item, int id) //from SpAPI's items
    {
        item.ForcedPositionInAmmonomicon = PickupObjectDatabase.GetById(id).ForcedPositionInAmmonomicon;
    }

    public static void AddStat(this PassiveItem item, PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
    {
        StatModifier statModifier = new StatModifier
        {
            amount = amount,
            statToBoost = statType,
            modifyType = method
        };
        foreach (StatModifier statModifier2 in item.passiveStatModifiers)
        {
            bool flag = statModifier2.statToBoost == statType;
            if (flag)
            {
                return;
            }
        }
        bool flag2 = item.passiveStatModifiers == null;
        if (flag2)
        {
            item.passiveStatModifiers = new StatModifier[]
            {
                    statModifier
            };
            return;
        }
        item.passiveStatModifiers = item.passiveStatModifiers.Concat(new StatModifier[]
        {
                statModifier
        }).ToArray<StatModifier>();
    }

    public static void RemoveStat(this PassiveItem item, PlayerStats.StatType statType)
    {
        List<StatModifier> list = new List<StatModifier>();
        for (int i = 0; i < item.passiveStatModifiers.Length; i++)
        {
            bool flag = item.passiveStatModifiers[i].statToBoost != statType;
            if (flag)
            {
                list.Add(item.passiveStatModifiers[i]);
            }
        }
        item.passiveStatModifiers = list.ToArray();
    }

    public static void AddStat(this PlayerItem item, PlayerStats.StatType statType, float amount, StatModifier.ModifyMethod method = StatModifier.ModifyMethod.ADDITIVE)
    {
        StatModifier statModifier = new StatModifier
        {
            amount = amount,
            statToBoost = statType,
            modifyType = method
        };
        foreach (StatModifier statModifier2 in item.passiveStatModifiers)
        {
            bool flag = statModifier2.statToBoost == statType;
            if (flag)
            {
                return;
            }
        }
        bool flag2 = item.passiveStatModifiers == null;
        if (flag2)
        {
            item.passiveStatModifiers = new StatModifier[]
            {
                    statModifier
            };
            return;
        }
        item.passiveStatModifiers = item.passiveStatModifiers.Concat(new StatModifier[]
        {
                statModifier
        }).ToArray<StatModifier>();
    }

    public static void RemoveStat(this PlayerItem item, PlayerStats.StatType statType)
    {
        List<StatModifier> list = new List<StatModifier>();
        for (int i = 0; i < item.passiveStatModifiers.Length; i++)
        {
            bool flag = item.passiveStatModifiers[i].statToBoost != statType;
            if (flag)
            {
                list.Add(item.passiveStatModifiers[i]);
            }
        }
        item.passiveStatModifiers = list.ToArray();
    }
}
