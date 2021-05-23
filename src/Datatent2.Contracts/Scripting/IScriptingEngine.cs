// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Contracts.Scripting
{
    public interface IScriptingEngine : IDisposable
    {
        public string Name { get; }

        public Guid Id { get; }

        T Execute<T>(object obj);
    }

    public interface IMultithreadedScriptingEngine : IScriptingEngine
    {
        List<ValueTuple<object, T>> Execute<T>(List<object> objects);
    }
}
