using JuneLib.Status;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JuneLib
{
    internal static class GoopCore
    {
        public static void Init()
        {
            GenericStatusEffects.InitCustomEffects();
            EasyGoopDefinitions.DefineDefaultGoops();
        }
    }
}
