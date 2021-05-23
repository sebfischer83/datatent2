// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts.Scripting;
using Esprima.Ast;
using Jint;
using Prise.Plugin;

namespace Datatent2.Plugins.Scripting.Javascript
{
    [Plugin(PluginType = typeof(IScriptingEngine))]
    public class JavascriptScriptingEngine : IScriptingEngine
    {
        private readonly string _script;
        private Engine _engine;

        public JavascriptScriptingEngine(string script)
        {
            _script = script;
            _engine = new Engine();
        }

        public string Name => "Jint";

        private static Guid ID = Guid.Parse("4cdf32fc-eee4-48b6-bfab-4ab0b26bfe40");

        public Guid Id => ID;

        public void Dispose()
        {
            
        }

        public T Execute<T>(object obj)
        {
            _engine.SetValue("dataObject", obj);
            return (T)_engine.Execute(_script).GetValue("res").ToObject();
        }
    }
}
