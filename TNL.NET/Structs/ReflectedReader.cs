using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace TNL.NET.Structs
{
    using Utils;

    public static class ReflectedReader
    {
        private static readonly Dictionary<String, Func<BitStream, Type, Object>> ReadLookup = new Dictionary<String, Func<BitStream, Type, Object>>();
        private static readonly Dictionary<String, Action<BitStream, Object>> WriteLookup = new Dictionary<String, Action<BitStream, Object>>();

        static ReflectedReader()
        {
            // Read Functions
            ReadLookup.Add("Byte", (b, t) => { Byte v; b.Read(out v); return v; });
            ReadLookup.Add("SByte", (b, t) => { SByte v; b.Read(out v); return v; });
            ReadLookup.Add("Int16", (b, t) => { Int32 v; b.Read(out v); return v; });
            ReadLookup.Add("UInt16", (b, t) => { UInt32 v; b.Read(out v); return v; });
            ReadLookup.Add("Int32", (b, t) => { Int32 v; b.Read(out v); return v; });
            ReadLookup.Add("UInt32", (b, t) => { UInt32 v; b.Read(out v); return v; });
            ReadLookup.Add("Int64", (b, t) => { Int64 v; b.Read(out v); return v; });
            ReadLookup.Add("UInt64", (b, t) => { UInt64 v; b.Read(out v); return v; });

            ReadLookup.Add("Single", (b, t) => { Single v; b.Read(out v); return v; });
            ReadLookup.Add("Double", (b, t) => { Double v; b.Read(out v); return v; });

            ReadLookup.Add("String", (b, t) => { String v; b.ReadString(out v); return v; });
            ReadLookup.Add("StringTableEntry", (b, t) => { StringTableEntry v; b.ReadStringTableEntry(out v); return v; });

            ReadLookup.Add("List", (b, t) =>
            {
                var ret = (IList) Activator.CreateInstance(t);
                var size = b.ReadInt(8);
                var memberType = t.GenericTypeArguments[0];
                for (var i = 0; i < size; ++i)
                    ret.Add(ReadLookup[memberType.Name](b, memberType));

                return ret;
            });

            ReadLookup.Add("IPEndPoint", (b, t) =>
            {
                UInt32 netNum;
                UInt16 port;
                b.Read(out netNum);
                b.Read(out port);
                return new IPEndPoint(netNum, port);
            });

            ReadLookup.Add("ByteBuffer", (b, t) =>
            {
                var size = b.ReadInt(10);
                var ret = new ByteBuffer(size);
                b.Read(size, ret.GetBuffer());
                return ret;
            });

            // Write Functions
            WriteLookup.Add("Byte", (b, o) => b.Write((Byte) o));
            WriteLookup.Add("SByte", (b, o) => b.Write((SByte) o));
            WriteLookup.Add("Int16", (b, o) => b.Write((Int16) o));
            WriteLookup.Add("UInt16", (b, o) => b.Write((UInt16) o));
            WriteLookup.Add("Int32", (b, o) => b.Write((Int32) o));
            WriteLookup.Add("UInt32", (b, o) => b.Write((UInt32) o));
            WriteLookup.Add("Int64", (b, o) => b.Write((Int64) o));
            WriteLookup.Add("UInt64", (b, o) => b.Write((UInt64) o));

            WriteLookup.Add("Single", (b, o) => b.Write((Single) o));
            WriteLookup.Add("Double", (b, o) => b.Write((Double) o));

            WriteLookup.Add("String", (b, o) => b.WriteString((String) o));

            WriteLookup.Add("StringTableEntry", (b, o) => b.WriteStringTableEntry((StringTableEntry) o));

            WriteLookup.Add("List", (b, o) =>
            {
                var list = (IList) o;
                var memberType = o.GetType().GenericTypeArguments[0].Name;
                b.WriteInt((UInt32) list.Count, 8);
                foreach (var t in list)
                    WriteLookup[memberType](b, t);
            });

            WriteLookup.Add("IPEndPoint", (b, o) =>
            {
                var iep = (IPEndPoint) o;
                b.Write(BitConverter.ToUInt32(iep.Address.GetAddressBytes(), 0));
                b.Write((UInt16) iep.Port);
            });

            WriteLookup.Add("ByteBuffer", (b, o) =>
            {
                var bb = (ByteBuffer) o;
                b.WriteInt(bb.GetBufferSize(), 10);
                b.Write(bb.GetBufferSize(), bb.GetBuffer());
            });
        }

        public static Object Read(BitStream stream, Type type)
        {
            var typeName = type.Name;

            if (ReadLookup.ContainsKey(typeName))
                return ReadLookup[typeName](stream, type);

            Console.WriteLine("Reading {0} with reflecftion is not implemented!", type);
            return null;
        }

        public static void Write(BitStream stream, Object obj, Type type)
        {
            var typeName = type.Name;

            if (!WriteLookup.ContainsKey(typeName))
            {
                Console.WriteLine("Writing {0} with reflecftion is not implemented!", type);
                return;
            }

            WriteLookup[typeName](stream, obj);
        }
    }
}
