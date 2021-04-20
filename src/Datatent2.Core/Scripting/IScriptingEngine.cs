// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Datatent2.Core.Scripting
{
    public interface IScriptingEngine : IDisposable
    {
        T Execute<T>(object obj);
    }

    public interface IMultithreadedScriptingEngine : IScriptingEngine
    {
        List<ValueTuple<object, T>> Execute<T>(List<object> objects);
    }
}
