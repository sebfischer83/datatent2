using System;
using System.Dynamic;
using Fasterflect;

namespace Datatent2.Core.Scripting.Csharp
{
    public class DataObjectCSharpScriptWrapper : DynamicObject
    {
        private readonly object _obj;
        private readonly Type _type;

        public DataObjectCSharpScriptWrapper(object obj)
        {
            _obj = obj;
            _type = obj.GetType();
        }
        
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = Reflect.Getter(_type, binder.Name)(_obj);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            Reflect.Setter(_type, binder.Name, FasterflectFlags.InstancePublic)(_obj, value);
            return true;
        }
    }
}