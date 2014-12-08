using System;

namespace TNL.NET.Structs
{
    using Utils;

    public abstract class Functor
    {
        public abstract void Set(Object[] parameters);
        public abstract void Read(BitStream stream);
        public abstract void Write(BitStream stream);
        public abstract void Dispatch(Object obj);
    }

}
