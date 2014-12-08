using System;
using System.Reflection;

namespace TNL.NET.Structs
{
    using Entities;
    using Utils;

    public class FunctorDecl<T> : Functor where T : EventConnection
    {
        public MethodInfo Method;
        public Object[] Parameters;
        public Type[] ParamTypes;

        public FunctorDecl(String methodName, Type[] paramTypes)
        {
            Method = typeof(T).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            ParamTypes = paramTypes;
            Parameters = new Object[ParamTypes.Length];
        }

        public override void Set(Object[] parameters)
        {
            Parameters = parameters;
        }

        public override void Read(BitStream stream)
        {
            for (var i = 0; i < ParamTypes.Length; ++i)
                Parameters[i] = ReflectedReader.Read(stream, ParamTypes[i]);
        }

        public override void Write(BitStream stream)
        {
            foreach (var t in Parameters)
                ReflectedReader.Write(stream, t, t.GetType());
        }

        public override void Dispatch(Object obj)
        {
            if (Method == null || Parameters == null || obj == null || (obj as T) == null)
                return;

            try
            {
                Method.Invoke(obj, Parameters);
            }
            catch
            {
                Console.WriteLine("Invalid type?? Expected: {0} | Found: {1}", typeof(T).Name, obj.GetType().Name);
            }
        }
    }

}
