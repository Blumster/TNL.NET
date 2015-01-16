using System;
using System.Collections.Generic;
using System.Security;
using TNL.NET.Notify;
using TNL.NET.Utils;

namespace TNL.NET.Entities
{
    using Data;
    using Structs;
    using Types;

    public class GhostRef
    {
        public UInt64 Mask { get; set; }
        public UInt32 GhostInfoFlags { get; set; }
        public GhostInfo Ghost { get; set; }
        public GhostRef NextRef { get; set; }
        public GhostRef UpdateChain { get; set; }
    }

    public class GhostConnection : EventConnection
    {
        #region Consts

        public const UInt32 GhostIdBitSize = 10;
        public const UInt32 GhostLookupTableSizeShift = 10;

        public const UInt32 MaxGhostCount = (1U << (Int32) GhostIdBitSize);
        public const UInt32 GhostCountBitSize = GhostIdBitSize + 1;

        public const UInt32 GhostLookupTableSize = (1 << (Int32) GhostLookupTableSizeShift);
        public const UInt32 GhostLookupTableMask = (GhostLookupTableSize - 1);

        #endregion Consts

        protected GhostInfo[] GhostArray;

        protected Int32 GhostZeroUpdateIndex;
        protected Int32 GhostFreeIndex;

        protected Boolean Ghosting;
        protected Boolean Scoping;
        protected UInt32 GhostingSequence;
        protected NetObject[] LocalGhosts;
        protected GhostInfo[] GhostRefs;
        protected NetObject ScopeObject;

        public static void RegisterNetClassReps()
        {
            NetEvent.ImplementNetEvent(out RPCStartGhosting.DynClassRep,        "RPC_GhostConnection_rpcStartGhosting",        NetClassMask.NetClassGroupGameMask, 0);
            NetEvent.ImplementNetEvent(out RPCReadyForNormalGhosts.DynClassRep, "RPC_GhostConnection_rpcReadyForNormalGhosts", NetClassMask.NetClassGroupGameMask, 0);
            NetEvent.ImplementNetEvent(out RPCEndGhosting.DynClassRep,          "RPC_GhostConnection_rpcEndGhosting",          NetClassMask.NetClassGroupGameMask, 0);
        }

        public GhostConnection()
        {
            ScopeObject = null;
            GhostingSequence = 0U;
            Ghosting = false;
            Scoping = false;
            GhostArray = null;
            GhostRefs = null;
            LocalGhosts = null;
            GhostZeroUpdateIndex = 0;
        }

        ~GhostConnection()
        {
            ClearAllPacketNotifies();

            if (GhostArray != null)
                ClearGhostInfo();

            DeleteLocalGhosts();
        }

        protected override PacketNotify AllocNotify()
        {
            return new GhostPacketNotify();
        }

        protected override void PacketDropped(PacketNotify note)
        {
            base.PacketDropped(note);

            var notify = note as GhostPacketNotify;
            if (notify == null)
                return;

            var packRef = notify.GhostList;
            while (packRef != null)
            {
                var temp = packRef.NextRef;

                var updateFlags = packRef.Mask;

                for (var walk = packRef.UpdateChain; walk != null && updateFlags > 0; walk = walk.UpdateChain)
                    updateFlags &= ~walk.Mask;

                if (updateFlags > 0UL)
                {
                    if (packRef.Ghost.UpdateMask == 0UL)
                    {
                        packRef.Ghost.UpdateMask = updateFlags;
                        GhostPushNonZero(packRef.Ghost);
                    }
                    else
                        packRef.Ghost.UpdateMask |= updateFlags;
                }

                if (packRef.Ghost.LastUpdateChain == packRef)
                    packRef.Ghost.LastUpdateChain = null;

                if ((packRef.GhostInfoFlags & (UInt32) GhostInfoFlags.Ghosting) != 0U)
                {
                    packRef.Ghost.Flags |= (UInt32) GhostInfoFlags.NotYetGhosted;
                    packRef.Ghost.Flags &= ~(UInt32) GhostInfoFlags.Ghosting;
                }
                else if ((packRef.GhostInfoFlags & (UInt32)GhostInfoFlags.KillingGhost) != 0U)
                {
                    packRef.Ghost.Flags |= (UInt32) GhostInfoFlags.KillGhost;
                    packRef.Ghost.Flags &= ~(UInt32) GhostInfoFlags.KillingGhost;
                }

                packRef = temp;
            }
        }

