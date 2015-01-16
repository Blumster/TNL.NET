using System;

namespace TNL.NET.Utils
{
    using Entities;

    public class ByteBuffer : BaseObject
    {
        public const UInt32 DefaultBufferSize = 1500U;

        protected Byte[] Data;
        protected UInt32 BufSize;

        public ByteBuffer(Byte[] data, UInt32 bufferSize)
        {
            BufSize = bufferSize;
            Data = data;
        }

        public ByteBuffer(UInt32 bufferSize = DefaultBufferSize)
        {
            BufSize = bufferSize;
            Data = new Byte[BufSize];
        }

        public void SetBuffer(Byte[] data, UInt32 bufferSize)
        {
            Data = data;
            BufSize = bufferSize;
        }

        public Boolean Resize(UInt32 newBufferSize)
        {
            if (BufSize >= newBufferSize)
                BufSize = newBufferSize;
            else
            {
                BufSize = newBufferSize;
                Array.Resize(ref Data, (Int32) newBufferSize);
                return true;
            }

            return false;
        }

        public Boolean AppendBuffer(Byte[] dataBuffer, UInt32 bufferSize)
        {
            var start = BufSize;
            if (!Resize(BufSize + bufferSize))
                return false;

            Array.Copy(dataBuffer, 0, Data, start, bufferSize);
            return true;
        }

        public Boolean AppendBuffer(ByteBuffer theBuffer)
        {
            return AppendBuffer(theBuffer.GetBuffer(), theBuffer.GetBufferSize());
        }

        public UInt32 GetBufferSize()
        {
            return BufSize;
        }

        public Byte[] GetBuffer()
        {
            return Data;
        }

        public void Clear()
        {
            for (var i = 0; i < BufSize; ++i)
                Data[i] = 0;
        }
    }
}
