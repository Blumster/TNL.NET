using System;
using System.Collections.Generic;
using System.Linq;

namespace TNL.NET.Structs
{
    public static class StringTable
    {
        public const UInt32 InitialHashTableSize = 1237;
        public const UInt32 InitialNodeListSize = 2048;
        public const UInt32 CompactThreshold = 32768;

        private static readonly Dictionary<UInt32, String> Table = new Dictionary<UInt32, String>();
        private static UInt32 _nextIndex = 1;

        public static UInt32 Insert(String str, Boolean caseSensitive = true)
        {
            if (str.Length == 0)
                return 0U;

            var ind = Lookup(str, caseSensitive);
            if (ind > 0)
                return ind;

            ind = _nextIndex++;

            Table.Add(ind, str);

            return ind;
        }

        public static UInt32 Lookup(String str, Boolean caseSensitive = true)
        {
            return (from pair in Table where pair.Value == str select pair.Key).FirstOrDefault();
        }

        public static UInt32 HashString(String inString)
        {
            throw new NotImplementedException();
        }

        public static String GetString(UInt32 index)
        {
            return index == 0 ? "" : Table[index];
        }

        public class Node
        {
            public UInt32 MasterIndex { get; set; }
            public UInt32 NextIndex { get; set; }
            public UInt32 Hash { get; set; }
            public UInt16 StringLen { get; set; }
            public UInt16 RefCount { get; set; }
            public String StringData { get; set; }
        }
    }
}
