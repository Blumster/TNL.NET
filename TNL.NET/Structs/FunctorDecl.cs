using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TNL.NET.Structs
{
    using Entities;
    using Utils;

    public class FunctorDecl<T> : Functor where T : EventConnection
    {
        public Delegate MethodDelegate;
        public Object[] Parameters;
        public Object[] Arguments;
        public Type[] ParamTypes;

        public FunctorDecl(String methodName, Type[] paramTypes)
        {
            var tType = typeof(T);

            var method = tType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            ParamTypes = paramTypes;

            var list = paramTypes.ToList();
            list.Insert(0, tType);

            MethodDelegate = Delegate.CreateDelegate(Expression.GetActionType(list.ToArray()), method);
        }

        public override void Set(Object[] parameters)
        {
            Parameters = parameters;
        }

        public override void Read(BitStream stream)
        {
            Arguments = new Object[ParamTypes.Length + 1];

            for (var i = 0; i < ParamTypes.Length; ++i)
                Arguments[1 + i] = ReflectedReader.Read(stream, ParamTypes[i]);
        }

        public override void Write(BitStream stream)
        {
            if (Parameters == null)
                return;

            foreach (var t in Parameters)
                ReflectedReader.Write(stream, t, t.GetType());
        }

        public override void Dispatch(Object obj)
        {
            if (MethodDelegate == null || Arguments == null || obj == null || (obj as T) == null)
                return;

            try
            {
                Arguments[0] = obj;
                MethodDelegate.DynamicInvoke(Arguments);
            }
            catch
            {
                Console.WriteLine("Invalid type?? Expected: {0} | Found: {1}", typeof(T).Name, obj.GetType().Name);
            }
        }
    }

}
