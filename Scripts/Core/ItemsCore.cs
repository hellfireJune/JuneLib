using JuneLib.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static JuneLib.OverrideItemCanBeUsed;

namespace JuneLib
{
    public static class ItemsCore
    {
        public static Action<PlayerController, PlayerItem, ValidOverrideArgs> OnPreUseItem;

        public static Action<PickupObject, PlayerController, bool, JuneLibLootEngineModificationAPI.ModifyLuckArgs> GetItemChanceMult;
        public static void Init()
        {
            //JuneLibRoomRewardAPI.Init();


            /*ETGModConsole.Commands.GetGroup("junelib").AddUnit("guarantee_room_drop", action: args =>
            {
                JuneLibRoomRewardAPI.GuaranteeRoomClearRewardDrop ^= true;
                ETGModConsole.Log($"Guarantee Room Clear Reward is now set to {JuneLibRoomRewardAPI.GuaranteeRoomClearRewardDrop}");
            });*/
        }
    }
}
