using System;
using System.Collections.Generic;
using System.Linq;

namespace TNL.NET.Types
{
    using Data;
    using Entities;
    using Utils;

    public abstract class NetClassRep
    {
        public static readonly List<NetClassRep> ClassList = new List<NetClassRep>();

        public static UInt32[][] NetClassBitSize { get; private set; }
        public static List<NetClassRep>[][] ClassTable { get; private set; }
        public static Boolean Initialized { get; private set; }
        public static UInt32[] ClassCRC { get; private set; }

        public UInt32 ClassGroupMask { get; protected set; }
        public Int32 ClassVersion { get; protected set; }
        public NetClassType ClassType { get; protected set; }
        public UInt32[] ClassId { get; protected set; }
        public String ClassName { get; protected set; }

        public UInt32 InitialUpdateBitsUsed { get; protected set; }
        public UInt32 PartialUpdateBitsUsed { get; protected set; }
        public UInt32 InitialUpdateCount { get; protected set; }
        public UInt32 PartialUpdateCount { get; protected set; }

        protected NetClassRep()
        {
            InitialUpdateCount = 0;
            InitialUpdateBitsUsed = 0;
            PartialUpdateCount = 0;
            PartialUpdateBitsUsed = 0;

            ClassId = new UInt32[(Int32) NetClassGroup.NetClassGroupCount];
        }

        public UInt32 GetClassId(NetClassGroup classGroup)
        {
            return ClassId[(Int32) classGroup];
        }

        public void AddInitialUpdate(UInt32 bitCount)
        {
            ++InitialUpdateCount;
            InitialUpdateBitsUsed += bitCount;
        }

        public void AddPartialUpdate(UInt32 bitCount)
        {
            ++PartialUpdateCount;
            PartialUpdateBitsUsed += bitCount;
        }

        public abstract BaseObject Create();

        #region Static Functions

        static NetClassRep()
        {
            Initialized = false;

            ClassCRC = new UInt32[(Int32)NetClassGroup.NetClassGroupCount];
            for (var i = 0; i < ClassCRC.Length; ++i)
                ClassCRC[i] = 0xFFFFFFFFU;

            NetClassBitSize = new UInt32[(Int32)NetClassGroup.NetClassGroupCount][];

            ClassTable = new List<NetClassRep>[(Int32)NetClassGroup.NetClassGroupCount][];
            for (var i = 0; i < ClassTable.Length; ++i)
            {
                NetClassBitSize[i] = new UInt32[(Int32)NetClassType.NetClassTypeCount];

                ClassTable[i] = new List<NetClassRep>[(Int32)NetClassType.NetClassTypeCount];
                for (var j = 0; j < ClassTable[i].Length; ++j)
                    ClassTable[i][j] = new List<NetClassRep>();
            }
        }

        public static BaseObject Create(String className)
        {
            return (from walk in ClassList where walk.ClassName == className select walk.Create()).FirstOrDefault();
        }

        public static BaseObject Create(UInt32 groupId, UInt32 typeId, Int32 classId)
        {
            return ClassTable[groupId][typeId].Count > classId ? ClassTable[groupId][typeId][classId].Create() : null;
        }

        public static UInt32 GetNetClassCount(UInt32 classGroup, UInt32 classType)
        {
            return (UInt32) ClassTable[classGroup][classType].Count;
        }

        public static UInt32 GetNetClassBitSize(UInt32 classGroup, UInt32 classType)
        {
            return NetClassBitSize[classGroup][classType];
        }

        public static Boolean IsVersionBorderCount(UInt32 classGroup, UInt32 classType, UInt32 count)
        {
            return count == GetNetClassCount(classGroup, classType) || (count > 0 && ClassTable[classGroup][classType][(Int32) count].ClassVersion != ClassTable[classGroup][classType][(Int32) count - 1].ClassVersion);
        }

        public static NetClassRep GetClass(UInt32 classGroup, UInt32 classType, UInt32 index)
        {
            return ClassTable[classGroup][classType][(Int32) index];
        }

        public static UInt32 GetClassGroupCRC(NetClassGroup classGroup)
        {
            return ClassCRC[(Int32)classGroup];
        }

        public static void Initialize()
        {
            if (Initialized)
                return;

            for (var group = 0; group < ClassTable.Length; ++group)
            {
                var groupMask = 1U << group;

                for (var type = 0; type < ClassTable[group].Length; ++type)
                {
                    var dynamicTable = new List<NetClassRep>();

                    dynamicTable.AddRange(ClassList.Where(walk => (Int32)walk.ClassType == type && (walk.ClassGroupMask & groupMask) != 0));
                    if (dynamicTable.Count == 0)
                        continue;

                    dynamicTable.Sort(new NetClassRepComparer());

                    ClassTable[group][type] = dynamicTable;

                    for (var i = 0; i < ClassTable[group][type].Count; ++i)
                        ClassTable[group][type][i].ClassId[group] = (UInt32) i;
                    
                    NetClassBitSize[group][type] = Utils.GetBinLog2(Utils.GetNextPow2((UInt32) ClassTable[group][type].Count + 1U));
                }
            }

            Initialized = true;
        }

        public static void LogBitUsage()
        {
            Console.WriteLine("Net Class Bit Usage:");

            foreach (var walk in ClassList)
            {
                if (walk.InitialUpdateCount > 0U)
                    Console.WriteLine("{0} (Initial) - Count: {1}   Avg Size: {2}", walk.ClassName, walk.InitialUpdateCount, walk.InitialUpdateBitsUsed / (Single) walk.InitialUpdateCount);

                if (walk.PartialUpdateCount > 0U)
                    Console.WriteLine("{0} (Partial) - Count: {1}   Avg Size: {2}", walk.ClassName, walk.PartialUpdateCount, walk.PartialUpdateBitsUsed / (Single) walk.PartialUpdateCount);
            }
        }

        #endregion

        public class NetClassRepComparer : IComparer<NetClassRep>
        {
            public Int32 Compare(NetClassRep a, NetClassRep b)
            {
                if (a.ClassVersion != b.ClassVersion)
                    return a.ClassVersion - b.ClassVersion;

                return String.Compare(a.ClassName, b.ClassName, StringComparison.Ordinal);
            }
        }
    }
}