        protected override void PacketReceived(PacketNotify note)
        {
            base.PacketReceived(note);

            var notify = note as GhostPacketNotify;
            if (notify == null)
                return;

            var packRef = notify.GhostList;

            while (packRef != null)
            {
                if (packRef.Ghost.LastUpdateChain == packRef)
                    packRef.Ghost.LastUpdateChain = null;

                var temp = packRef.NextRef;

                if ((packRef.GhostInfoFlags & (UInt32) GhostInfoFlags.Ghosting) != 0U)
                {
                    packRef.Ghost.Flags &= ~(UInt32) GhostInfoFlags.Ghosting;

                    if (packRef.Ghost.Obj != null)
                        packRef.Ghost.Obj.OnGhostAvailable(this);
                }
                else if ((packRef.GhostInfoFlags & (UInt32) GhostInfoFlags.KillingGhost) != 0U)
                    FreeGhostInfo(packRef.Ghost);

                packRef = temp;
            }
        }

        public override void PrepareWritePacket()
        {
            base.PrepareWritePacket();

            if (!DoesGhostFrom() && !Ghosting)
                return;

            for (var i = 0; i < GhostZeroUpdateIndex; ++i)
            {
                var walk = GhostArray[i];
                ++walk.UpdateSkipCount;

                if ((walk.Flags & (UInt32) GhostInfoFlags.ScopeLocalAlways) == 0)
                    walk.Flags &= ~(UInt32) GhostInfoFlags.InScope;
            }

            if (ScopeObject != null)
                ScopeObject.PerformScopeQuery(this);
        }

