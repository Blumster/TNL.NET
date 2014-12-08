using System;
using System.Net;

namespace TNL.NET.Utils
{
    using Network;

    public class PacketStream : BitStream
    {
        private readonly Byte[] _buffer = new Byte[TNLSocket.MaxPacketDataSize];

        public PacketStream(UInt32 targetPacketSize = TNLSocket.MaxPacketDataSize)
        {
            BufSize = targetPacketSize;
            Data = _buffer;

            SetMaxSizes(targetPacketSize, TNLSocket.MaxPacketDataSize);
            Reset();
            CurrentByte = new Byte[1];
        }

        public NetError SendTo(TNLSocket outgoingSocket, IPEndPoint theAddress)
        {
            return outgoingSocket.Send(theAddress, _buffer, GetBytePosition());
        }

        public NetError RecvFrom(TNLSocket incomingSocket, out IPEndPoint recvAddress)
        {
            if (incomingSocket.PacketsToBeHandled.Count == 0)
            {
                recvAddress = null;
                return NetError.WouldBlock;
            }

            var d = incomingSocket.PacketsToBeHandled.Dequeue();

            var dataSize = d.Item2.Length > TNLSocket.MaxPacketDataSize ? TNLSocket.MaxPacketDataSize : (UInt32) d.Item2.Length;

            Array.Copy(d.Item2, _buffer, dataSize);

            SetBuffer(_buffer, dataSize);
            SetMaxSizes(dataSize, 0U);
            Reset();

            recvAddress = d.Item1;

            return NetError.NoError;
        }
    }
}
