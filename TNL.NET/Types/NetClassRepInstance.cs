using System;

namespace TNL.NET.Types
{
    using Data;
    using Entities;

    public class NetClassRepInstance<T> : NetClassRep where T : BaseObject, new()
    {
        public NetClassRepInstance(String className, UInt32 groupMask, NetClassType classType, Int32 classVersion)
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
}
