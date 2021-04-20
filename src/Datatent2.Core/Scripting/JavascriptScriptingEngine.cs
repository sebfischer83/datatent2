// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jint;

namespace Datatent2.Core.Scripting
{
    internal class JavascriptScriptingEngine : IScriptingEngine
    {
        private readonly string _script;
        private Engine _engine;

        public JavascriptScriptingEngine(string script)
        {
            _script = script;
            _engine = new Engine();
        }

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