        protected override void WritePacket(BitStream stream, PacketNotify note)
        {
            base.WritePacket(stream, note);

            var notify = note as GhostPacketNotify;
            if (notify == null)
                return;

            if (ConnectionParameters.DebugObjectSizes)
                stream.WriteInt(DebugCheckSum, 32);

            notify.GhostList = null;

            if (!DoesGhostFrom())
                return;

            if (!stream.WriteFlag(Ghosting && ScopeObject != null))
                return;

            for (var i = GhostZeroUpdateIndex - 1; i >= 0; --i)
            {
                if ((GhostArray[i].Flags & (UInt32) GhostInfoFlags.InScope) == 0)
                    DetachObject(GhostArray[i]);
            }

            var maxIndex = 0U;
            for (var i = GhostZeroUpdateIndex - 1; i >= 0; --i)
            {
                var walk = GhostArray[i];
                if (walk.Index > maxIndex)
                    maxIndex = walk.Index;

                if ((walk.Flags & (UInt32) GhostInfoFlags.KillGhost) != 0U &&
                    (walk.Flags & (UInt32) GhostInfoFlags.NotYetGhosted) != 0U)
                {
                    FreeGhostInfo(walk);
                    continue;
                }

                if ((walk.Flags & (UInt32) (GhostInfoFlags.KillingGhost | GhostInfoFlags.Ghosting)) == 0U)
                {
                    if ((walk.Flags & (UInt32) GhostInfoFlags.KillGhost) != 0U)
                        walk.Priority = 10000.0f;
                    else
                        walk.Priority = walk.Obj.GetUpdatePriority(ScopeObject, walk.UpdateMask, (Int32) walk.UpdateSkipCount);
                }
                else
                    walk.Priority = 0.0f;
            }

            GhostRef updateList = null;

            var list = new List<GhostInfo>();
            for (var i = 0; i < GhostZeroUpdateIndex; ++i)
                list.Add(GhostArray[i]);
            
            list.Sort(new GhostInfoComparer());

            for (var i = 0; i < list.Count; ++i)
            {
                GhostArray[i] = list[i];
                GhostArray[i].ArrayIndex = i;
            }

            var sendSize = 1;

            while ((maxIndex >>= 1) > 0)
                ++sendSize;

            if (sendSize < 3)
                sendSize = 3;

            stream.WriteInt((UInt32) sendSize - 3U, 3);

            for (var i = GhostZeroUpdateIndex - 1; i >= 0 && !stream.IsFull(); --i)
            {
                var walk = GhostArray[i];
                if ((walk.Flags & (UInt32) (GhostInfoFlags.KillingGhost | GhostInfoFlags.Ghosting)) != 0U)
                    continue;

                var updateStart = stream.GetBitPosition();
                var updateMask = walk.UpdateMask;
                var retMask = 0UL;

                stream.WriteFlag(true);
                stream.WriteInt(walk.Index, (Byte) sendSize);

                if (!stream.WriteFlag((walk.Flags & (UInt32) GhostInfoFlags.KillGhost) != 0U))
                {
                    if (ConnectionParameters.DebugObjectSizes)
                        stream.AdvanceBitPosition(BitStreamPosBitSize);

                    var startPos = stream.GetBitPosition();

                    if ((walk.Flags & (UInt32) GhostInfoFlags.NotYetGhosted) != 0U)
                    {
                        var classId = walk.Obj.GetClassId(GetNetClassGroup());
                        stream.WriteClassId(classId, (UInt32) NetClassType.NetClassTypeObject, (UInt32) GetNetClassGroup());
                        NetObject.PIsInitialUpdate = true;
                    }

                    retMask = walk.Obj.PackUpdate(this, updateMask, stream);

                    if (NetObject.PIsInitialUpdate)
                    {
                        NetObject.PIsInitialUpdate = false;
                        walk.Obj.GetClassRep().AddInitialUpdate(stream.GetBitPosition() - startPos);
                    }
                    else
                        walk.Obj.GetClassRep().AddPartialUpdate(stream.GetBitPosition() - startPos);

                    if (ConnectionParameters.DebugObjectSizes)
                        stream.WriteIntAt(stream.GetBitPosition(), BitStreamPosBitSize, startPos - BitStreamPosBitSize);
                }

                if (stream.GetBitSpaceAvailable() < MinimumPaddingBits)
                {
                    stream.SetBitPosition(updateStart);
                    stream.ClearError();
                    break;
                }

                var upd = new GhostRef
                {
                    NextRef = updateList
                };

                updateList = upd;

                if (walk.LastUpdateChain != null)
                    walk.LastUpdateChain.UpdateChain = upd;

                walk.LastUpdateChain = upd;

                upd.Ghost = walk;
                upd.GhostInfoFlags = 0U;
                upd.UpdateChain = null;

                if ((walk.Flags & (UInt32) GhostInfoFlags.KillGhost) != 0U)
                {
                    walk.Flags &= ~(UInt32) GhostInfoFlags.KillGhost;
                    walk.Flags |= (UInt32) GhostInfoFlags.KillingGhost;
                    walk.UpdateMask = 0UL;
                    upd.Mask = updateMask;
                    GhostPushToZero(walk);
                    upd.GhostInfoFlags = (UInt32) GhostInfoFlags.KillingGhost;
                }
                else
                {
                    if ((walk.Flags & (UInt32) GhostInfoFlags.NotYetGhosted) != 0U)
                    {
                        walk.Flags &= ~(UInt32) GhostInfoFlags.NotYetGhosted;
                        walk.Flags |= (UInt32) GhostInfoFlags.Ghosting;
                        upd.GhostInfoFlags = (UInt32) GhostInfoFlags.Ghosting;
                    }

                    walk.UpdateMask = retMask;
                    if (retMask == 0UL)
                        GhostPushToZero(walk);

                    upd.Mask = updateMask & ~retMask;
                    walk.UpdateSkipCount = 0U;
                }
            }

            stream.WriteFlag(false);
            notify.GhostList = updateList;
        }

