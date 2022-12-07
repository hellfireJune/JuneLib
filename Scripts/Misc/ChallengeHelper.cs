using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JuneLib
{
    public static class  ChallengeHelper
    {
        public static ChallengeManager ChallengeManagerPrefab;
        public static void Init()
        {
            ChallengeManagerPrefab = ((GameObject)BraveResources.Load("Global Prefabs/_ChallengeManager", ".prefab")).GetComponent<ChallengeManager>();
        }
    }
}
