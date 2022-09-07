namespace TNL.Notify;

using TNL.Entities;

public class GhostPacketNotify : EventPacketNotify
{
    public GhostRef GhostList { get; set; }

    public GhostPacketNotify()
    {
        GhostList = null;
    }
}
