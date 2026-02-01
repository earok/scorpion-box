using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibRetroFE_WrapperOnly.Compatibility
{
    internal class Debug
    {
        internal static void Log(string v)
        {
            System.Diagnostics.Debug.WriteLine(v);
        }
    }
}
