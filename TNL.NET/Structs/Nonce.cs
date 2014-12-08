using System;

namespace TNL.NET.Structs
{
    using Utils;

    public class Nonce
    {
        public const Int32 NonceSize = 8;

        public Byte[] Data { get; private set; }

        public Nonce()
        {
            Data = new Byte[NonceSize];
        }

        public Nonce(Byte[] data)
            : this()
        {
            Array.Copy(data, Data, NonceSize);
        }

        public static Boolean operator ==(Nonce a, Nonce b)
        {
            return !ReferenceEquals(a, null) && !ReferenceEquals(b, null) && BitConverter.ToUInt64(a.Data, 0) == BitConverter.ToUInt64(b.Data, 0);
        }

        public static Boolean operator !=(Nonce a, Nonce b)
        {
            return !(a == b);
        }

        public override Boolean Equals(Object obj)
        {
            if (obj == null)
                return false;

            var other = obj as Nonce;
            if (other == null)
                return false;

            return this == other;
        }

        public override Int32 GetHashCode()
        {
            return (Data != null ? Data.GetHashCode() : 0);
        }

        public void Read(BitStream stream)
        {
            stream.Read(NonceSize, Data);
        }

        public void Write(BitStream stream)
        {
            stream.Write(NonceSize, Data);
        }

        public void GetRandom()
        {
            RandomUtil.Read(Data, NonceSize);
        }
    }
}
