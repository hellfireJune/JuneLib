using Dungeonator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using static JuneLib.JuneLibRoomRewardAPI;

namespace JuneLib
{
    public static class JuneLibCore
    {
        /*private static Action<DebrisObject, RoomHandler> OnRoomClearItemDrop;
        private static Action<RoomHandler, ValidRoomRewardContents, float> OnRoomRewardDetermineContents;*/
        public static void Init()
        {
            ConsoleCommandGroup group = ETGModConsole.Commands.AddGroup("junelib", args =>
            {
                ETGModConsole.Log("Please specify a valid command.");
            });
            JunePlayerEvents.Init();

            ItemsCore.Init();
            GoopCore.Init();
            UICore.Init();


            ChallengeHelper.Init();
        }
    }
}
