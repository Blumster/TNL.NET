namespace TNL.Types;

using TNL.Data;
using TNL.Entities;

public class NetClassRepInstance<T> : NetClassRep where T : BaseObject, new()
{
    public NetClassRepInstance(string className, uint groupMask, NetClassType classType, int classVersion)
    {
        ClassName = className;
        ClassType = classType;
        ClassGroupMask = groupMask;
        ClassVersion = classVersion;

        for (var i = 0; i < ClassId.Length; ++i)
            ClassId[i] = 0U;

        ClassList.Add(this);
    }

    public override BaseObject Create()
    {
        return new T();
    }
}
