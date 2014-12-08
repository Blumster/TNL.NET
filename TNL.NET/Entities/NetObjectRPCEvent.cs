﻿using System;

namespace TNL.NET.Entities
{
    using Utils;

    public abstract class NetObjectRPCEvent : RPCEvent
    {
        public NetObject DestObject { get; set; }
        public NetObjectRPCDirection RpcDirection { get; set; }

        protected NetObjectRPCEvent(NetObject destObject, RPCGuaranteeType gType, NetObjectRPCDirection dir)
            : base(gType, RPCDirection.RPCDirAny)
        {
            DestObject = destObject;
            RpcDirection = dir;
        }

        public override void Pack(EventConnection ps, BitStream stream)
        {
            var gc = ps as GhostConnection;
            if (gc == null)
                return;

            var ghostIndex = -1;
            if (DestObject != null)
                ghostIndex = gc.GetGhostIndex(DestObject);

            if (!stream.WriteFlag(ghostIndex != -1))
                return;

            stream.WriteInt((UInt32) ghostIndex, (Byte) GhostConnection.GhostIdBitSize);
            base.Pack(ps, stream);
        }

        public override void Unpack(EventConnection ps, BitStream stream)
        {
            var gc = ps as GhostConnection;
            if (gc == null)
                return;

            if ((gc.DoesGhostTo() && RpcDirection == NetObjectRPCDirection.RPCToGhost) ||
                (gc.DoesGhostFrom() && RpcDirection == NetObjectRPCDirection.RPCToGhostParent))
            {
                if (stream.ReadFlag())
                {
                    var ghostIndex = (Int32) stream.ReadInt((Byte) GhostConnection.GhostIdBitSize);
                    base.Unpack(ps, stream);

                    DestObject = RpcDirection == NetObjectRPCDirection.RPCToGhost ? gc.ResolveGhost(ghostIndex) : gc.ResolveGhostParent(ghostIndex);
                }
            }
            else
                NetConnection.SetLastError("Invalid packet.");
        }

        public override void Process(EventConnection ps)
        {
            if (DestObject == null)
                return;

            if (!CheckClassType(DestObject))
                return;

            NetObject.SetRPCSourceConnection(ps as GhostConnection);

            Functor.Dispatch(DestObject);

            NetObject.SetRPCSourceConnection(null);
        }
    }
}