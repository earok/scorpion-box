using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibRetroFE_WrapperOnly.Compatibility
{
    internal class math
    {
        internal static float clamp(float v, float min, float max)
        {
            if (v < min)
            {
                return min;
            }
            if (v > max)
            {
                return max;
            }
            return v;
        }
    }
}
