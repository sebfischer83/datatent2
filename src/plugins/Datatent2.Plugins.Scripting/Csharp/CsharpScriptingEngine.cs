// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using Datatent2.Contracts.Scripting;
using Datatent2.Core.Scripting.Csharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Prise.Plugin;
using System;
using System.Reflection;

namespace Datatent2.Plugins.Scripting.Csharp
{
    //[Plugin(PluginType = typeof(IScriptingEngine))]
    //public class CsharpScriptingEngine : IScriptingEngine
    //{
    //    private Assembly _asm;
    //    private Assembly _asmCsharp;
        
    //    public CsharpScriptingEngine()
    //    {
    //        _asm = typeof(Globals).Assembly;
    //        _asmCsharp = typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly;
    //    }

    //    public string Name => "CSharp";

    //    public Guid Id => Guid.Parse("8d94c4fb-8c75-421d-bb36-5cb6e91dd2f1");

    //    public void Dispose()
    //    {
    //    }

    //    public object Execute(string script, object obj)
    //    {
    //        return CSharpScript.EvaluateAsync(script, ScriptOptions.Default.WithReferences(_asm, _asmCsharp), new Globals(new DataObjectCSharpScriptWrapper(obj))).Result;
    //    }
    //}
}
