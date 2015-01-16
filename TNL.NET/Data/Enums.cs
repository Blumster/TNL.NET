namespace TNL.NET.Data
{
    public enum NetClassType
    {
        NetClassTypeNone = -1,
        NetClassTypeObject = 0,
        NetClassTypeDataBlock,
        NetClassTypeEvent,
        NetClassTypeCount,
    }

    public enum NetClassGroup
    {
        NetClassGroupGame = 0,
        NetClassGroupCommunity,
        NetClassGroupMaster,
        NetClassGroupUnused2,
        NetClassGroupCount,
        NetClassGroupInvalid = NetClassGroupCount,
    };

    public enum NetClassMask
    {
        NetClassGroupGameMask      = 1 << NetClassGroup.NetClassGroupGame,
        NetClassGroupCommunityMask = 1 << NetClassGroup.NetClassGroupCommunity,
        NetClassGroupMasterMask    = 1 << NetClassGroup.NetClassGroupMaster,

        NetClassGroupAllMask      = (1 << NetClassGroup.NetClassGroupCount) - 1,
    };
}
