using System.Collections.Generic;
using UnityEngine;

//Thanks nn :)
namespace JuneLib.Status
{
    public static class EasyGoopDefinitions
    {
        //Basegame Goops
        public static GoopDefinition FireDef;
        public static GoopDefinition OilDef;
        public static GoopDefinition PoisonDef;
        public static GoopDefinition BlobulonGoopDef;
        public static GoopDefinition WebGoop;
        public static GoopDefinition WaterGoop;
        public static GoopDefinition CharmGoopDef = PickupObjectDatabase.GetById(310)?.GetComponent<WingsItem>()?.RollGoop;
        public static GoopDefinition GreenFireDef = (PickupObjectDatabase.GetById(698) as Gun).DefaultModule.projectiles[0].GetComponent<GoopModifier>().goopDefinition;
        public static GoopDefinition CheeseDef = (PickupObjectDatabase.GetById(808) as Gun).DefaultModule.projectiles[0].GetComponent<GoopModifier>().goopDefinition;

        //Custom Goops
        public static GoopDefinition PlayerFriendlyWebGoop;
        public static void DefineDefaultGoops()
        {
            //Sets up the goops that have to be extracted from asset bundles
            AssetBundle assetBundle = ResourceManager.LoadAssetBundle("shared_auto_001");
            goopDefs = new List<GoopDefinition>();
            foreach (string text in goops)
            {
                GoopDefinition goopDefinition;
                try
                {
                    GameObject gameObject = assetBundle.LoadAsset(text) as GameObject;
                    goopDefinition = gameObject.GetComponent<GoopDefinition>();
                }
                catch
                {
                    goopDefinition = (assetBundle.LoadAsset(text) as GoopDefinition);
                }
                goopDefinition.name = text.Replace("assets/data/goops/", "").Replace(".asset", "");
                goopDefs.Add(goopDefinition);
            }
            List<GoopDefinition> list = goopDefs;

            //Define the asset bundle goops
            FireDef = goopDefs[0];
            OilDef = goopDefs[1];
            PoisonDef = goopDefs[2];
            BlobulonGoopDef = goopDefs[3];
            WebGoop = goopDefs[4];
            WaterGoop = goopDefs[5];

            //PLAYER FRIENDLY WEB GOOP - A web-textured goop that slows down enemies, but not players.
            GoopDefinition midInitWeb = UnityEngine.Object.Instantiate(WebGoop);
            midInitWeb.playerStepsChangeLifetime = false;
            midInitWeb.SpeedModifierEffect = GenericStatusEffects.FriendlyWebGoopSpeedMod;
            PlayerFriendlyWebGoop = midInitWeb;
        }
        //also thanks NN :)
        public static GoopDefinition GenerateBloodGoop(float dps, Color Color, float lifeSpan = 20)
        {
            GoopDefinition Blood = ScriptableObject.CreateInstance<GoopDefinition>();
            Blood.CanBeIgnited = false;
            Blood.damagesEnemies = true;
            Blood.damagesPlayers = false;
            Blood.baseColor32 = Color;
            Blood.goopTexture = PoisonDef.goopTexture;
            Blood.lifespan = lifeSpan;
            Blood.usesLifespan = true;
            Blood.damagePerSecondtoEnemies = dps;
            Blood.CanBeElectrified = true;
            Blood.electrifiedTime = 1;
            Blood.electrifiedDamagePerSecondToEnemies = 20;
            Blood.electrifiedDamageToPlayer = 0.5f;
            Blood.goopDamageTypeInteractions = new List<GoopDefinition.GoopDamageTypeInteraction> { new GoopDefinition.GoopDamageTypeInteraction { damageType = CoreDamageTypes.Electric, electrifiesGoop = true } };
            return Blood;
        }

        private static string[] goops = new string[]
        {
            "assets/data/goops/napalmgoopthatworks.asset",
            "assets/data/goops/oil goop.asset",
            "assets/data/goops/poison goop.asset",
            "assets/data/goops/blobulongoop.asset",
            "assets/data/goops/phasewebgoop.asset",
            "assets/data/goops/water goop.asset",
        };

        private static List<GoopDefinition> goopDefs;
    }

}