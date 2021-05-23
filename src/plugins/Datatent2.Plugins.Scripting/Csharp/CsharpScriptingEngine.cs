// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using Datatent2.Contracts.Scripting;
using Datatent2.Core.Scripting.Csharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Prise.Plugin;
using System;

namespace Datatent2.Plugins.Scripting.Csharp
{
    [Plugin(PluginType = typeof(IScriptingEngine))]
    public class CsharpScriptingEngine : IScriptingEngine
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

        public string Name => throw new NotImplementedException();

        private static Guid ID = Guid.Parse("8d94c4fb-8c75-421d-bb36-5cb6e91dd2f1");

        public Guid Id => ID;

        public void Dispose()
        {
        }

        public T Execute<T>(object obj)
        {
            return (T)_engine.RunAsync(new Globals(new DataObjectCSharpScriptWrapper(obj))).Result.ReturnValue;
        }
    }
}
