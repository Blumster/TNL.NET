using System;

namespace TNL.NET.Structs
{
    public class StringTableEntry
    {
        public UInt32 Index { get; private set; }

        public StringTableEntry()
        {
            Index = 0;
        }

        public StringTableEntry(String str, Boolean caseSensitive = true)
        {
            Index = StringTable.Insert(str, caseSensitive);
        }

        public StringTableEntry(StringTableEntry copy)
        {
            Index = copy.Index;
        }

        public void Set(String str, Boolean caseSensitive = true)
        {
            Index = StringTable.Insert(str, caseSensitive);
        }

        public Boolean IsNull()
        {
            return Index == 0U;
        }

        public Boolean IsNotNull()
        {
            return Index != 0U;
        }

        public Boolean IsValid()
        {
            return Index != 0U;
        }

        public String GetString()
        {
            return StringTable.GetString(Index);
        }

        public static Boolean operator ==(StringTableEntry s1, StringTableEntry s2)
        {
            return s1 != null && s2 != null && s1.Index == s2.Index;
        }

        public static Boolean operator !=(StringTableEntry s1, StringTableEntry s2)
        {
            return !(s1 == s2);
        }

        public static implicit operator Boolean(StringTableEntry s)
        {
            return s.Index != 0U;
        }

        public Boolean Equals(StringTableEntry other)
        {
            return Index == other.Index;
        }

        public override Boolean Equals(Object obj)
        {
            if (obj == null)
                return false;

            var other = obj as StringTableEntry;
            return other != null && Equals(other);
        }

        public override Int32 GetHashCode()
        {
            return Index.GetHashCode();
        }
    }
}
