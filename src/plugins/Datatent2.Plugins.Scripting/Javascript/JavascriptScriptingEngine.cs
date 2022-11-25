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
        private readonly Engine _engine;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="script"></param>
        public JavascriptScriptingEngine()
        {
            _engine = new Engine();
        }

        /// <inheritdoc />
        public string Name => "Jint";

        /// <inheritdoc />
        public Guid Id => Guid.Parse("4cdf32fc-eee4-48b6-bfab-4ab0b26bfe40");

        /// <inheritdoc />
        public object Execute(string script, object obj)
        {
            _engine.SetValue("dataObject", obj);
            return _engine.Execute(script).GetValue("res").ToObject();
        }
    }
}
