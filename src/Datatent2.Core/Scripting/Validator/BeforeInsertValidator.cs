// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Scripting.Validator
{
    internal class BeforeInsertValidator
    {
        private readonly string _tableName;
        private readonly ScriptingProvider _provider;
        private readonly IScriptingEngine _scriptingEngine;

        public BeforeInsertValidator(string tableName, string script, ScriptingProvider provider)
        {
            _tableName = tableName;
            _provider = provider;

            _scriptingEngine = provider switch
            {
                ScriptingProvider.Lua => new LuaScriptingEngine(script),
                ScriptingProvider.Javascript => new JavascriptScriptingEngine(script),
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
            };
        }

        public bool IsValid(object obj)
        {
            return _scriptingEngine.Execute<bool>(obj);
        }

        public bool IsTableSupported(string tableName)
        {
            return string.Equals(_tableName, tableName, StringComparison.Ordinal);
        }
    }
}
