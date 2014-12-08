using System;

namespace TNL.NET.Structs
{
    public class BitSet
    {
        public UInt32 BitMask { get; private set; }

        public BitSet()
        {
            BitMask = 0U;
        }

        public BitSet(UInt32 bits)
        {
            BitMask = bits;
        }

        public void Set()
        {
            BitMask = 0xFFFFFFFFU;
        }

        public void Set(UInt32 bits)
        {
            BitMask |= bits;
        }

        public void Set(BitSet s, Boolean b)
        {
            BitMask = (BitMask & ~(s.BitMask)) | (b ? s.BitMask : 0);
        }

        public void Clear()
        {
            BitMask = 0;
        }

        public void Clear(UInt32 bits)
        {
            BitMask &= ~bits;
        }

        public void Toggle(UInt32 bits)
        {
            BitMask ^= bits;
        }

        public Boolean Test(UInt32 bits)
        {
            return (BitMask & bits) != 0U;
        }

        public Boolean TestStrict(UInt32 bits)
        {
            return (BitMask & bits) == bits;
        }

        public static implicit operator UInt32(BitSet b)
        {
            return b.BitMask;
        }

        public static implicit operator BitSet(UInt32 bits)
        {
            return new BitSet(bits);
        }

        public static BitSet operator |(BitSet b, UInt32 bits)
        {
            return new BitSet(b.BitMask | bits);
        }

        public static BitSet operator &(BitSet b, UInt32 bits)
        {
            return new BitSet(b.BitMask & bits);
        }

        public static BitSet operator ^(BitSet b, UInt32 bits)
        {
            return new BitSet(b.BitMask ^ bits);
        }
    }
}
