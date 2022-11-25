// # SPDX-License-Identifier: MIT
// # Copyright 2021
// # Sebastian Fischer sebfischer@gmx.net

using System;
using System.Collections.Generic;

namespace Datatent2.Contracts.Scripting
{
    /// <summary>
    /// Interface for scripting engines that can process multiple objects in parallel
    /// </summary>
    public interface IMultithreadedScriptingEngine : IScriptingEngine
    {
        /// <summary>
        /// Executes the given script on a list of object in parallel
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objects"></param>
        /// <returns></returns>
        List<ValueTuple<object, object>> Execute(string script, List<object> objects);
    }
}