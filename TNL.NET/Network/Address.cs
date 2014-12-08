using System;
using System.Net;

namespace TNL.NET.Network
{
    public enum TransportProtocol
    {
        IPProtocol,
        TCPProtocol,
        IPXProtocol,
        IPv6Protocol
    }

    public enum NamedAddress
    {
        None,
        Localhost,
        Broadcast,
        Any
    }

    public class Address
    {
        public TransportProtocol Transport { get; set; }
        public UInt16 Port { get; set; }
        public UInt32[] NetNum { get; private set; }

        private Address()
        {
            NetNum = new UInt32[4];
        }

        public Address(TransportProtocol type = TransportProtocol.IPProtocol, NamedAddress name = NamedAddress.Any, UInt16 port = 0)
            : this()
        {
            Transport = type;
            Port = port;

            switch (Transport)
            {
                case TransportProtocol.IPProtocol:
                    NetNum[1] = NetNum[2] = NetNum[3] = 0U;

                    switch (name)
                    {
                        case NamedAddress.None:
                            NetNum[0] = 0x00000000U;
                            break;

                        case NamedAddress.Localhost:
                            NetNum[0] = 0x0100007FU;
                            break;

                        case NamedAddress.Broadcast:
                            NetNum[0] = 0xFFFFFFFFU;
                            break;

                        case NamedAddress.Any:
                            NetNum[0] = 0x00000000U;
                            break;
                    }
                    break;

                case TransportProtocol.IPXProtocol:
                    NetNum[0] = NetNum[1] = NetNum[2] = NetNum[3] = 0xFFFFFFFFU;
                    break;
            }
        }

        public Address(String str)
            : this()
        {
            Set(str);
        }

        public Boolean Set(String str)
        {
            if (!str.StartsWith("ipx:"))
            {
                var isTCP = false;

                if (str.StartsWith("ip:"))
                    str = str.Substring(3);
                else if (str.StartsWith("tcp:"))
                {
                    isTCP = true;
                    str = str.Substring(4);
                }

                if (str.Length > 256)
                    return false;

                var end = str.Length;
                var ind = str.IndexOf(':');
                if (ind != -1)
                    end = ind;

                var val = str.Substring(0, end);
                switch (val)
                {
                    case "broadcast":
                        NetNum[0] = 0xFFFFFFFFU;
                        break;

                    case "localhost":
                        NetNum[0] = 0x7F000001U;
                        break;

                    case "any":
                        NetNum[0] = 0x00000000U;
                        break;

                    default:
                        NetNum[0] = BitConverter.ToUInt32(IPAddress.Parse(val).GetAddressBytes(), 0);
                        /*var bytes = IPAddress.Parse(val).GetAddressBytes();
                        if (bytes.Length >= 4)
                            NetNum[0] = BitConverter.ToUInt32(bytes, 0);

                        if (bytes.Length >= 8)
                            NetNum[1] = BitConverter.ToUInt32(bytes, 4);

                        if (bytes.Length >= 12)
                            NetNum[2] = BitConverter.ToUInt32(bytes, 8);

                        if (bytes.Length == 16)
                            NetNum[4] = BitConverter.ToUInt32(bytes, 12);*/

                        break;
                }


                Port = ind > 0 ? UInt16.Parse(str.Substring(ind)) : (UInt16) 0U;

                Transport = isTCP ? TransportProtocol.TCPProtocol : TransportProtocol.IPProtocol;
                return true;
            }

            Transport = TransportProtocol.IPXProtocol;
            NetNum[0] = NetNum[1] = NetNum[2] = NetNum[3] = 0xFFFFFFFFU;

            if (str == "ipx:broadcast")
            {
                Port = 0;
                return true;
            }

            if (str.StartsWith("ipx:broadcast:"))
            {
                Port = UInt16.Parse(str.Substring(14));
                return true;
            }

            // TODO: it won't be used...
            return true;
        }

        public override String ToString()
        {
            return ToIPAddress().ToString();
        }

        public Boolean IsEqualAddress(Address b)
        {
            return b != null && Transport == b.Transport && NetNum[0] == b.NetNum[0] && NetNum[1] == b.NetNum[1] && NetNum[2] == b.NetNum[2] && NetNum[3] == b.NetNum[3];
        }

        public UInt32 Hash()
        {
            return NetNum[0] ^ ((UInt32) Port << 8) ^ (NetNum[1] << 16) ^ (NetNum[1] >> 16) ^ (NetNum[2] << 5);
        }

        public IPEndPoint ToIPAddress()
        {
            return new IPEndPoint(NetNum[0], Port);
        }

        public static Boolean operator ==(Address a, Address b)
        {
            return a != null && b != null && a.IsEqualAddress(b) && a.Port == b.Port;
        }

        public static Boolean operator !=(Address a, Address b)
        {
            return !(a == b);
        }

        protected bool Equals(Address other)
        {
            return Transport == other.Transport && Port == other.Port && Equals(NetNum, other.NetNum);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Address)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Transport;
                hashCode = (hashCode * 397) ^ Port.GetHashCode();
                hashCode = (hashCode * 397) ^ (NetNum != null ? NetNum.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
