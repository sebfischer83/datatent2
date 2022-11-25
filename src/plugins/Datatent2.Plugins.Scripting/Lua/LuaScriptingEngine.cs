// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Datatent2.Contracts.Scripting;
using NLua;
using Prise.Plugin;

namespace Datatent2.Plugins.Scripting. Lua
{
    /// <summary>
    /// A lua scripting engine
    /// </summary>
    [Plugin(PluginType = typeof(IMultithreadedScriptingEngine))]
    public class LuaScriptingEngine : IMultithreadedScriptingEngine
    {
        /// <inheritdoc />
        public string Name => "NLua";

        /// <inheritdoc />
        public Guid Id => Guid.Parse("0e022cd2-3a12-4ae5-9bb4-d45f16d17869");

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="script"></param>
        public LuaScriptingEngine()
        {
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Execute(string script, object obj)
        {
            using var lua = new NLua.Lua();
            lua["dataObject"] = obj;
            lua.DoString(script);
            return lua.GetObjectFromPath("res");
        }

        /// <inheritdoc />
        public List<ValueTuple<object, object>> Execute(string script, List<object> objects)
        {
            var list = new List<ValueTuple<object, object>>(objects.Count);
            if (objects.Count > 100)
            {
                Parallel.For(0, objects.Count, i =>
                {
                    list.Add((objects[i], Execute(script, objects[i])));
                });
            }
            else
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    list.Add((objects[i], Execute(script, objects[i])));
                }
            }

            return list;
        }
    }
}