        protected override void ReadPacket(BitStream stream)
        {
            base.ReadPacket(stream);

            if (ConnectionParameters.DebugObjectSizes)
            {
                var sum = stream.ReadInt(32);
                Console.WriteLine("Assert({0} == {1} || Invalid checksum.", sum, DebugCheckSum);
            }

            if (!DoesGhostTo())
                return;

            if (!stream.ReadFlag())
                return;

            var idSize = (Int32) stream.ReadInt(3) + 3;

            while (stream.ReadFlag())
            {
                var index = stream.ReadInt((Byte) idSize);

                if (stream.ReadFlag())
                {
                    if (LocalGhosts[index] != null)
                    {
                        LocalGhosts[index].OnGhostRemove();
                        LocalGhosts[index] = null;
                    }
                }
                else
                {
                    var endPos = 0U;
                    if (ConnectionParameters.DebugObjectSizes)
                        endPos = stream.ReadInt(BitStreamPosBitSize);

                    if (LocalGhosts[index] == null)
                    {
                        var classId = stream.ReadClassId((UInt32) NetClassType.NetClassTypeObject, (UInt32) GetNetClassGroup());
                        if (classId == -1)
                        {
                            SetLastError("Invalid packet.");
                            return;
                        }

                        var obj = Create((UInt32) GetNetClassGroup(), (UInt32) NetClassType.NetClassTypeObject, (Int32) classId) as NetObject;
                        if (obj == null)
                        {
                            SetLastError("Invalid packet.");
                            return;
                        }

                        obj.SetOwningConnection(this);
                        obj.SetNetFlags(NetFlag.IsGhost);
                        obj.SetNetIndex(index);

                        LocalGhosts[index] = obj;

                        NetObject.PIsInitialUpdate = true;

                        LocalGhosts[index].UnpackUpdate(this, stream);

                        NetObject.PIsInitialUpdate = false;

                        if (!obj.OnGhostAdd(this))
                        {
                            if (ErrorBuffer[0] == 0)
                                SetLastError("Invalid packet.");

                            return;
                        }

                        if (RemoteConnection != null)
                        {
                            var gc = RemoteConnection as GhostConnection;
                            if (gc == null)
                                return;

                            obj.SetServerObject(gc.ResolveGhostParent((Int32) index));
                        }
                    }
                    else
                        LocalGhosts[index].UnpackUpdate(this, stream);

                    if (ConnectionParameters.DebugObjectSizes)
                        Console.WriteLine("Assert({0} == {1} || unpackUpdate did not match packUpdate for object of class {0}. Expected {1} bits, got {2} bits.", LocalGhosts[index].GetClassName(), endPos, stream.GetBitPosition());

                    if (ErrorBuffer[0] != 0)
                        return;
                }
            }
        }

        public override Boolean IsDataToTransmit()
        {
            return base.IsDataToTransmit() || GhostZeroUpdateIndex != 0U;
        }

        protected void ClearGhostInfo()
        {
            for (var walk = NotifyQueueHead; walk != null; walk = walk.NextPacket)
            {
                var note = walk as GhostPacketNotify;
                if (note == null)
                    continue;

                var delWalk = note.GhostList;
                note.GhostList = null;

                while (delWalk != null)
                {
                    var next = delWalk.NextRef;

                    delWalk.Ghost = null;
                    delWalk.NextRef = null;
                    delWalk.UpdateChain = null;

                    delWalk = next;
                }
            }

            for (var i = 0; i < MaxGhostCount; ++i)
            {
                if (GhostRefs[i].ArrayIndex >= GhostFreeIndex)
                    continue;

                DetachObject(GhostRefs[i]);

                GhostRefs[i].LastUpdateChain = null;

                FreeGhostInfo(GhostRefs[i]);
            }
        }

        protected void DeleteLocalGhosts()
        {
            if (LocalGhosts == null)
                return;

            for (var i = 0; i < MaxGhostCount; ++i)
            {
                if (LocalGhosts[i] != null)
                {
                    LocalGhosts[i].OnGhostRemove();
                    LocalGhosts[i] = null;
                }
            }
        }

        protected Boolean ValidateGhostArray()
        {
            return true;
        }

