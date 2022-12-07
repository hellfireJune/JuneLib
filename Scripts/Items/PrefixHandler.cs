using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JuneLib
{
    public static class PrefixHandler
    {

        internal static Dictionary<Assembly, string> pairs = new Dictionary<Assembly, string>();
        public static void AddPrefixForAssembly(string prefix, Assembly self = null)
        {
            if (self == null) { self = Assembly.GetCallingAssembly(); }
            pairs[self] = prefix;
        }
    }
}
