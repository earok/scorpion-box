using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibRetroFE_WrapperOnly.Compatibility
{
    internal class Mathf
    {
        internal static float Clamp(short value, short minValue, short maxValue)
        {
            if(value < minValue)
            {
                return value;
            }
            if(value > maxValue)
            {
                return value;
            }
            return value;
        }

        internal static short Round(float floatValue)
        {
            return (short)Math.Round(floatValue);
        }
    }
}