        protected void FreeGhostInfo(GhostInfo ghost)
        {
            if (ghost.ArrayIndex < GhostZeroUpdateIndex)
            {
                ghost.UpdateMask = 0UL;
                GhostPushToZero(ghost);
            }

            GhostPushZeroToFree(ghost);
        }

        protected virtual void OnStartGhosting()
        {
        }

        protected virtual void OnEndGhosting()
        {
        }

        public void SetGhostFrom(Boolean ghostFrom)
        {
            if (GhostArray != null)
                return;

            if (!ghostFrom)
                return;

            GhostFreeIndex = GhostZeroUpdateIndex = 0;
            GhostArray = new GhostInfo[MaxGhostCount];
            GhostRefs = new GhostInfo[MaxGhostCount];

            for (var i = 0U; i < MaxGhostCount; ++i)
            {
                GhostRefs[i] = new GhostInfo
                {
                    Obj = null,
                    Index = i,
                    UpdateMask = 0UL
                };
            }
        }

        public void SetGhostTo(Boolean ghostTo)
        {
            if (LocalGhosts != null)
                return;

            if (ghostTo)
                LocalGhosts = new NetObject[MaxGhostCount];
        }

        public Boolean DoesGhostFrom()
        {
            return GhostArray != null;
        }

        public Boolean DoesGhostTo()
        {
            return LocalGhosts != null;
        }

        public UInt32 GetGhostingSequence()
        {
            return GhostingSequence;
        }

        public void SetScopeObject(NetObject obj)
        {
            ScopeObject = obj;
        }

        public NetObject GetScopeObject()
        {
            return ScopeObject;
        }

        public void ObjectInScope(NetObject obj)
        {
            if (!Scoping || !DoesGhostFrom())
                return;

            if (!obj.IsGhostable() || (obj.IsScopeLocal() && !IsLocalConnection()))
                return;

            for (var i = 0; i < MaxGhostCount; ++i)
            {
                if (GhostArray[i] != null && GhostArray[i].Obj == obj)
                {
                    GhostArray[i].Flags |= (UInt32) GhostInfoFlags.InScope;
                    return;
                }
            }

            if (GhostFreeIndex == MaxGhostCount)
                return;

            var gi = GhostArray[GhostFreeIndex];
            GhostPushFreeToZero(gi);
            gi.UpdateMask = 0xFFFFFFFFFFFFFFFFUL;
            GhostPushNonZero(gi);

            gi.Flags = (UInt32) (GhostInfoFlags.NotYetGhosted | GhostInfoFlags.InScope);
            gi.Obj = obj;
            gi.LastUpdateChain = null;
            gi.UpdateSkipCount = 0U;
            gi.Connection = this;
            gi.NextObjectRef = obj.GetFirstObjectRef();

            if (obj.GetFirstObjectRef() != null)
                obj.GetFirstObjectRef().PrevObjectRef = gi;

            gi.PrevObjectRef = null;
            obj.SetFirstObjectRef(gi);
        }

        public void ObjectLocalScopeAlways(NetObject obj)
        {
            if (!DoesGhostFrom())
                return;

            ObjectInScope(obj);

            for (var i = 0; i < MaxGhostCount; ++i)
            {
                if (GhostArray[i] != null && GhostArray[i].Obj == obj)
                {
                    GhostArray[i].Flags |= (UInt32) GhostInfoFlags.ScopeLocalAlways;
                    return;
                }
            }
        }

        public void ObjectLocalClearAlways(NetObject obj)
        {
            if (!DoesGhostFrom())
                return;

            for (var i = 0; i < MaxGhostCount; ++i)
            {
                if (GhostArray[i] != null && GhostArray[i].Obj == obj)
                {
                    GhostArray[i].Flags &= ~(UInt32) GhostInfoFlags.ScopeLocalAlways;
                    return;
                }
            }
        }

        public NetObject ResolveGhost(Int32 id)
        {
            return id == -1 ? null : LocalGhosts[id];
        }

        public NetObject ResolveGhostParent(Int32 id)
        {
            return GhostRefs[id].Obj;
        }

