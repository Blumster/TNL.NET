using System;

namespace TNL.NET.Structs
{
    using Entities;

    [Flags]
    public enum GhostInfoFlags
    {
        InScope = 1,
        ScopeLocalAlways = 2,
        NotYetGhosted = 4,
        Ghosting = 8,
        KillGhost = 16,
        KillingGhost = 32,

        NotAvailable = (NotYetGhosted | Ghosting | KillGhost | KillingGhost)
    }

    public class GhostInfo
    {
        public NetObject Obj { get; set; }
        public UInt64 UpdateMask { get; set; }
        public GhostRef LastUpdateChain { get; set; }
        public GhostInfo NextObjectRef { get; set; }
        public GhostInfo PrevObjectRef { get; set; }
        public GhostConnection Connection { get; set; }
        public UInt32 UpdateSkipCount { get; set; }
        public UInt32 Flags { get; set; }
        public Single Priority { get; set; }
        public UInt32 Index { get; set; }
        public Int32 ArrayIndex { get; set; }
    }
}
