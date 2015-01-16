using System;

namespace TNL.NET.Utils
{
    public static class Utils
    {
        public static Boolean IsPow2(UInt32 number)
        {
            return number > 0 && (number & (number - 1)) == 0;
        }

        public static UInt32 GetBinLog2(UInt32 value)
        {
            var floatValue = (Single) value;
            var ret = Math.Floor(Math.Log(value, 2));

            unsafe
            {
                return (*((UInt32*) &floatValue) >> 23) - 127;
            }
        }

        public static UInt32 GetNextBinLog2(UInt32 number)
        {
            return GetBinLog2(number) + (IsPow2(number) ? 0U : 1U);
        }

        public static UInt32 GetNextPow2(UInt32 value)
        {
            return IsPow2(value) ? value : (1U << ((Int32) GetBinLog2(value) + 1));
        }
    }
}