        public void GhostPushNonZero(GhostInfo info)
        {
            if (info.ArrayIndex != GhostZeroUpdateIndex)
            {
                GhostArray[GhostZeroUpdateIndex].ArrayIndex = info.ArrayIndex;
                GhostArray[info.ArrayIndex] = GhostArray[GhostZeroUpdateIndex];
                GhostArray[GhostZeroUpdateIndex] = info;
                info.ArrayIndex = GhostZeroUpdateIndex;
            }
            ++GhostZeroUpdateIndex;
        }

        public void GhostPushToZero(GhostInfo info)
        {
            --GhostZeroUpdateIndex;

            if (info.ArrayIndex == GhostZeroUpdateIndex)
                return;

            GhostArray[GhostZeroUpdateIndex].ArrayIndex = info.ArrayIndex;
            GhostArray[info.ArrayIndex] = GhostArray[GhostZeroUpdateIndex];
            GhostArray[GhostZeroUpdateIndex] = info;
            info.ArrayIndex = GhostZeroUpdateIndex;
        }

        public void GhostPushZeroToFree(GhostInfo info)
        {
            --GhostFreeIndex;

            if (info.ArrayIndex != GhostFreeIndex)
            {
                GhostArray[GhostFreeIndex].ArrayIndex = info.ArrayIndex;
                GhostArray[info.ArrayIndex] = GhostArray[GhostFreeIndex];
                GhostArray[GhostFreeIndex] = info;
                info.ArrayIndex = GhostFreeIndex;
            }
        }

        public void GhostPushFreeToZero(GhostInfo info)
        {
            if (info.ArrayIndex != GhostFreeIndex)
            {
                GhostArray[GhostFreeIndex].ArrayIndex = info.ArrayIndex;
                GhostArray[info.ArrayIndex] = GhostArray[GhostFreeIndex];
                GhostArray[GhostFreeIndex] = info;
                info.ArrayIndex = GhostFreeIndex;
            }

            ++GhostFreeIndex;
        }

        public Int32 GetGhostIndex(NetObject obj)
        {
            if (obj == null)
                return -1;

            if (!DoesGhostFrom())
                return (Int32) obj.GetNetIndex();

            for (var i = 0; i < MaxGhostCount; ++i)
                if (GhostArray[i] != null && GhostArray[i].Obj == obj && (GhostArray[i].Flags & (UInt32) (GhostInfoFlags.KillingGhost | GhostInfoFlags.Ghosting | GhostInfoFlags.NotYetGhosted | GhostInfoFlags.KillGhost)) == 0U)
                    return (Int32) GhostArray[i].Index;

            return -1;
        }

        public Boolean IsGhostAvailable(NetObject obj)
        {
            return GetGhostIndex(obj) != -1;
        }

        public void ResetGhosting()
        {
            if (!DoesGhostFrom())
                return;

            Ghosting = false;
            Scoping = false;

            rpcEndGhosting();

            ++GhostingSequence;

            ClearGhostInfo();
        }

        public void ActivateGhosting()
        {
            if (!DoesGhostFrom())
                return;

            ++GhostingSequence;

            for (var i = 0; i < MaxGhostCount; ++i)
            {
                GhostArray[i] = GhostRefs[i];
                GhostArray[i].ArrayIndex = i;
            }

            Scoping = true;
            rpcStartGhosting(GhostingSequence);
        }

        public Boolean IsGhosting()
        {
            return Ghosting;
        }

        public void DetachObject(GhostInfo info)
        {
            info.Flags |= (UInt32) GhostInfoFlags.KillGhost;

            if (info.UpdateMask == 0UL)
            {
                info.UpdateMask = 0xFFFFFFFFFFFFFFFFUL;
                GhostPushNonZero(info);
            }

            if (info.Obj == null)
                return;

            if (info.PrevObjectRef != null)
                info.PrevObjectRef.NextObjectRef = info.NextObjectRef;
            else
                info.Obj.SetFirstObjectRef(info.NextObjectRef);

            if (info.NextObjectRef != null)
                info.NextObjectRef.PrevObjectRef = info.PrevObjectRef;

            info.PrevObjectRef = info.NextObjectRef = null;
            info.Obj = null;
        }

