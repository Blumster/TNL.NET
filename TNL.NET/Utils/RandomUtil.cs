using System;

namespace TNL.NET.Utils
{
    public static class RandomUtil
    {
        public static void Read(Byte[] data, UInt32 size)
        {
            if (data != null)
                new Random().NextBytes(data);
        }

        public static UInt32 ReadI()
        {
            var data = new Byte[4];

            new Random().NextBytes(data);

            return BitConverter.ToUInt32(data, 0);
        }

        public static UInt32 ReadI(UInt32 rangeStart, UInt32 rangeEnd)
        {
            return (ReadI() % (rangeEnd - rangeStart + 1)) + rangeStart;
        }

        public static Single ReadF()
        {
            return ReadI() / (Single) UInt32.MaxValue;
        }

        public static Boolean ReadB()
        {
            var data = new Byte[1];

            new Random().NextBytes(data);

            return (data[0] & 1) != 0;
        }
    }
}
