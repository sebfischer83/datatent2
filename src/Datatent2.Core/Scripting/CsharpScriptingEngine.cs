using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Esprima.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Fasterflect;

namespace Datatent2.Core.Scripting
{
    public class Globals
    {
        public dynamic DataObject { get; set; }

        public Globals(DataObjectCSharpScriptWrapper dataObject)
        {
            this.DataObject = dataObject;
        }
    }

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
    }

    internal class CsharpScriptingEngine : IScriptingEngine
    {
        private readonly string _script;
        private readonly Script<object> _engine;

        public CsharpScriptingEngine(string script)
        {
            var asm = typeof(Globals).Assembly;
            var asmCsharp = typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly;
            _script = script;
            _engine = CSharpScript.Create(script, ScriptOptions.Default.WithReferences(asm, asmCsharp), globalsType: typeof(Globals));
        }

        public void Dispose()
        {
        }

        public T Execute<T>(object obj)
        {
            return (T)_engine.RunAsync(new Globals(new DataObjectCSharpScriptWrapper(obj))).Result.ReturnValue;
        }
    }
}
