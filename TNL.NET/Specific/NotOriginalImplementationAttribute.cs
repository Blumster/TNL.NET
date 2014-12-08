using System;

namespace TNL.NET.Specific
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class NotOriginalImplementationAttribute : Attribute
    {
    }
}
