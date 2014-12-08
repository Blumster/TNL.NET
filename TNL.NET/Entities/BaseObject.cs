using System;

namespace TNL.NET.Entities
{
    using Data;
    using Interfaces;
    using Types;

    public class BaseObject : INetObject
    {
        public virtual NetClassRep GetClassRep()
        {
            return null;
        }

        public UInt32 GetClassId(NetClassGroup group)
        {
            return GetClassRep().GetClassId(group);
        }

        public String GetClassName()
        {
            return GetClassRep().ClassName;
        }

        public static BaseObject Create(String className)
        {
            return NetClassRep.Create(className);
        }

        public static BaseObject Create(UInt32 groupId, UInt32 typeId, Int32 classId)
        {
            return NetClassRep.Create(groupId, typeId, classId);
        }

        public static void Declare<T>(out NetClassRep rep) where T : BaseObject, new()
        {
            rep = new NetClassRepInstance<T>(typeof(T).Name, 0, NetClassType.NetClassTypeNone, 0);
        }
    }
}
