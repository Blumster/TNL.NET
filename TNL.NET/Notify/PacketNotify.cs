using System;

namespace TNL.NET.Notify
{
    using Structs;

    public class PacketNotify
    {
        public Boolean RateChanged { get; set; }
        public Int32 SendTime { get; set; }
        public ConnectionStringTable.PacketList StringList { get; set; }
        public PacketNotify NextPacket { get; set; }

        public PacketNotify()
        {
            RateChanged = false;
            SendTime = 0;
        }
    }
}