        private class GhostInfoComparer : IComparer<GhostInfo>
        {
            public Int32 Compare(GhostInfo a, GhostInfo b)
            {
                var ret = a.Priority - b.Priority;
                return ret < 0.0f ? -1 : (ret > 0.0f ? 1 : 0);
            }
        }

        #region RPC Calls

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
        public void rpcStartGhosting(UInt32 sequence)
        #region rpcStartGhosting
        {
            var rpcEvent = new RPCStartGhosting();
            rpcEvent.Functor.Set(new Object[] { sequence });

            PostNetEvent(rpcEvent);
        }

        private void rpcStartGhosting_remote(UInt32 sequence)
        #endregion
        {
            if (!DoesGhostTo())
            {
                SetLastError("Invalid packet.");
                return;
            }

            OnStartGhosting();
            rpcReadyForNormalGhosts(sequence);
        }

        public void rpcReadyForNormalGhosts(UInt32 sequence)
        #region rpcReadyForNormalGhosts
        {
            var rpcEvent = new RPCReadyForNormalGhosts();
            rpcEvent.Functor.Set(new Object[] { sequence });

            PostNetEvent(rpcEvent);
        }

        private void rpcReadyForNormalGhosts_remote(UInt32 sequence)
        #endregion
        {
            if (!DoesGhostFrom())
            {
                SetLastError("Invalid packet.");
                return;
            }

            if (sequence != GhostingSequence)
                return;

            Ghosting = true;
        }

        public void rpcEndGhosting()
        #region rpcEndGhosting
        {
            var rpcEvent = new RPCEndGhosting();
            rpcEvent.Functor.Set(new Object[] { });

            PostNetEvent(rpcEvent);
        }

        private void rpcEndGhosting_remote()
        #endregion
        {
            if (!DoesGhostTo())
            {
                SetLastError("Invalid packet.");
                return;
            }

            DeleteLocalGhosts();
            OnEndGhosting();
        }
// ReSharper restore UnusedMember.Local
// ReSharper restore InconsistentNaming

        #endregion

        #region RPC Classes

        private class RPCStartGhosting : RPCEvent
        {
            public static NetClassRepInstance<RPCStartGhosting> DynClassRep;
            public RPCStartGhosting() : base(RPCGuaranteeType.RPCGuaranteedOrdered, RPCDirection.RPCDirAny)
            { Functor = new FunctorDecl<GhostConnection>("rpcStartGhosting_remote", new[] { typeof(UInt32) }); }
            public override Boolean CheckClassType(Object obj) { return (obj as GhostConnection) != null; }
            public override NetClassRep GetClassRep() { return DynClassRep; }
        }

        private class RPCReadyForNormalGhosts : RPCEvent
        {
            public static NetClassRepInstance<RPCReadyForNormalGhosts> DynClassRep;
            public RPCReadyForNormalGhosts() : base(RPCGuaranteeType.RPCGuaranteedOrdered, RPCDirection.RPCDirAny)
            { Functor = new FunctorDecl<GhostConnection>("rpcReadyForNormalGhosts_remote", new[] { typeof(UInt32) }); }
            public override Boolean CheckClassType(Object obj) { return (obj as GhostConnection) != null; }
            public override NetClassRep GetClassRep() { return DynClassRep; }
        }

        private class RPCEndGhosting : RPCEvent
        {
            public static NetClassRepInstance<RPCEndGhosting> DynClassRep;
            public RPCEndGhosting() : base(RPCGuaranteeType.RPCGuaranteedOrdered, RPCDirection.RPCDirAny)
            { Functor = new FunctorDecl<GhostConnection>("rpcEndGhosting_remote", new Type[] { }); }
            public override Boolean CheckClassType(Object obj) { return (obj as GhostConnection) != null; }
            public override NetClassRep GetClassRep() { return DynClassRep; }
        }

        #endregion
    }
}
