using System;
using System.IO;

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

            using (var sw = new StreamWriter("reps.txt", true))
                sw.WriteLine("Registered \"{0,-51}\"! Type: {1,2} | GroupMask: {2,3} | Version: {3}", className, (Int32)classType, groupMask, classVersion);
        }

        public override BaseObject Create()
        {
            return new T();
        }
    }
}
