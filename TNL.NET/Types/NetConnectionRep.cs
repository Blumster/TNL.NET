using System;
using System.Collections.Generic;
using System.Linq;

namespace TNL.NET.Types
{
    using Entities;

    public class NetConnectionRep
    {
        public static readonly List<NetConnectionRep> LinkedList = new List<NetConnectionRep>();
 
        public NetClassRep ClassRep { get; private set; }
        public Boolean CanRemoteCreate { get; private set; }

        public NetConnectionRep(NetClassRep classRep, Boolean canRemoteCreate)
        {
            LinkedList.Add(this);

            ClassRep = classRep;
            CanRemoteCreate = canRemoteCreate;
        }

        public static NetConnection Create(String name)
        {
            return (from walk in LinkedList where walk.CanRemoteCreate && walk.ClassRep.ClassName == name select walk.ClassRep.Create() as NetConnection).FirstOrDefault();
        }
    }
}
