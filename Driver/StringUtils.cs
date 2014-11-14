using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BangBits.ER301.Driver
{
    public static class StringUtils
    {
        public static String littleEndianToBigEndian(this String hexString)
        {
            char[] littleEndian = hexString.ToCharArray();
            char[] bigEndian = new char[32];

            for (int i = 0; i < littleEndian.Length; i += 2)
            {
                bigEndian[31 - i] = littleEndian[i + 1];
                bigEndian[30 - i] = littleEndian[i];
            }

            return new String(bigEndian);
        }
    }
}
