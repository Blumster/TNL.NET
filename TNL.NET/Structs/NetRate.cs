using System;

namespace TNL.NET.Structs
{
    public class NetRate
    {
        public UInt32 MinPacketSendPeriod { get; set; }
        public UInt32 MinPacketRecvPeriod { get; set; }
        public UInt32 MaxSendBandwidth { get; set; }
        public UInt32 MaxRecvBandwidth { get; set; }
    }
}
