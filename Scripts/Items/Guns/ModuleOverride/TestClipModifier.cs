using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JuneLib
{
    internal class TestClipModifier : ClipModifierBase
    {
        public override void AddVolleys(GunClipModifiers.RuntimeModifierContainer modifier, Gun gun, PlayerController player, ProjectileModule baseModule)
        {
            var volley = ScriptableObject.CreateInstance<ProjectileVolleyData>();
            volley.projectiles = new List<ProjectileModule>() { (((Gun)PickupObjectDatabase.GetById(GunClipModifierHolder.TEST_GUN_ID)).DefaultModule) };

            //modifier.InsertData(0, volley);
            modifier.InsertData(1, volley);
            modifier.InsertData(2, volley);
            //modifier.InsertData(6, volley);
        }
    }
}
