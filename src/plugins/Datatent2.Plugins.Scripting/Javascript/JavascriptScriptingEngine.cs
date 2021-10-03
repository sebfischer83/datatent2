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
    /// <summary>
    /// A javascript based engine
    /// </summary>
    [Plugin(PluginType = typeof(IScriptingEngine))]
    public class JavascriptScriptingEngine : IScriptingEngine
    {
        private readonly string _script;
        private readonly Engine _engine;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="script"></param>
        public JavascriptScriptingEngine(string script)
        {
            _script = script;
            _engine = new Engine();
        }

        /// <inheritdoc />
        public string Name => "Jint";

        private static readonly Guid ID = Guid.Parse("4cdf32fc-eee4-48b6-bfab-4ab0b26bfe40");

        /// <inheritdoc />
        public Guid Id => ID;

        /// <inheritdoc />
        public void Dispose()
        {
            // engine need no dispose
        }

        /// <inheritdoc />
        public T Execute<T>(object obj)
        {
            _engine.SetValue("dataObject", obj);
            return (T)_engine.Execute(_script).GetValue("res").ToObject();
        }
    }
}
