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
    [Plugin(PluginType = typeof(IMultithreadedScriptingEngine))]
    public class LuaScriptingEngine : IMultithreadedScriptingEngine
    {
        private readonly string _script;

        public string Name => "NLua";

        private static Guid ID = Guid.Parse("0e022cd2-3a12-4ae5-9bb4-d45f16d17869");

        public Guid Id => ID;

        public LuaScriptingEngine(string script)
        {
            _script = script;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Execute<T>(object obj)
        {
            using var lua = new NLua.Lua();
            lua["dataObject"] = obj;
            lua.DoString(_script);
            return (T)lua.GetObjectFromPath("res");
        }

        public List<ValueTuple<object, T>> Execute<T>(List<object> objects)
        {
            var list = new List<ValueTuple<object, T>>(objects.Count);
            if (objects.Count > 100)
            {
                Parallel.For(0, objects.Count, i =>
                {
                    list.Add((objects[i], Execute<T>(objects[i])));
                });
            }
            else
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    list.Add((objects[i], Execute<T>(objects[i])));
                }
            }

            return list;
        }

        public void Dispose()
        {
        }
    }
}
