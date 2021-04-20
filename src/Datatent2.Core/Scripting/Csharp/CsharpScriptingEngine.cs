// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Datatent2.Core.Scripting.Csharp
{
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
