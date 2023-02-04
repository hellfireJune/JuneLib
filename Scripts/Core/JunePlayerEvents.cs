using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alexandria.Misc;
using JuneLib;

    public static class JunePlayerEvents
    {
        internal static void Init()
        {
            CustomActions.OnNewPlayercontrollerSpawned += player =>
            {
                player.gameObject.AddComponent<JEventsComponent>();
            };
        }

        internal static JEventsComponent GetJEvents(this PlayerController player)
        {
            return player.GetComponent<JEventsComponent>();
        }
    }

    public class JEventsComponent : BraveBehaviour
    {
        public Action<PlayerController, Gun, ProjectileVolleyData, RegeneratingVolleyModifiers.ModifyProjArgs> ConstantModifyGunVolley;

    }